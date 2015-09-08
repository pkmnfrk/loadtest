using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTCommon
{
    public interface IStatus
    {
        Status Status { get; }
        int Opened { get; }
        int Closed { get; }
        int Errored { get; }

        Guid Tag { get; }
    }
}
