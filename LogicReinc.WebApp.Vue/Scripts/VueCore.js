
var vuePushChanges = undefined;

var app = new Vue({{
	el: "{0}",
	data:{1},
	methods:{{
		PushChanges(json){{
			var obj = json;//JSON.parse(json);
			Vue.nextTick(()=>{{
				for(var key in obj)
					this[key] = obj[key];
			}});
		}},
		{2}
	}},
	mounted(){{
        console.log("Vue:Mounted");
		evalJson = (js)=>{{
			return function(str){{ 
				return JSON.stringify(eval(str)) 
			}}.call(this, js);
		}};
		Mounted();
	}}
}});

