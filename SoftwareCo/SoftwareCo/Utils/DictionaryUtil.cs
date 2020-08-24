using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SoftwareCo
{
    class DictionaryUtil
    {
        public static string TryGetStringFromDictionary(Dictionary<string, object> dict, string key)
        {
            object value = null;
            dict.TryGetValue(key, out value);
            if (value != null)
            {
                return value.ToString();
            }
            return null;
        }

        public static List<string> TryGetStringListFromDictionary(Dictionary<string, List<string>> dict, string key)
        {
            List<string> list = null;
            dict.TryGetValue(key, out list);
            if (list != null)
            {
                return list;
            }
            return null;
        }

        public static IDictionary<string, object> ConvertObjectToSource(IDictionary<string, object> dict)
        {
            dict.TryGetValue("source", out object sourceJson);
            try
            {
                IDictionary<string, object> sourceData = (sourceJson == null) ? null : (IDictionary<string, object>)sourceJson;
                return sourceData;
            }
            catch (Exception e)
            {
                //
            }
            return new Dictionary<string, object>();
        }

        public static PluginDataProject ConvertObjectToProject(IDictionary<string, object> dict)
        {
            try
            {
                PluginDataProject proj = (PluginDataProject)JsonConvert.DeserializeObject(dict["project"].ToString());
                return proj;
            }
            catch (Exception) { }

            return new PluginDataProject("Unnamed", "Untitled");
        }

        public static string ConvertObjectToString(IDictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key))
            {
                return "";
            }
            try
            {
                return Convert.ToString(dict[key]);
            }
            catch (Exception e)
            {
                //
            }
            return "";
        }

        public static long ConvertObjectToLong(IDictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key))
            {
                return 0;
            }
            try
            {
                return Convert.ToInt64(dict[key]);
            }
            catch (Exception e)
            {
                //
            }
            return 0;
        }

        public static bool ConvertObjectToBool(IDictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key))
            {
                return false;
            }
            try
            {
                return Convert.ToBoolean(dict[key]);
            }
            catch (Exception e)
            {
                //
            }
            return false;
        }

        public static double ConvertObjectToDouble(IDictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key))
            {
                return 0.0;
            }
            try
            {
                return Convert.ToDouble(dict[key]);
            }
            catch (Exception e)
            {
                //
            }
            return 0.0;
        }

        public static int ConvertObjectToInt(IDictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key))
            {
                return 0;
            }
            try
            {
                return Convert.ToInt32(dict[key]);
            }
            catch (Exception e)
            {
                //
            }
            return 0;
        }

        public static T DictionaryToObject<T>(IDictionary<string, object> dict) where T : new()
        {
            var t = new T();
            PropertyInfo[] properties = t.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (!dict.Any(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                KeyValuePair<string, object> item = dict.First(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase));

                // Find which property type (int, string, double? etc) the CURRENT property is...
                Type tPropertyType = t.GetType().GetProperty(property.Name).PropertyType;

                // Fix nullables...
                Type newT = Nullable.GetUnderlyingType(tPropertyType) ?? tPropertyType;

                // ...and change the type
                object newA = Convert.ChangeType(item.Value, newT);
                t.GetType().GetProperty(property.Name).SetValue(t, newA, null);
            }
            return t;
        }
    }
}
