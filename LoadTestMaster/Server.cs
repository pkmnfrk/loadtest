using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using LTCommon;

namespace LoadTestMaster
{
    public class ServerSettings
    {
        public string ListenPrefix { get; set; }
    }
    public class Server
    {
        private string listenerPrefix;
        private HttpListener listener;
        private HttpListenerRouter router;
        private HashSet<string> slaves = new HashSet<string>();

        public Server(ServerSettings settings)
        {
            this.listenerPrefix = settings.ListenPrefix;
            this.router = new HttpListenerRouter
            {
                {"^/slave$", "PUT", OnPutSlave},
                {"^/slave$", "GET", OnGetSlave},
            };
        }

        public void Start()
        {
            this.listener = new HttpListener();
            this.listener.Prefixes.Add(this.listenerPrefix);
            this.listener.Start();

            Task<HttpListenerContext>.Factory.FromAsync(listener.BeginGetContext, listener.EndGetContext, null)
                 .ContinueWith(OnGetContext);
        }

        public void Stop()
        {
            
        }

        private void OnGetContext(Task<HttpListenerContext> result)
        {
            Task<HttpListenerContext>.Factory.FromAsync(listener.BeginGetContext, listener.EndGetContext, null)
                 .ContinueWith(OnGetContext);

            router.Route(result.Result);
        }

        private class ClientRequest
        {
            public string Status { get; set; }
        }

        private void OnPutSlave(HttpListenerRequest req, HttpListenerResponse res)
        {
            var dto = req.ReadBody<ClientRequest>();
            var slave = req.RemoteEndPoint.Address.ToString();

            if (dto.Status == "up")
            {
                slaves.Add(slave);
            }
            else if (dto.Status == "down")
            {
                slaves.Remove(slave);
            }

            res.Write204();
        }

        private void OnGetSlave(HttpListenerRequest req, HttpListenerResponse res)
        {
            res.WriteJson(slaves);
        }
    }

}
