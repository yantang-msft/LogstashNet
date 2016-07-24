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

        public static bool EvaluateCondition(JObject json, string condition)
        {
            try
            {
                var cond = condition;

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
                    string value = null;
                    if (json.TryExpandPropertyByPath(propertyPath, out value))
                    {
                        cond = cond.Replace(propertyPath, value);
                    }
                    else
                    {
                        return false;
                    }
                }

                var result = Eval(cond);

                return result is bool ? (bool)result : false;
            }
            catch
            {
                return false;
            }
        }

        private static object Eval(string sCSCode)
        {
            CSharpCodeProvider c = new CSharpCodeProvider();
            CompilerParameters cp = new CompilerParameters();

            cp.ReferencedAssemblies.Add("system.dll");
            cp.ReferencedAssemblies.Add("system.xml.dll");
            cp.ReferencedAssemblies.Add("system.data.dll");
            cp.ReferencedAssemblies.Add("system.windows.forms.dll");
            cp.ReferencedAssemblies.Add("system.drawing.dll");

            cp.CompilerOptions = "/t:library";
            cp.GenerateInMemory = true;

            StringBuilder sb = new StringBuilder("");
            sb.Append("using System;\n");
            sb.Append("using System.Xml;\n");
            sb.Append("using System.Data;\n");
            sb.Append("using System.Data.SqlClient;\n");
            sb.Append("using System.Windows.Forms;\n");
            sb.Append("using System.Drawing;\n");

            sb.Append("namespace CSCodeEvaler{ \n");
            sb.Append("public class CSCodeEvaler{ \n");
            sb.Append("public object EvalCode(){\n");
            sb.Append("return " + sCSCode + "; \n");
            sb.Append("} \n");
            sb.Append("} \n");
            sb.Append("}\n");
            
            CompilerResults cr = c.CompileAssemblyFromSource(cp, sb.ToString());
            if (cr.Errors.Count > 0)
            {
                return false;
            }

            System.Reflection.Assembly a = cr.CompiledAssembly;
            object o = a.CreateInstance("CSCodeEvaler.CSCodeEvaler");

            Type t = o.GetType();
            MethodInfo mi = t.GetMethod("EvalCode");

            object s = mi.Invoke(o, null);
            return s;
        }
    }
}
