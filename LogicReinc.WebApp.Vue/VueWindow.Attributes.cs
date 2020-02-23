using System;
using System.Collections.Generic;
using System.Text;

namespace LogicReinc.WebApp.Vue
{
    public class VueSettingsAttribute : Attribute
    {
        public bool RequireExpose { get; set; }
    }
    public class VueDataAttribute : Attribute
    {
       
        public VueDataAttribute()
        {

        }
    }
}
