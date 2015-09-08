using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LTCommon;

namespace LoadTestSlave
{
    public class HttpLoaderSettings
    {
        public int ConcurrentConnections { get; set; }
        public int TotalRequests { get; set; }
        public string URL { get; set; }
        public Guid Tag { get; set; }
    }

    public class HttpLoader : IStatus, IJob
    {
        private int reqCount;
        private int concurrent;
        private Task[] requests;
        private string url;

        private ManualResetEvent notify;

        public event Action<Exception> OnError;

        public ManualResetEvent Complete { get; private set; }

        private int _opened, _closed, _error;

        public Guid Tag { get; private set; }
        public Status Status { get; private set; }


        public int Opened
        {
            get
            {
                return _opened;
            }
        }

        public int Closed
        {
            get
            {
                return _closed;
            }
        }

        public int Errored
        {
            get
            {
                return _error;
            }
        }

        public HttpLoader(HttpLoaderSettings settings)
        {
            this.concurrent = settings.ConcurrentConnections;
            this.reqCount = settings.TotalRequests;
            this.requests = new Task[this.concurrent];

            this.url = settings.URL;

            this.notify = new ManualResetEvent(false);
            this.Complete = new ManualResetEvent(false);

            this.Tag = settings.Tag;
            this.Status = Status.Enqueued;
        }

        public void Start()
        {
            this.Status = Status.Started;
            ThreadPool.QueueUserWorkItem((a) =>
            {
                
                BeginConnections();

                while (true)
                {
                    while (!notify.WaitOne()) ;

                    notify.Reset();
                    if (requests.All(r => r == null))
                    {
                        this.Status = Status.Complete;
                        Complete.Set();
                    }
                }
            });
            
        }


        private void TriggerOnOpened()
        {
            Interlocked.Increment(ref _opened);
        }

        private void TriggerOnClosed()
        {
            Interlocked.Increment(ref _closed);
        }

        private void TriggerOnError(Exception ex)
        {
            Interlocked.Increment(ref _error);
            if (ex is AggregateException)
            {
                ex = ex.InnerException;
            }
            Console.Write(ex);

            var onErr = this.OnError;

            if (onErr != null)
            {
                lock (this)
                {
                    onErr(ex);
                }
            }
        }

        private void BeginConnections()
        {
            var factory = Task<WebResponse>.Factory;

            for (var i = 0; i < this.concurrent; i++)
            {
                if (!OpenConnection(i)) break;
            }
        }

        private void OnResponse(Task<WebResponse> result)
        {
            var i = (int)result.AsyncState;
            

            try
            {
                var resp = (HttpWebResponse)result.Result;
                resp.Dispose();
            }
            catch (Exception ex)
            {
                TriggerOnError(ex);
            }
            TriggerOnClosed();
            requests[i] = null;

            OpenConnection(i);

        }

        private bool OpenConnection(int i)
        {
            if (this.reqCount <= 0)
            {
                notify.Set();
                return false;
            }

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.KeepAlive = true;
            req.ServerCertificateValidationCallback += (a, b, c, d) => true;
            req.AllowAutoRedirect = false;
            var task = Task<WebResponse>.Factory.FromAsync(req.BeginGetResponse, req.EndGetResponse, (object)i);
            task.ContinueWith(OnResponse);

            requests[i] = task;
            Interlocked.Decrement(ref this.reqCount);

            TriggerOnOpened();

            return true;
        }

    }
}
