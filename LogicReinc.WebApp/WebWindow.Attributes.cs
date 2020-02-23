using System;
using System.Collections.Generic;
using System.Text;

namespace LogicReinc.WebApp
{

    public enum WebExposeType
    {
        All = 0,
        Attributed = 1,
        None = 2
    }
    public class ExposeSecurityAttribute : Attribute
    {
     
        public WebExposeType Type { get; set; }
        public ExposeSecurityAttribute(WebExposeType type)
        {
            Type = type;
        }
    }
    public class WebExposeAttribute : Attribute { }
    public class ContextResourcesAttribute : Attribute
    {
        public string Resources { get; set; }
        public string Path { get; set; }
        public ContextResourcesAttribute(string rPath, string path)
        {
            Path = path;
            Resources = rPath;
        }
    }
    public class ResourcePageAttribute : Attribute
    {
        public string Resource { get; set; }
        public ResourcePageAttribute(string resource)
        {
            Resource = resource;
        }
    }
    public class AppSizeAttribute : Attribute
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public AppSizeAttribute(int w, int h)
        {
            Width = w;
            Height = h;
        }
    }
    public class WindowAttribute : Attribute
    {
        public bool HasBorder { get; set; }
        public WindowAttribute() { }
    }
}
