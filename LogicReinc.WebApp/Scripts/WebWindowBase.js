
window.onerror = function(error, url, line, col, errObj) {{
    _IPC(
        {{
            type:'error', 
            arguments:[
                {{
                    error:error, 
                    line: line,  
                    col:col, 
                    stack:errObj.stack
                }}
            ]
        }}
    );
}};

window.onload = function(){{
    {0}
}}

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
//Workaround for complex functions evalJson(getFunctionBody(()=>{{complexJS}}))
var getFunctionBody = function(func){{
    var funcStr = func.toString();
    var funcBody = funcStr.substring(funcStr.indexOf("{{") + 1, funcStr.lastIndexOf("}}"));
    return funcBody;
}};

function _LOG(msg){{
    _IPC({{
        respid: undefined,
        type: "log",
        arguments: [{{msg: msg}}]
	}}, true);
}}
