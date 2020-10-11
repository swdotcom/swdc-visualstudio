using System;
using System.Collections.Generic;

namespace SoftwareCo
{
    class CacheManager
    {
        public static string jwt = null;
        private static Dictionary<string, List<string>> hashDict = new Dictionary<string, List<string>>();
        private static Dictionary<string, CacheValue> cmdResultMap = new Dictionary<string, CacheValue>();

        public static bool HasJwt()
        {
            // the jwt is not null and is not blank, return true
            return !string.IsNullOrEmpty(jwt);
        }

        public static bool HasCachedValue(string dataType, string hashedValue)
        {
            List<string> hashValues = DictionaryUtil.TryGetStringListFromDictionary(hashDict, dataType);
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

        private static string getCmdCacheKey(string projectDir, string cmd)
        {
            return projectDir + ":" + cmd;
        }

        public static void UpdateCmdResult(string projectDir, string cmd, List<string> result)
        {
            DateTimeOffset offset = DateTimeOffset.Now;
            CacheValue v = new CacheValue();
            v.unix_time = offset.ToUnixTimeSeconds();
            v.value = result;
            cmdResultMap.Add(getCmdCacheKey(projectDir, cmd), v);
        }

        public static List<string> GetCmdResultCachedValue(string projectDir, string cmd)
        {
            string key = getCmdCacheKey(projectDir, cmd);
            CacheValue cacheValue;
            cmdResultMap.TryGetValue(key, out cacheValue);
            if (cacheValue != null)
            {
                DateTimeOffset offset = DateTimeOffset.Now;
                if (offset.ToUnixTimeSeconds() - cacheValue.unix_time > 60 * 60)
                {
                    // clear the cache itme, it's been an hour or longer
                    cmdResultMap.Remove(key);
                }
                return cacheValue.value;

            }
            return null;
        }
    }
}
