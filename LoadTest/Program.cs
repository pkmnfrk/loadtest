using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Net;

namespace LoadTestSlave
{
    class Program
    {


        static void Main(string[] args)
        {

            ServicePointManager.ServerCertificateValidationCallback += (a, b, c, d) => true;

            var master = new MasterProxy();

            var settings = new CnCSettings
            {
                LisenerPrefix = "http://+:4444/",
                MasterProxy = master
            };

            var server = new CnC(settings);

            server.Start();

            //now that the web interface is up, we can notify our master
            master.NotifyUp();

#if !MONO
            Console.WriteLine("Press any key to quit");
            Console.ReadKey(true);
#else
            System.Threading.Thread.Sleep(); //zzz until all goes away...
#endif

            master.NotifyDown();

            server.Stop();
        }

    }
}
