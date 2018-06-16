using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SshConfigParser
{
    public class SshConfig : List<ConfigNode>
    {
        private static readonly Regex RE_SPACE = new Regex("\\s");
        private static readonly Regex RE_LINE_BREAK = new Regex("\\r|\\n");
        private static readonly Regex RE_SECTION_DIRECTIVE = new Regex("^(Host|Match)$", RegexOptions.IgnoreCase);
        private static readonly Regex RE_QUOTED = new Regex("^(\")(.*)\\1$");

        public IDictionary<string, object> Find(string host)
        {
            var results = new Dictionary<string, object>();

            void SetProperty(string name, object value)
            {
                if (name == "IdentityFile")
                {
                    if (!results.ContainsKey(name))
                    {
                        results[name] = new List<object>();
                    }

                    ((List<object>) results[name]).Add(value);
                }
                else if (!results.ContainsKey(name))
                {
                    results[name] = value;
                }
            }

            foreach (var line in this)
            {
                if (line.Type != NodeType.Directive)
                {
                    continue;
                }

                if (line.Param == "Host")
                {
                    if (glob(line.Value, host))
                    {
                        SetProperty(line.Param, line.Value);

                        line.Config
                            .Where(n => n.Type == NodeType.Directive)
                            .ForEach(n => SetProperty(n.Param, n.Value));
                    }
                }
                else if (line.Param == "Match")
                {
                    // TODO
                }
                else
                {
                    SetProperty(line.Param, line.Value);
                }
            }

            return results;
        }

        /// <summary>
        /// Removes an entry by host.
        /// </summary>
        /// <param name="host"></param>
        public void RemoveByHost(string host)
        {
            var result = FindNodeByHost(host);

            if (result != null)
            {
                this.Remove(result);
            }
        }


        /// <summary>
        /// Removes an entry by match
        /// </summary>
        /// <param name="match"></param>
        public void RemoveByMatch(string match)
        {
            var result = FindNodeByMatch(match);

            if (result != null)
            {
                this.Remove(result);
            }
        }


        /// <summary>
        /// Append new section to existing ssh config
        /// </summary>
        /// <param name="opts"></param>
        public SshConfig Add(IDictionary opts)
        {
            // We use IDictionary so we can support Hashtables from powershell
            // or a Dictionary<string, string>

            var config = this;
            var configWas = this;
            var indent = "  ";

            foreach (var line in this)
            {
                if (RE_SECTION_DIRECTIVE.IsMatch(line.Param))
                {
                    foreach (var subline in line.Config)
                    {
                        if (!string.IsNullOrEmpty(subline.Before))
                        {
                            indent = subline.Before;
                            break;
                        }
                    }
                }
            }

            foreach (var key in opts.Keys)
            {
                var line = new ConfigNode
                {
                    Type = NodeType.Directive,
                    Param = key.ToString(),
                    Separator = " ",
                    Value = opts[key]?.ToString(),
                    Before = "",
                    After = "\n"
                };

                if (RE_SECTION_DIRECTIVE.IsMatch(key.ToString()))
                {
                    config = configWas;
                    config.Add(line);
                    config = line.Config = new SshConfig();
                }
                else
                {
                    line.Before = indent;
                    config.Add(line);
                }
            }

            config[config.Count - 1].After += '\n';

            return configWas;
        }

        /// <summary>
        /// Finds a config element by host.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public ConfigNode FindNodeByHost(string host)
        {
            return FindNode("Host", host);
        }

        /// <summary>
        /// Finds a config element by Match
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public ConfigNode FindNodeByMatch(string match)
        {
            return FindNode("Match", match);
        }

        private ConfigNode FindNode(string findBy, string find)
        {
            if (findBy != "Match" && findBy != "Host")
            {
                throw new Exception("Can only find by Host or Match");
            }

            var query = from line in this
                where line.Type == NodeType.Directive
                      && RE_SECTION_DIRECTIVE.IsMatch(line.Param)
                      && line.Param == findBy
                      && find == line.Value
                select line;


            return query.FirstOrDefault();
        }

        /// <summary>
        /// Stringify structured object into ssh config text
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var output = new StringBuilder();

            void Format(ConfigNode line)
            {
                output.Append(line.Before);

                if (line.Type == NodeType.Comment)
                {
                    output.Append(line.Content);
                }
                else if (line.Type == NodeType.Directive)
                {
                    string str = line.Quoted || (line.Param == "IdentityFile" && RE_SPACE.IsMatch(line.Value))
                        ? line.Param + line.Separator + '"' + line.Value + '"'
                        : line.Param + line.Separator + line.Value;

                    output.Append(str);
                }

                output.Append(line.After);

                line.Config?.ForEach(Format);
            }

            ForEach(Format);

            return output.ToString();
        }

        /// <summary>
        /// Parses a file by path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static SshConfig ParseFile(string path)
        {
            return Parse(File.ReadAllText(path));
        }

        /// <summary>
        /// Parses the SSH config text.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static SshConfig Parse(string str)
        {
            var i = 0;
            var chr = Next();
            var config = new SshConfig();
            var configWas = config;

            string Next()
            {
                var j = i++;
                return j < str.Length ? str[j].ToString() : null;
            }

            string Space()
            {
                var spaces = "";

                while (chr != null && RE_SPACE.IsMatch(chr))
                {
                    spaces += chr;
                    chr = Next();
                }

                return spaces;
            }

            string Linebreak()
            {
                var breaks = "";

                while (chr != null && RE_LINE_BREAK.IsMatch(chr))
                {
                    breaks += chr;
                    chr = Next();
                }

                return breaks;
            }

            string Option()
            {
                var opt = "";

                while (!string.IsNullOrEmpty(chr) && chr != " " && chr != "=")
                {
                    opt += chr;
                    chr = Next();
                }

                return opt;
            }

            string Separator()
            {
                var sep = Space();

                if (chr == "=")
                {
                    sep += chr;
                    chr = Next();
                }

                return sep + Space();
            }

            string Value()
            {
                var val = "";

                while (!string.IsNullOrEmpty(chr) && !RE_LINE_BREAK.IsMatch(chr))
                {
                    val += chr;
                    chr = Next();
                }

                return val.Trim();
            }

            ConfigNode Comment()
            {
                var type = NodeType.Comment;
                var content = "";

                while (!string.IsNullOrEmpty(chr) && !RE_LINE_BREAK.IsMatch(chr))
                {
                    content += chr;
                    chr = Next();
                }

                return new ConfigNode {Type = type, Content = content};
            }

            ConfigNode Directive()
            {
                var type = NodeType.Directive;

                return new ConfigNode
                {
                    Type = type,
                    Param = Option(),
                    Separator = Separator(),
                    Value = Value()
                };
            }

            ConfigNode Line()
            {
                var before = Space();
                var node = chr == "#" ? Comment() : Directive();
                var after = Linebreak();

                node.Before = before;
                node.After = after;

                if (RE_QUOTED.IsMatch(node.Value))
                {
                    node.Value = RE_QUOTED.Replace(node.Value, "$2");
                    node.Quoted = true;
                }

                return node;
            }


            while (chr != null)
            {
                var node = Line();

                if (node.Type == NodeType.Directive && RE_SECTION_DIRECTIVE.IsMatch(node.Param))
                {
                    config = configWas;
                    config.Add(node);
                    config = node.Config = new SshConfig();
                }
                else
                {
                    config.Add(node);
                }
            }

            return configWas;
        }
    }
}