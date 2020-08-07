using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sodium;

namespace SwdcVsTracker
{
    public class HashManager
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
                    Dictionary<string, List<string>> dictionary = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);

                    // go through each key and set the cache
                    foreach (string key in dictionary.Keys)
                    {
                        List<string> hashValueList = UtilManager.TryGetStringListFromDictionary(dictionary, key);
                        if (hashValueList != null)
                        {
                            CacheManager.UpdateCacheValues(key, hashValueList);
                        }
                    }
                } catch (Exception e)
                {
                    Console.WriteLine("SwdcVsTracker - populate cache error: {0}", e.Message);
                }
            }
        }
    }
}
