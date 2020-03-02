
var _IPCCounter = 1;
var _IPCResolves = {{}};
function _IPC(obj, noCallback){{
    var newID = _IPCCounter++;
    if(!noCallback)
        obj.cb = newID;
    else
        newID = "";
    return new Promise(function(resolve, reject){{
        if(!noCallback){{
            _IPCResolves[newID] = (resp)=> {{
                resolve(resp); 
                _IPCResolves[newID] = undefined;
            }};
        }}
        _IPC_send(JSON.stringify(obj));
        //window.external.notify(newID + ':' + JSON.stringify(obj));
    }});
}}

var eval = eval;

var evalJson = function(js){{
    try{{
        return JSON.stringify(eval(js));
    }}
    catch(err){{
        var newErr = err.constructor("Error in eval: " + err.message);
        if(err.lineNumber)
            newErr.lineNumber = err.lineNumber - newErr.lineNumber + 3;
        throw newErr;
	}}
}}

var evalJsonWithCallback = function (id, js) {{
    var result = evalJson(js);
    _IPC_send(JSON.stringify({{
        type: "response",
        id: id,
        arguments: [ result ]
    }}));
}}



//Workaround for complex functions evalJson(getFunctionBody(()=>{{complexJS}}))
var getFunctionBody = function(func){{
    var funcStr = func.toString();
    var funcBody = funcStr.substring(funcStr.indexOf("{{") + 1, funcStr.lastIndexOf("}}"));
    return funcBody;
}};