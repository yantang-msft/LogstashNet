using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CSharp;
using Newtonsoft.Json.Linq;

namespace LogstashNet
{
    internal static class Utilities
    {
        public static void WriteError(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.ResetColor();
        }

        public static object CompileCondition(string condition)
        {
            // This is the preprocess step to replace the json oject field reference to function calls.
            // Although this step can also be done in CompileConditionCore(), we put it here so it's more debuggable.
            string cond = condition;
            string regex = @"(\[\w+\])+"; // match the event properties reference. i.e., [field][subfield]
            var matchedPropertyPaths = new HashSet<string>();

            var match = Regex.Match(condition, regex);
            while (match.Success)
            {
                matchedPropertyPaths.Add(match.Value);
                match = match.NextMatch();
            }

            foreach (var propertyPath in matchedPropertyPaths)
            {
                cond = condition.Replace(propertyPath, $"ExpandPropertyByPath(evt, \"{propertyPath}\")");
            }

            var result = CompileConditionCore(cond);
            if (result == null)
            {
                throw new Exception("Failed to compile condition: " + condition);
            }

            return result;
        }

        private static object CompileConditionCore(string condition)
        {
            CSharpCodeProvider c = new CSharpCodeProvider();
            CompilerParameters cp = new CompilerParameters();

            cp.ReferencedAssemblies.Add("system.dll");
            cp.ReferencedAssemblies.Add("system.core.dll");
            cp.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            cp.ReferencedAssemblies.Add("Newtonsoft.Json.dll");

            cp.CompilerOptions = "/t:library";
            cp.GenerateInMemory = true;

            var code = @"
using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ConditionEvaluator
{
    public class Evaluator
    {
        private static dynamic ExpandPropertyByPath(JObject json, string propertyPath)
        {
            try
            {
                string regex = @""\[\w+\]""; // match each
                var match = Regex.Match(propertyPath, regex);
                JToken currentToken = json;

                while (match.Success && currentToken != null)
                {
                    var propertyName = match.Value.Trim(new char[] { '[', ']' });

                    // If trying to get the value from an array
                    int index = 0;
                    if (currentToken is JArray && int.TryParse(propertyName, out index))
                    {
                        currentToken = (currentToken as JArray)[index];
                    }
                    else
                    {
                        currentToken = (currentToken as JObject).GetValue(propertyName);
                    }
                    match = match.NextMatch();
                }

                var jvalue = currentToken as JValue;
                if (jvalue != null)
                {
                    if (jvalue.Type == JTokenType.Boolean
                        || jvalue.Type == JTokenType.Integer
                        || jvalue.Type == JTokenType.Float)
                    {
                        return jvalue.Value;
                    }
                    else
                    {
                        return jvalue.Value.ToString();
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        public bool Evaluate(JObject evt)
        {
            return <condition>;
        }
    }
}";

            CompilerResults cr = c.CompileAssemblyFromSource(cp, code.Replace("<condition>", condition));
            if (cr.Errors.Count > 0)
            {
                return null;
            }

            System.Reflection.Assembly a = cr.CompiledAssembly;
            return a.CreateInstance("ConditionEvaluator.Evaluator");
        }

        public static bool EvaluateCondition(object evaluator, JObject json)
        {
            try
            {
                var type = evaluator.GetType();
                var method = type.GetMethod("Evaluate");
                return (bool)method.Invoke(evaluator, new[] { json });
            }
            catch
            {
            }

            return false;
        }
    }
}
