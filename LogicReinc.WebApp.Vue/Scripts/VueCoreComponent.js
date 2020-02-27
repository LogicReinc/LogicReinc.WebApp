
window._comp{0}Eval = {{}};
Vue.component("{0}", {{
	data:()=>{{
		var dataObj = {{
			{1}
		}};
		return dataObj;
	}},
	methods:{{
		PushChanges(changes){{
			var obj = changes;
			Vue.nextTick(()=>{{
				for(var key in obj)
				{{
					this[key] = obj[key];
				}}
			}});
		}},
		{2}
	}},
	mounted(){{
		CreateComponentInstance("{0}").then((result)=>{{
			this._comp = result//await CreateComponentInstance("{0}");
			window._comp{0}Eval[this._comp.id] = (js)=>{{
				return function(str){{ 
					return eval(str); 
				}}.call(this, js);
			}};

			Vue.nextTick(()=>{{
				if(!this._comp_data)
				for(var key in this._comp.data){{
					this[key] = this._comp.data[key];
					Alert(key + ":" + this._comp.data[key]);
				}}
				if(this.Mounted)
					this.Mounted();
			}});
		}});
	}},
	destroyed(){{
		if(this.Destroyed)
			this.Destroyed();
	}},
	template: `{3}`
}});