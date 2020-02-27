var _IPCCounter = 0;
var _IPCResolves = {{}};
function _IPC(obj, noCallback){{
    var newID = _IPCCounter++;
    noCallback = noCallback || obj.nocallback;
    if(!noCallback)
        obj.respid = newID;
    else
        newID = "";
    return new Promise(function(resolve, reject){{
        if(!noCallback)
            _IPCResolves[newID] = (resp)=> {{
                resolve(resp); 
                _IPCResolves[newID] = undefined;
            }};
        obj.nocallback = noCallback;
        window.cefQuery(
        {{
            request: newID + ':' + JSON.stringify(obj), 
            onSuccess: function(resp){{}}, 
            onFailure: function(err,m){{}}
        }});
    }});
}}
function _ChromResponse(id, obj, ex){{
  window.cefQuery(
    {{
            request: "chrom:" + JSON.stringify({{ id: id, result: obj, excp: ex}}), 
            onSuccess: function(resp){{}}, 
            onFailure: function(err,m){{}}
    }});  
}}
function evalJsonChrome(id, js){{
    try
    {{
        var result = evalJson(js);
        _ChromResponse(id, result, undefined);
	}}
    catch(ex)
    {{
        _ChromResponse(id, undefined, "ex:" + ex.message);
        throw ex;
	}}
}}