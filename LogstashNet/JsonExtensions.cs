using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LogstashNet
{
    internal static class JsonExtensions
    {
        public static void AddTag(this JObject json, string tag)
        {
            var tags = json["tags"];
            if (tags == null)
            {
                json.Add("tags", new JArray(tag));
            }
            else
            {
                Debug.Assert(tags is JArray);
                ((JArray)tags).Add(tag);
            }
        }

        /// <summary>
        /// Given a propertyPath like "[property][subproperty]", return the retrieved property value. The final value must be JValue type.
        /// Return false if failed to retrieve.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="propertyPath"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryExpandPropertyByPath(this JObject json, string propertyPath, out string value)
        {
            value = null;
            try
            {
                string regex = @"\[\w+\]"; // match each
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

                if (currentToken is JValue)
                {
                    value = currentToken.ToString();

                    // String type value will lost double quote after the ToString() method, add it back at here.
                    if (currentToken.Type == JTokenType.String)
                    {
                        value = string.Format("\"{0}\"", value);
                    }
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
