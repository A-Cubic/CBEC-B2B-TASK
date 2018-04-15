using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Com.Portsoft.Framework.Database;
using Pomelo.AspNetCore.TimedJob;

namespace core测试.Controllers
{
    public class TestJob:Job
    {
        // Begin 起始时间；Interval执行时间间隔，单位是毫秒，建议使用以下格式，此处为3小时；SkipWhileExecuting是否等待上一个执行完成，true为等待；
        // [Invoke(Begin = "2016-11-29 22:10", Interval = 1000 * 3600 * 3, SkipWhileExecuting = true)]
        [Invoke(Begin = "2016-11-29 22:10", Interval = 1000 * 10 , SkipWhileExecuting = true)]
        public void Run()
        {
            string ss = "select now() as now";
            DatabaseOperation.TYPE = new DBManager();
            DataTable dt = DatabaseOperation.ExecuteSelectDS(ss, "1").Tables[0];
            //Job要执行的逻辑代码

            //LogHelper.Info("Start crawling");
            //AddToLatestMovieList(100);
            //AddToHotMovieList();
            //LogHelper.Info("Finish crawling");
        }
    }
}
