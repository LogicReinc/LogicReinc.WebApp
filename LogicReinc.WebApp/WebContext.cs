using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace LogicReinc.WebApp
{
    public class WebContext
    {
        private Dictionary<string, WebPage.Component> _data = new Dictionary<string, WebPage.Component>();


        public void EmbedWebPage(WebPage page)
        {
            page.ReplaceScripts(_data);
            page.ReplaceStyles(_data);
        }


        public void AddData(string path, WebPage.Component component)
        {
            _data.Add(path.ToLower(), component);
        }

        
        public static List<WebPage.Component> GetResourceComponents(Assembly a, string resourcePath, bool relativePaths = false)
        {
            List<WebPage.Component> components = new List<WebPage.Component>();

            string[] validExtensions = new string[] { "css", "js" };

            string[] arr = a.GetManifestResourceNames().Where(x =>
            {
                return x.StartsWith(resourcePath) &&
                    validExtensions.Any(y => x.EndsWith($".{y}"));
            }).ToArray();

            foreach (string rName in arr)
            {
                WebPage.ComponentType type = WebPage.ComponentType.Unknown;
                switch (rName.Substring(rName.LastIndexOf('.') + 1))
                {
                    case "css":
                        type = WebPage.ComponentType.Style;
                        break;
                    case "js":
                        type = WebPage.ComponentType.Script;
                        break;
                    default:
                        continue;
                }

                string data = LoadStringResource(a, rName);

                string newPath = rName;
                if (relativePaths)
                {
                    newPath = rName.ToLower().Replace(resourcePath.ToLower(), "").Trim('.');

                    string namePath = newPath.Substring(0, newPath.LastIndexOf('.'));

                    if (namePath.EndsWith(".min"))
                        namePath = namePath.Substring(0, namePath.LastIndexOf(".")).Replace(".", "/") + ".min";
                    else
                        namePath = namePath.Replace(".", "/");

                    newPath = namePath + newPath.Substring(newPath.LastIndexOf('.'));
                }

                WebPage.Component comp = new WebPage.Component(newPath, data, type);
                components.Add(comp);
            }
            return components;
        }
        
        public void AddResourceComponents(string path, List<WebPage.Component> comps)
        {
            foreach (WebPage.Component comp in comps)
                AddData(Path.Combine(path, comp.Path), comp);
        }
        
        public void AddResourceData(Assembly a, string resourcePath, string path)
        {
            List<WebPage.Component> components = GetResourceComponents(a, resourcePath, true);

            foreach (WebPage.Component comp in components)
                AddData(Path.Combine(path, comp.Path), comp);
        }

        public static string LoadStringResource(Assembly a, string resource)
        {
            string[] rs = a.GetManifestResourceNames();
            using (Stream str = a.GetManifestResourceStream(resource))
                if (str != null)
                    using (StreamReader reader = new StreamReader(str))
                        return reader.ReadToEnd();
                else
                    throw new ArgumentException($"Resource [{resource}] was not found in main assembly");
        }
    }
}
