using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace B2BTask.Controllers
{
    static class CKConfig
    {
        public static int smsNum = 10;
        public static string phoneNum = "";

        public static void sms(string txt)
        {
            using (var client = new WebClient())
            {
                
            }
        }
    }
}
