using LogicReinc.WebApp.Javascript;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogicReinc.WebApp
{
    public class WebWindow
    {
        public static Assembly EntryAssembly { get; set; } = null;

        internal string _lastLoadedHtml = "";
        private bool _showOnReady = false;

        private Dictionary<string, Func<JToken[], object>> _ipcAvailable = null;

        private List<(string, Action<JToken, Exception>)> _jsStartupQueue = new List<(string, Action<JToken, Exception>)>();

        public string[] IPCAvailable => _ipcAvailable?.Select(x => x.Key).ToArray() ?? new string[] { };

        public IWebWindowImplementation Window { get; private set; }

        public WebContext Context { get; set; } = new WebContext();

        public bool IsReady { get; private set; } = false;


        public bool WriteHtmlToFile { get; set; } = true;
        public bool EmbedHtml { get; set; } = true;


        public event Action Loaded;

        public dynamic JS { get; private set; }

        public WebWindow(IWebWindowImplementation window = null)
        {
            Window = window ?? WebWindows.GetWindow();
            Window.Controller = this;
            WebAppLogger.Log(WebAppLogLevel.Info, "Window Created");
            Window.OnIPC += OnIPC;
            Loaded += OnReady;
            MapIPC();
            Initializing();
            Window.Invoke(() =>
            {
                Window.Startup();
            });
        }
        private void Initializing()
        {
            Type type = GetType();

            JS = new JSPassthrough(this);
            
            Window.RegisterInitScript(WebAppTemplates.Format_WebWindowBase(CSCallback((args) =>
            {
                WebAppLogger.Log(WebAppLogLevel.Info, "Initializing..");
                if (Loaded != null)
                    Loaded();
                return new NoIPCResponse();
            }, false, "Initializing")));

            //Load ContextResources
            foreach (ContextResourcesAttribute r in type.GetCustomAttributes<ContextResourcesAttribute>())
                Context.AddResourceData(EntryAssembly ?? Assembly.GetEntryAssembly(), r.Resources, r.Path);

            //Set size if specified
            AppSizeAttribute sizeAtt = type.GetCustomAttribute<AppSizeAttribute>();
            if (sizeAtt != null)
                WebWindows.Manager.Invoke(() => 
                    Window.SetSize(sizeAtt.Width, sizeAtt.Height));

            //Window settings
            WindowAttribute windowAtt = type.GetCustomAttribute<WindowAttribute>();
            if(windowAtt != null)
            {
                
                Window.Invoke(() =>
                {
                    if (!windowAtt.HasBorder)
                        Window.RemoveBorder();
                    else
                        Window.ShowBorder();
                });
            }

            Initialize();

            //Load page if specified
            ResourcePageAttribute rpageatt = type.GetCustomAttribute<ResourcePageAttribute>();
            if (rpageatt != null)
                LoadResource(rpageatt.Resource);
        }


        public JToken Execute(string js)
        {
            return Window.Execute(js); ;
        }


        //Shared
        [WebExpose]
        public void Show()
        {
            if (!IsReady)
                _showOnReady = true;
            else
            {
                Window.Invoke(() =>
                    Window.Show());
            }
        }
        [WebExpose]
        public void Focus()
        {
                Window.Invoke(() =>
                    Window.Focus());
        }
        [WebExpose]
        public void Hide()
        {
            Window.Invoke(() =>
                Window.Hide());
        }
        [WebExpose]
        public void Close()
        {
            Window.Invoke(() =>
            {
                Window.Close();
            });
        }

        [WebExpose]
        public void SetPosition(int x, int y)
        {
            Window.Invoke(() =>
                Window.SetPosition(x, y));
        }
        [WebExpose]
        public void SetSize(int w, int h)
        {
            Window.Invoke(() =>
                Window.SetSize(w, h));
        }


        [WebExpose]
        public void StartDragMove()
            => Window.StartDragMove();
        [WebExpose]
        public void StopDragMove()
            => Window.StopDragMove();

        [WebExpose]
        public void Alert(string msg)
        {
            Console.WriteLine(msg);
        }
        [WebExpose]
        public void Print(string msg)
        {
            Console.WriteLine(msg);
        }


        //Loading
        public void LoadResource(string resource)
        {
            //Workaround for some platforms
            Assembly c = EntryAssembly ?? Assembly.GetEntryAssembly();
            LoadHtml(WebContext.LoadStringResource(c, resource));
        }
        public void LoadHtml(string html)
        {
            WebPage page = new WebPage(html);
            Context.EmbedWebPage(page);
            _lastLoadedHtml = page.ToString();
            if (WriteHtmlToFile)
            {
                string path = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "lastpage.html");
                WebAppLogger.Log(WebAppLogLevel.Info, "Writing html to:" + path);
                File.WriteAllText(path, _lastLoadedHtml);
            }
            Window.LoadHtml(_lastLoadedHtml);
        }
        public void LoadUrl(string url)
        {
            Window.LoadUrl(url);
        }


        public void RegisterFunction(string function, Func<JToken[], object> cb, bool hasCallback = true)
        {
            if (function.Any(x => !char.IsLetter(x) && x != '_'))
                throw new ArgumentException("Only letters allowed in function name");

            Window.RegisterInitScript(WebAppTemplates.FormatIPCFunction(function, new IPCObject()
            {
                Type = "callback",
                DebugName = function,
                ID = RegisterCallback(cb, false),
                Arguments = new JavascriptReference("arguments"),
                NoCallback = !hasCallback
            }));
        }


        //Private
        private object[] ConvertIPCParameters(ParameterInfo[] paras, JToken[] args)
        {
            if (paras.Length != args.Length)
                throw new ArgumentException("IPC parameters do not match count");

            object[] resultArgs = new object[paras.Length];
            for(int i = 0; i < paras.Length; i++)
            {
                ParameterInfo parameter = paras[i];
                JToken arg = args[i];
                resultArgs[i] = arg.ToObject(parameter.ParameterType);
            }
            return resultArgs;
        }

        private async void OnReady()
        {
            IsReady = true;

            if (_showOnReady)
                this.Show();

            WebAppLogger.Log(WebAppLogLevel.Info, "Executing startup scripts");
            if (_jsStartupQueue != null && _jsStartupQueue.Count > 0)
                foreach (var jsq in _jsStartupQueue)
                    try
                    {
                        jsq.Item2(Execute(jsq.Item1), null);
                    }
                    catch (Exception ex)
                    {
                        jsq.Item2(null, ex);
                    }
            OnLoaded();
        }

        public virtual void Initialize()
        {

        }
        public virtual void OnLoaded()
        {

        }

        private async Task<object> OnIPC(string notify)
        {

            Task<object> t = Task.Run(() =>
            {
                JObject obj = JObject.Parse(notify);
                if (obj.ContainsKey("type"))
                {
                    string type = obj.GetValue("type").ToString();
                    JToken[] objs = obj.GetValue("arguments").ToArray();

                    switch (type)
                    {
                        case "callback":
                            WebAppLogger.Log(WebAppLogLevel.Info, "IPC: Callback");
                            string id = obj.GetValue("id").ToString();
                            object cbResult = TriggerCallback(id, objs);
                            return cbResult;
                        case "ipc":
                            WebAppLogger.Log(WebAppLogLevel.Info, "IPC: Passthrough");
                            string function = obj.GetValue("function").ToString();
                            return OnIPCCall(function, objs);
                        case "error":
                            WebAppLogger.Log(WebAppLogLevel.Info, "IPC: Error");
                            JObject errObj = objs[0].ToObject<JObject>();
                            OnScriptError(
                                errObj.GetValue("line").ToObject<int>(),
                                errObj.GetValue("col").ToObject<int>(),
                                errObj.GetValue("error").ToString(),
                                (errObj.ContainsKey("stack")) ?
                                    errObj.GetValue("stack").ToString() :
                                    "");
                            break;
                        case "log":
                            JObject logObj = objs[0].ToObject<JObject>();
                            Console.WriteLine($"Log:{logObj.GetValue("msg").ToString()}");
                            break;
                        default:
                            object result = null;
                            if (HandleIPC(type, obj, out result))
                                return result;
                            break;
                    }
                }
                return null;
            });
            return await t;
        }
        protected virtual object OnIPCCall(string function, JToken[] arguments)
        {
            if (_ipcAvailable.ContainsKey(function))
                return _ipcAvailable[function](arguments);
            return null;
        }
        protected virtual bool HandleIPC(string type, JObject ipcPackage, out object result)
        {
            result = null;
            return false;
        }

        private void OnScriptError(int line, int collumn, string error, string stack)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WebAppLogger.Log(WebAppLogLevel.Error, $"Script Error ({line}): {error}");
            WebAppLogger.Log(WebAppLogLevel.Error, stack);

            string[] htmlLines = _lastLoadedHtml.Split('\n');
            if (htmlLines.Length > line && line > 0)
            {
                string errLine = htmlLines[line - 1];
                if (errLine.Length > 100)
                {
                    int start = Math.Max(collumn - 50, 0);
                    int end = Math.Min(errLine.Length, collumn + 50) - start;
                    errLine = errLine.Substring(start,end);
                }
                Console.ForegroundColor = ConsoleColor.DarkRed;
                WebAppLogger.Log(WebAppLogLevel.Error, errLine);
                Console.ResetColor();
            }
        }
        private void MapIPC()
        {
            Type type = GetType();

            WebExposeType expose = type.GetCustomAttribute<ExposeSecurityAttribute>()?.Type ?? WebExposeType.All;

            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);

            _ipcAvailable = new Dictionary<string, Func<JToken[], object>>();
            switch (expose)
            {
                case WebExposeType.Attributed:
                    methods = methods.Where(x => x.GetCustomAttribute<WebExposeAttribute>() != null).ToArray();
                    break;
                case WebExposeType.None:
                    return;
            }

            foreach (MethodInfo meth in methods)
            {
                if (_ipcAvailable.ContainsKey(meth.Name))
                    throw new InvalidOperationException("Not allowed duplicate method names in UI class");

                ParameterInfo[] methParas = meth.GetParameters();
                _ipcAvailable.Add(meth.Name, (paras) =>
                {
                    return meth.Invoke(this, ConvertIPCParameters(methParas, paras));
                });
            }

            foreach (MethodInfo meth in methods)
                RegisterFunction(meth.Name, (args) => OnIPCCall(meth.Name, args), meth.ReturnType != typeof(void));
        }

        //Core
        #region CallbackSystem
        private Dictionary<string, ScriptCallback> _scriptCallbacks = new Dictionary<string, ScriptCallback>();

        public string CSCallback(Func<JToken[], object> callback, bool repeatable = false, string debugName = null, params string[] arguments)
        {
            return WebAppTemplates.FormatIPC(new IPCObject()
            {
                DebugName = debugName,
                Type = "callback",
                ID = RegisterCallback(callback, !repeatable),
                Arguments = arguments.Select(x=>new JavascriptReference(x)).ToArray()
            });
        }

        private string RegisterCallback(Func<JToken[], object> callback, bool removeOnCall = false, string debugName = null)
        {
            ScriptCallback cb = new ScriptCallback()
            {
                DebugName = debugName,
                ID = Guid.NewGuid().ToString(),
                Callback = callback,
                RemoveOnCall = removeOnCall,
            };
            if (_scriptCallbacks.ContainsKey(cb.ID))
                _scriptCallbacks[cb.ID] = cb;
            else
                _scriptCallbacks.Add(cb.ID, cb);
            return cb.ID;
        }
        private object TriggerCallback(string id, JToken[] args)
        {
            if (_scriptCallbacks.ContainsKey(id))
            {
                ScriptCallback callback = _scriptCallbacks[id];
                if (callback.RemoveOnCall)
                    _scriptCallbacks.Remove(id);
                return callback.Callback(args);
            }
            return null;
        }


        private class ScriptCallback
        {
            public string ID { get; set; }
            public string DebugName { get; set; }
            public Func<JToken[], object> Callback { get; set; }
            public bool RemoveOnCall { get; set; }
        }
        #endregion


        public class JSPassthrough : DynamicObject
        {
            public string ParentPath { get; set; }
            public WebWindow Parent { get; set; }

            private bool _existsFunctionCacheEnabled = true;
            private Dictionary<string, bool> _existsFunctionCache = new Dictionary<string, bool>();
            private bool _existsObjectCacheEnabled = true;
            private Dictionary<string, bool> _existsObjectCache = new Dictionary<string, bool>();

            public JSPassthrough(WebWindow parent)
            {
                Parent = parent;
            }
            public JSPassthrough(WebWindow parent, string parentPath)
            {
                Parent = parent;
                ParentPath = parentPath;
            }

            private string GetMemberName(string name)
            {
                string accessor = name;
                if (ParentPath != null)
                    accessor = $"{ParentPath}.{accessor}";
                return accessor;
            }

            public async Task<bool> Exists(string name)
            {
                if (_existsObjectCacheEnabled)
                {
                    if (!_existsObjectCache.ContainsKey(name))
                        _existsObjectCache.Add(name, (Parent.Execute($"(typeof {name} == 'undefined')")).ToObject<bool>());
                    return _existsObjectCache[name];
                }
                else
                    return (Parent.Window.Execute($"(typeof {name} == 'undefined')")).ToObject<bool>();
            }
            public async Task<bool> CanCall(string name)
            {
                if (_existsFunctionCacheEnabled)
                {
                    if (!_existsFunctionCache.ContainsKey(name))
                        _existsFunctionCache.Add(name, (Parent.Execute($"(typeof {name} == 'function')")).ToObject<bool>());
                    return _existsFunctionCache[name];
                }
                else
                    return (Parent.Window.Execute($"(typeof {name} == 'function')")).ToObject<bool>();
            }


            public JToken Get()
            {
                if (ParentPath != null)
                {
                    JToken result = Parent.Window.Execute(ParentPath);
                    return result;
                }
                else
                    throw new ArgumentException("Invalid object?");
            }


            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                string accessor = GetMemberName(binder.Name);
                if (accessor != null)
                {
                    string json = JsonConvert.SerializeObject(value);
                    Parent.Window.Execute($"{accessor} = {json};");
                    return true;
                }
                else
                    throw new ArgumentException("Invalid object?");
            }
            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                string name = binder.Name;
                if (name == "_this")
                    name = "this";

                result = new JSPassthrough(Parent, GetMemberName(name));
                return true;
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                string call = $"{GetMemberName(binder.Name)}({string.Join(",", args.Select(x => JsonConvert.SerializeObject(x)))})";
                result = Parent.Execute(call);

                return true;
            }
        
        
            public static implicit operator String(JSPassthrough ps) =>
                ps.Get().ToString();
            public static implicit operator short(JSPassthrough ps) =>
                ps.Get().ToObject<short>();
            public static implicit operator int(JSPassthrough ps) =>
                ps.Get().ToObject<int>();
            public static implicit operator long(JSPassthrough ps) =>
                ps.Get().ToObject<long>();
            public static implicit operator float(JSPassthrough ps) =>
                ps.Get().ToObject<float>();
            public static implicit operator double(JSPassthrough ps) =>
                ps.Get().ToObject<double>();
            public static implicit operator DateTime(JSPassthrough ps) =>
                ps.Get().ToObject<DateTime>();
            public static implicit operator TimeSpan(JSPassthrough ps) =>
                ps.Get().ToObject<TimeSpan>();
            public static implicit operator bool(JSPassthrough ps) =>
                ps.Get().ToObject<bool>();

            public T[] ToArray<T>()
            {
                return Get().ToObject<T[]>();
            }
            public List<T> ToList<T>()
            {
                return Get().ToObject<List<T>>();
            }


            private static object AutoResolveJToken(JToken result)
            {
                switch (result.Type)
                {
                    case JTokenType.String:
                        return result.ToString();
                    case JTokenType.Integer:
                        return result.ToObject<int>();
                    case JTokenType.Float:
                        return result.ToObject<float>();
                    case JTokenType.Date:
                        return result.ToObject<DateTime>();
                    case JTokenType.Boolean:
                        return result.ToObject<bool>();
                    case JTokenType.Bytes:
                        return result.ToObject<byte[]>();
                    case JTokenType.Null:
                        return null;
                    case JTokenType.Object:
                        return (dynamic)result;
                    case JTokenType.TimeSpan:
                        return result.ToObject<TimeSpan>();
                    default:
                        throw new ArgumentException("Could not automatically convert JS object");
                }
            }


        }


        private static Task WaitOneAsync(WaitHandle waitHandle)
        {
            if (waitHandle == null)
                throw new ArgumentNullException("waitHandle");

            var tcs = new TaskCompletionSource<bool>();
            var rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle,
                delegate { tcs.TrySetResult(true); }, null, -1, true);
            var t = tcs.Task;
            t.ContinueWith((antecedent) => rwh.Unregister(null));
            return t;
        }
    }
}
