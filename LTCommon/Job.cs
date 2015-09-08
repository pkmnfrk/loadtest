using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTCommon
{
    public class Job : IStatus
    {
        public Guid Tag { get; set; }
        public List<Result> SubResults { get; private set; }

        public Dictionary<DateTime, string> Log { get; set; }

        private IEnumerable<int> Zero = new[] { 0 };

        public Status Status
        {
            get {
                if (SubResults.All(s => s.Status == Status.Complete))
                {
                    return Status.Complete;
                }
                else if (SubResults.All(s => s.Status == Status.Enqueued))
                {
                    return Status.Enqueued;
                }
                else
                {
                    return Status.Started;
                }
            }
        }

        public int Opened
        {
            get {
                return SubResults.Select(s => s.Opened).Union(Zero).Sum();
            }
        }

        public int Closed
        {
            get
            {
                return SubResults.Select(s => s.Closed).Union(Zero).Sum();
            }
        }

        public int Errored
        {
            get
            {
                return SubResults.Select(s => s.Errored).Union(Zero).Sum();
            }
        }
    }

    public class Result : IStatus
    {
        public Guid Tag { get; set; }
        public Status Status { get; set; }

        public int Opened { get; set; }
        public int Closed { get; set; }
        public int Errored { get; set; }

        public Dictionary<DateTime, string> Log { get; set; }

        public Result(IStatus src)
        {
            this.Status = src.Status;
            this.Opened = src.Opened;
            this.Closed = src.Closed;
            this.Errored = src.Errored;
            this.Tag = src.Tag;
        }

    }
}

