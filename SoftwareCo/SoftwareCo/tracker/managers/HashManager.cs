using Konscious.Security.Cryptography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SoftwareCo
{
    class HashManager
    {
        private static HMACBlake2B blake2b = null;

        public static string ByteArrayToHexString(byte[] Bytes)
        {
            StringBuilder Result = new StringBuilder(Bytes.Length * 2 + 1);
            string HexAlphabet = "0123456789ABCDEF";

            foreach (byte B in Bytes)
            {
                Result.Append(HexAlphabet[(int)(B >> 4)]);
                Result.Append(HexAlphabet[(int)(B & 0xF)]);
            }

            return Result.ToString();
        }

        public static string HashValue(string value, string dataType)
        {
            if (!CacheManager.HasJwt() || string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (blake2b == null)
            {
                blake2b = new HMACBlake2B(512);
                blake2b.Initialize();
            }

            try
            {
                byte[] hashedBytes = blake2b.ComputeHash(Encoding.UTF8.GetBytes(value));
                string hashedValue = ByteArrayToHexString(hashedBytes).ToLower();

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
