using Newtonsoft.Json;
using Sodium;
using System;
using System.Collections.Generic;

namespace SoftwareCo
{
    class HashManager
    {
        public static String HashValue(string value, string dataType)
        {
            if (!CacheManager.HasJwt() || string.IsNullOrEmpty(value))
            {
                return "";
            }

            try
            {
                byte[] key = GenericHash.GenerateKey(); // 64 byte key
                string hashedValue = Utilities.BinaryToHex(GenericHash.Hash(value, key, 128));
                if (CacheManager.HasCachedValue(dataType, hashedValue))
                {
                    // doesn't exist yet, encrypt it
                    EncryptValue(value, hashedValue, dataType);

                    // refresh the cache
                    PopulateHashValues();
                }
                return hashedValue;
            }
            catch (Exception e)
            {
                Console.WriteLine("SwdcVsTracker - Hash value error: {0}", e.Message);
            }
            return "";
        }

        private static async void EncryptValue(string value, string hashedValue, string dataType)
        {
            UserEncryptedData data = new UserEncryptedData();
            data.data_type = dataType;
            data.hashed_value = hashedValue;
            data.value = value;

            await Http.PostAsync("/user_encrypted_data", data);
        }

        private static async void PopulateHashValues()
        {
            Response resp = await Http.GetAsync("/hashed_values");
            if (resp.ok)
            {
                // serialize the object to the UserHashedValues object
                string json = JsonConvert.SerializeObject(resp.responseData);
                try
                {
                    // responseData will be like this: {data: {key1: ["hash1", "hash2"], key2: ["hash3", "hash4"]}}

                    Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (data != null)
                    {
                        Dictionary<string, List<string>> dictionary = (Dictionary<string, List<string>>)data["data"];
                        // go through each key and set the cache
                        foreach (string key in dictionary.Keys)
                        {
                            List<string> hashValueList = DictionaryUtil.TryGetStringListFromDictionary(dictionary, key);
                            if (hashValueList != null)
                            {
                                CacheManager.UpdateCacheValues(key, hashValueList);
                            }
                        }
                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine("SwdcVsTracker - populate cache error: {0}", e.Message);
                }
            }
        }
    }
}
