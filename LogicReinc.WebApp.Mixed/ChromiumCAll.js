try
{{
    var call = getFunctionBody(()=>{{
      {0}  
    }});
    _ChromResponse({1}, evalJson(call), undefined);
}}
catch(ex)
{{
    _ChromResponse({1}, undefined, "ex:" + ex.message + ", line:" + ex.line);
    throw ex;
}}