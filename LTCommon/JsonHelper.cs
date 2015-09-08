using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace LTCommon
{
    public static class JsonHelper
    {
        public static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
           
        };

        static JsonHelper()
        {
            settings.Converters.Add(new StringEnumConverter());

             
        }

        public static string ToJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj, settings);
        }

        public static T FromJson<T>(this string str)
        {
            return JsonConvert.DeserializeObject<T>(str, settings);
        }
    }

}
