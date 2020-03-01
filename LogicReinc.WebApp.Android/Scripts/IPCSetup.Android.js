var _IPCCounter = 0;
var _IPCResolves = {{}};
function _IPC(obj) {{
        var newID = _IPCCounter++;
        obj.respid = newID;
        return new Promise(function (resolve, reject) {{
                _IPCResolves[newID] = (resp) => {{
                        resolve(resp);
                        _IPCResolves[newID] = undefined;
                }};
                _host.onIPC(newID + ':' + JSON.stringify(obj));
        }});
}}