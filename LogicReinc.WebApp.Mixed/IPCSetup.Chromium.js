function _IPC_send(str) {{
    window.cefQuery({{
        request: str,
        onSuccess: function (resp) {{}},
        onFailure: function (err, m) {{}}
    }});
}}