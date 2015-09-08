using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace LTCommon
{

    using Route = Action<HttpListenerRequest, HttpListenerResponse>;
    using Filter = Func<HttpListenerRequest, HttpListenerResponse, bool>;
    using System.Text.RegularExpressions;

    public class HttpListenerRouter : IEnumerable<Tuple<string, string, Route>>
    {
        private List<Tuple<string, string, Route>> routes = new List<Tuple<string, string, Route>>();
        private List<Filter> before = new List<Filter>();
        private List<Filter> after = new List<Filter>();
        
        public void Add(string prefix, string method, Route action)
        {
            routes.Add(Tuple.Create(prefix, method.ToUpper(), action));
        }

        public void AddPreFilter(Filter action)
        {
            before.Add(action);
        }

        public void AddPostFilter(Filter action)
        {
            after.Add(action);
        }

        public void Route(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            bool done = false;

            foreach (var filter in before)
            {
                if (filter(req, res))
                {
                    done = true;
                    break;
                }
            }

            if (!done)
            {
                foreach (var route in routes)
                {
                    if (route.Item2 == req.HttpMethod)
                    {
                        if (Regex.IsMatch(req.Url.AbsolutePath, route.Item1)) {
                            route.Item3(req, res);
                            break;
                        }
                    }
                }
            }

            foreach (var filter in after)
            {
                if (filter(req, res))
                {
                    break;
                }
            }

            res.Close();
        }

        public IEnumerator<Tuple<string, string, Route>> GetEnumerator()
        {
            return routes.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)routes).GetEnumerator();
        }
    }
}
