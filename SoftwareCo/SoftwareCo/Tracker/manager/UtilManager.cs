using System.Collections.Generic;

namespace SwdcVsTracker
{
    public class UtilManager
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
    }
}
