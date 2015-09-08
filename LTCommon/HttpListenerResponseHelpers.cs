using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LTCommon
{
    public static class HttpListenerResponseHelpers
    {
        /// <summary>
        /// Writes a Bad Request header, indicating that the client's request is malformed or violates some constraint
        /// </summary>
        public static void Write400(this HttpListenerResponse resp)
        {
            WriteJson(resp, new { Error = "Bad Request"}, 400);
        }

        /// <summary>
        /// Writes an Unauthorized header, indicating authorization may help
        /// </summary>
        public static void Write401(this HttpListenerResponse resp)
        {
            WriteJson(resp, new { Error = "Unauthorized" }, 401);
        }

        /// <summary>
        /// Writes a Forbidden header, indicating that there is content at this URL, but the client may not access it. Authorizing will not help
        /// </summary>
        public static void Write403(this HttpListenerResponse resp)
        {
            WriteJson(resp, new { Error = "Forbidden" }, 403);
        }

        /// <summary>
        /// Writes a Not Found header, indicating that the URL specified does not point to anything meaningful
        /// </summary>
        public static void Write404(this HttpListenerResponse resp)
        {
            WriteJson(resp, new { Error = "Not Found" }, 404);
        }

        /// <summary>
        /// Writes a Method Not Allowed header, indicating that the verb that was attempted is not permitted
        /// </summary>
        public static void Write405(this HttpListenerResponse resp)
        {
            WriteJson(resp, new { Error = "Method Not Allowed" }, 405);
        }

        /// <summary>
        /// Writes an Accepted header, indicating that the job has been enqueued, but it's best if we don't hang around waiting for the result
        /// </summary>
        public static void Write202(this HttpListenerResponse resp, string location)
        {
            resp.StatusCode = 202;
            resp.RedirectLocation = location;
            WriteJson(resp, new { Location = location }, 202);
        }

        /// <summary>
        /// Writes a No Content response. This generally means that some action has been performed, but there's nothing to show except that it's done
        /// </summary>
        public static void Write204(this HttpListenerResponse resp)
        {
            resp.StatusCode = 204;
        }

        public static void WriteJson(this HttpListenerResponse resp, object result, int status = 200)
        {
            resp.StatusCode = status;
            resp.ContentType = "application/json";
            using (var os = new StreamWriter(resp.OutputStream, Encoding.UTF8))
            {
                os.Write(result == null ? "null" : result.ToJson());
            }
        }
    }
}
