using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogicReinc.WebApp
{
    public class WebPage
    {
        private string _document = "";
        public WebPage(string str)
        {
            _document = str;
        }

        public override string ToString()
        {
            return _document;
        }

        public void AddToBodyStart(string replacement)
        {
            int hlength = "<body>".Length;
            int hIndex = _document.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
            _document = _document.Substring(0, hIndex) + "<body>" + replacement + _document.Substring(hIndex + hlength);
        }
        public void AddToEnd(string replacement)
        {
            int blength = "</body>".Length;
            int bIndex = _document.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            _document = _document.Substring(0, bIndex) +  replacement + "</body>" + _document.Substring(bIndex + blength);
        }
        public void AddToHeaderStart(string replacement)
        {
            int hlength = "<head>".Length;
            int hIndex = _document.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
            _document = _document.Substring(0, hIndex) + "<head>" + replacement + _document.Substring(hIndex + hlength);
        }
        public void AddToHeader(string replacement)
        {
            int hlength = "</head>".Length;
            int hIndex = _document.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            _document = _document.Substring(0, hIndex) + replacement + "</head>" + _document.Substring(hIndex + hlength);
        }

        public void ReplaceScripts(Dictionary<string, Component> data)
        {
            MatchCollection scripts = GetTag("script");

            Dictionary<string, string> replacements = new Dictionary<string, string>();
            foreach (Match script in scripts)
            {
                var args = GetTagArgs(script);

                if (!args.ContainsKey("src"))
                    continue;
                string tag = script.Groups[0].Value;
                string src = args["src"];//GetAttributeArg("src", tag);

                if (data.ContainsKey(src.ToLower()))
                {
                    Component c = data[src.ToLower()];
                    if (c.Type == ComponentType.Script)
                    {
                        if (!replacements.ContainsKey(tag))
                            replacements.Add(tag, c.GetDataFormatted());
                    }
                }
            }
            foreach (var keyval in replacements)
                _document = _document.Replace(keyval.Key, keyval.Value);
        }

        public void ReplaceStyles(Dictionary<string, Component> data)
        {
            MatchCollection scripts = GetTag("link", false);

            Dictionary<string, string> replacements = new Dictionary<string, string>();
            foreach (Match style in scripts)
            {

                var args = GetTagArgs(style);

                if (!args.ContainsKey("rel"))
                    continue;
                if (!args.ContainsKey("href"))
                    continue;

                string rel = args["rel"];
                string href = args["href"];

                if (data.ContainsKey(href.ToLower()))
                {
                    Component c = data[href.ToLower()];
                    if (c.Type == ComponentType.Style)
                    {
                        if (!replacements.ContainsKey(style.Value))
                            replacements.Add(style.Value, c.GetDataFormatted());
                    }
                }
            }
            foreach (var keyval in replacements)
                _document = _document.Replace(keyval.Key, keyval.Value);
        }


        private string GetAttributeArg(string attr, string tag)
        {
            int indexAttr = tag.IndexOf(attr);
            int indexArg = tag.IndexOf("\"", indexAttr);
            int argLength = tag.IndexOf("\"", indexArg + 1) - indexArg;
            return tag.Substring(indexArg + 1, argLength - 1);
        }
        private Dictionary<string, string> GetTagArgs(Match match)
        {
            string parasRegex = "([a-zA-Z]*)=\\\"(.*?)\\\"";

            Dictionary<string, string> args = new Dictionary<string, string>();
            foreach (Match arg in Regex.Matches(match.Groups[1].Value, parasRegex))
                if (args.ContainsKey(arg.Groups[1].Value))
                    args[arg.Groups[1].Value] = arg.Groups[2].Value;
                else
                    args.Add(arg.Groups[1].Value, arg.Groups[2].Value);
            return args;
        }
        private MatchCollection GetTag(string tag, bool hasEndTag = true)
        {
            string b = $"<{tag}\\w*?(.*)>" + ((hasEndTag) ? $".*<\\/{tag}>" : "");
            return Regex.Matches(_document, b);
        }

        public class Component
        {
            public ComponentType Type { get; set; }
            public string Path { get; set; }
            public string Name { get; set; }
            public string Data { get; set; }

            public Component()
            {
            }
            public Component(string data, ComponentType type)
            {
                this.Data = data;
                this.Type = type;
            }
            public Component(string path, string data, ComponentType type)
            {
                this.Path = path;
                this.Data = data;
                this.Type = type;
            }

            public string GetDataFormatted()
            {
                switch (Type)
                {
                    case ComponentType.Script:
                        return $"<!--{Path}-->\n<script>\n{Data}\n</script>";
                    case ComponentType.Style:
                        return $"<!--{Path}-->\n<style>\n{Data}\n</style>";
                }
                return "";
            }
        }

        public enum ComponentType
        {
            Unknown = 0,
            Script = 1,
            Style = 2
        }
    }
}
