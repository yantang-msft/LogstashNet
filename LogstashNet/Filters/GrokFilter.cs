using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LogstashNet.Filters
{
    internal class GrokPattern
    {
        public string Name { get; set; }
        public string Pattern { get; set; }
    }

    internal class GrokFilter : FilterBase
    {
        #region static
        public static List<GrokPattern> GrokPatterns = new List<GrokPattern>();

        static GrokFilter()
        {
            var grokPatternFile = @".\Filters\GrokPatterns.pattern";
            var rawPatterns = ReadRawPatterns(grokPatternFile);

            // Some grok pattern definitions depends on other grok pattern, e.g. NUMBER (?:%{BASE10NUM}), NUMBER pattern depends on BASE10NUM
            // Replace the depended patterns with the real regex.
            var noDependencyPatternQueue = new Queue<GrokPattern>();

            // Get the pattern without any dependency
            FindNoDependencyPatterns(noDependencyPatternQueue, rawPatterns);

            while (noDependencyPatternQueue.Count > 0)
            {
                var pattern = noDependencyPatternQueue.Dequeue();
                GrokPatterns.Add(pattern);

                // Replace the remaining patterns that has dependency
                foreach (var rawPattern in rawPatterns)
                {
                    rawPattern.Pattern = new Regex(string.Format("%{{{0}}}", pattern.Name)).Replace(rawPattern.Pattern, pattern.Pattern);
                }

                // If the pattern has no dependency after replace, then we can add it to no dependency pattern queue
                FindNoDependencyPatterns(noDependencyPatternQueue, rawPatterns);
            }

            if (rawPatterns.Count > 0)
            {
                throw new Exception("Failed to initialize the grok pattern from file " + Path.GetFullPath(grokPatternFile));
            }
        }

        private static List<GrokPattern> ReadRawPatterns(string filePath)
        {
            var patterns = new List<GrokPattern>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    {
                        continue;
                    }

                    line = line.Trim();
                    int spaceIndex = line.IndexOf(' ');
                    if (spaceIndex > 0)
                    {
                        patterns.Add(new GrokPattern()
                        {
                            Name = line.Substring(0, spaceIndex),
                            Pattern = line.Substring(spaceIndex + 1)
                        });
                    }
                }
            }

            return patterns;
        }

        private static void FindNoDependencyPatterns(Queue<GrokPattern> noDependencyPatternQueue, List<GrokPattern> rawPatterns)
        {
            var dependedPatternRegex = @"%{\w+}";

            foreach (var rawPattern in rawPatterns.ToArray())
            {
                if (!Regex.Match(rawPattern.Pattern, dependedPatternRegex).Success)
                {
                    noDependencyPatternQueue.Enqueue(rawPattern);
                    rawPatterns.Remove(rawPattern);
                }
            }
        }
        #endregion

        private string _condition;
        private string _propertyPath;
        private string _pattern;

        public GrokFilter(List<string> match, string condition)
        {
            _condition = condition;
            _propertyPath = match.ElementAt(0);
            _pattern = ExpandPatternWithRegex(match.ElementAt(1));
        }

        public override void Apply(JObject evt)
        {
            // Filter by condition
            if (!string.IsNullOrEmpty(_condition))
            {
                if (!Utilities.EvaluateCondition(evt, _condition))
                {
                    return;
                }
            }

            ApplyMatch(evt);
        }

        private void ApplyMatch(JObject evt)
        {
            string value = null;

            try
            {
                if (evt.TryExpandPropertyByPath(_propertyPath, out value))
                {
                    // The expand method will add double quote for string type value.
                    // Trim it for grok regex match
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    var regex = new Regex(_pattern);
                    var match = regex.Match(value);
                    if (match.Success)
                    {
                        for (int i = 0; i < match.Groups.Count; i++)
                        {
                            var groupName = regex.GroupNameFromNumber(i);

                            // If the group name doesn't match the ordinal number, it's a named group (semantic). Add the semantic to event field.
                            // A corner case is the named group's name equals to its ordinal number, e.g., (?<1>\w+), such case will be ignored.
                            if (groupName != i.ToString())
                            {
                                evt[groupName] = match.Groups[i].Value;
                            }
                        }

                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Utilities.WriteError(e.ToString());
            }

            AddParseFailedTag(evt);
        }

        private string ExpandPatternWithRegex(string pattern)
        {
            foreach (var grokPattern in GrokPatterns)
            {
                var patternWithSemantic = "%{(?<syntax>" + grokPattern.Name + @"):(?<semantic>\w+)}";
                var patternWithoutSemantic = "%{(?<syntax>" + grokPattern.Name + ")}";

                pattern = Regex.Replace(pattern, patternWithSemantic, "(?<${semantic}>%{${syntax}})");
                pattern = Regex.Replace(pattern, patternWithoutSemantic, grokPattern.Pattern);
            }

            return pattern;
        }

        private void AddParseFailedTag(JObject evt)
        {
            evt.AddTag("_grokparsefailure");
        }
    }
}
