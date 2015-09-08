using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

using LTCommon;
using System.Text.RegularExpressions;
using System.Threading;

namespace LoadTestSlave
{

    public class CnCSettings
    {
        public string LisenerPrefix { get; set; }

        public MasterProxy MasterProxy { get; set; }
    }

    public class CnC
    {
        private Thread workerThread;
        private ManualResetEvent workerSignal;

        private string listenPrefix;

        private HttpListener listener;

        Dictionary<Guid, IStatus> jobs = new Dictionary<Guid, IStatus>();

        Queue<IStatus> jobQueue = new Queue<IStatus>();
        List<IStatus> completed = new List<IStatus>();
        IStatus current = null;

        private MasterProxy master;

        private HttpListenerRouter router;

        public CnC(CnCSettings settings)
        {
            listenPrefix = settings.LisenerPrefix;
            master = settings.MasterProxy;

            router = new HttpListenerRouter
            {
                {"^/start/?", "POST", OnStart },
                {"^/status/?$", "GET", StatusGeneral },
                {"^/status/[a-fA-F0-9{}-]+$", "GET", OnStatusSpecific },
            };
        }

        public void Start()
        {
            workerSignal = new ManualResetEvent(true);

            listener = new HttpListener();

            listener.Prefixes.Add(listenPrefix);

            listener.Start();

            Task<HttpListenerContext>.Factory.FromAsync(listener.BeginGetContext, listener.EndGetContext, null)
                .ContinueWith(OnGetContext);

            workerThread = new Thread(WorkerThread);
            workerThread.Start();
        }

        public void Stop()
        {
            //listener.Stop(); // the process is about to go away anyway...
            workerThread.Abort();
        }

        private void OnGetContext(Task<HttpListenerContext> result)
        {
            Task<HttpListenerContext>.Factory.FromAsync(listener.BeginGetContext, listener.EndGetContext, null)
                 .ContinueWith(OnGetContext);

            var context = result.Result;

            router.Route(context);
        }

        private void OnStatusSpecific(HttpListenerRequest req, HttpListenerResponse resp)
        {
            if (req.HttpMethod != "GET")
            {
                resp.Write405();
            }

            var g = req.Url.AbsolutePath.Substring(8);

            Guid job;

            if (!Guid.TryParse(g, out job) || !jobs.ContainsKey(job))
            {
                resp.Write404();
                return;
            }

            resp.WriteJson(new Result(jobs[job]));
                
            
        }

        private void StatusGeneral(HttpListenerRequest req, HttpListenerResponse resp)
        {
            IList<Guid> queued, comp;

            lock (jobQueue)
            {
                queued = jobQueue.Select(j => j.Tag).ToList();
            }

            lock (completed)
            {
                comp = completed.Select(c => c.Tag).ToList();
            }

            var cur = current;

            resp.WriteJson(new
            {
                enqueued = queued,
                current = cur != null ? (Guid?)cur.Tag : (Guid?)null,
                completed = comp
            });
        }

        private void OnStart(HttpListenerRequest req, HttpListenerResponse resp)
        {

            if (req.HttpMethod != "POST")
            {
                resp.Write405();
                return;
            }

            if (!req.HasEntityBody)
            {
                resp.Write400();
                return;
            }

            CnCStartRequest args;

            using (var ist = new StreamReader(req.InputStream))
            {
                var body = ist.ReadToEnd();
                args = body.FromJson<CnCStartRequest>();
            }
            if (args.job == Guid.Empty)
            {
                args.job = Guid.NewGuid();
            }

            if (args.job == Guid.Empty || jobs.ContainsKey(args.job))
            {
                resp.Write400();
                return;
            }

            if (args.type == "http")
            {
                if (args.http == null)
                {
                    resp.Write400();
                    return;
                }

                args.http.Tag = args.job;

                var http = new HttpLoader(args.http);
                jobs[args.job] = http;

                lock (jobQueue)
                {
                    jobQueue.Enqueue(http);
                }

                workerSignal.Set();

                //speculatively wait a few seconds, in case the job is done real quick like
                Thread.Sleep(2000);

                resp.WriteJson(new
                {
                    Location = "/status/" + args.job,
                    Data = new Result(http)
                }, http.Status == Status.Complete ? 200 : 202);

                
            }
            else
            {
                resp.Write400();
                return;
            }
            
        }


        private void WorkerThread()
        {
            Thread.CurrentThread.Name = "Job Manager";
            try
            {
                while (true)
                {
                    while (!workerSignal.WaitOne()) ;

                    workerSignal.Reset();

                    IJob job = null;

                    while (true)
                    {
                        lock (jobQueue)
                        {
                            if (jobQueue.Count > 0)
                            {
                                job = jobQueue.Dequeue() as IJob;
                            }
                        }

                        if (job != null)
                        {
                            job.Start();

                            while (!job.Complete.WaitOne()) ;

                            lock (completed)
                            {
                                completed.Add((IStatus)job);
                            }

                            master.NotifyResults(new Result((IStatus)job));

                            job = null;
                        }
                        else
                        {
                            break;
                        }
                    }

                }
            }
            catch (ThreadAbortException)
            {

            }
        }
    }

    public class CnCStartRequest
    {
        public Guid job { get; set; }
        public string type { get; set; }
        public HttpLoaderSettings http { get; set; }
    }
}
