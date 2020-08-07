using System;
using System.Collections.Generic;

namespace SwdcVsTracker
{
    public class CacheManager
    {
        public static string jwt = null;
        private static Dictionary<string, List<string>> hashDict = new Dictionary<string, List<string>>();

        public static bool HasJwt()
        {
            // the jwt is not null and is not blank, return true
            return !string.IsNullOrEmpty(jwt);
        }

        public static bool HasCachedValue(string dataType, string hashedValue)
        {
            List<string> hashValues = UtilManager.TryGetStringListFromDictionary(hashDict, dataType);
            if (hashValues != null && hashValues.Contains(hashedValue))
            {
                return true;
            }
            return false;
        }

        public static void UpdateCacheValues(string dataType, List<string> hashValues)
        {
            hashDict.Add(dataType, hashValues);
        }
    }
}
