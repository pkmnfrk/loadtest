using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LTCommon
{
    public static class HttpListenerRequestHelpers
    {
        public static string ReadBody(this HttpListenerRequest req)
        {
            using (var ir = new StreamReader(req.InputStream, Encoding.UTF8))
            {
                return ir.ReadToEnd();
            }
        }

        public static T ReadBody<T>(this HttpListenerRequest req)
        {
            return req.ReadBody().FromJson<T>();
        }
    }
}
