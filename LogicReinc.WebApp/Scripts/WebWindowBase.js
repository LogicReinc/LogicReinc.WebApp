
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
    return JSON.stringify(eval(js));
}}
