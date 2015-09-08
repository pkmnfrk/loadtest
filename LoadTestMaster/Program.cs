using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadTestMaster
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = new ServerSettings()
            {
                ListenPrefix = "http://+:4445/"
            };

            var server = new Server(settings);
            server.Start();

            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);

            server.Stop();

        }
    }
}
