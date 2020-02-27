/*
var _IPCCounter = 0;
var _IPCResolves = {{}};
function _IPC(obj, noCallback){{
    var newID = _IPCCounter++;
    if(!noCallback)
        obj.respid = newID;
    else
        newID = "";
    return new Promise(function(resolve, reject){{
        if(!noCallback){{
            _IPCResolves[newID] = (resp)=> {{
                resolve(resp); 
                _IPCResolves[newID] = undefined;
            }};
        }
        window.external.notify(newID + ':' + JSON.stringify(obj));
    }});
}}*/