using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LTCommon;

namespace LoadTestSlave
{
    public class MasterProxy
    {
        //private string masterUrl = "https://mike-caron.com/load/api";
        private string masterUrl = "http://localhost:4445/";

        public void NotifyUp()
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.UploadString(masterUrl + "/slave", "PUT", "{\"status\":\"up\"}");
                }
            }
            catch (Exception) { }
        }

        public void NotifyDown()
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.UploadString(masterUrl + "/slave", "PUT", "{\"status\":\"down\"}");
                }

            }
            catch (Exception) { }
        }

        public void NotifyResults(Result result)
        {
            try
            {
                var data = result.ToJson();

                using (var wc = new WebClient())
                {
                    wc.Headers.Add("Content-Type", "application/json");
                    wc.UploadString(masterUrl + "/slave/job/" + result.Tag, "PUT", data);
                }
            }
            catch (Exception) { }
        }
    }
}
