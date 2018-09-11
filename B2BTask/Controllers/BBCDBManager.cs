using Com.ACBC.Framework.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace core测试.Controllers
{
    public class BBCDBManager : IType
    {
        private DBType dbt;
        private string str = "";
        public BBCDBManager()
        {
            var url = System.Environment.GetEnvironmentVariable("B2BDBUrl");
            var uid = System.Environment.GetEnvironmentVariable("B2BDBUser");
            var port = System.Environment.GetEnvironmentVariable("B2BDBPort");
            var passd = System.Environment.GetEnvironmentVariable("B2BDBPassword");

            this.str = "Server=" + url
                     + ";Port=" + port
                     + ";Database=new-eshop;Uid=" + uid
                     + ";Pwd=" + passd
                     + ";CharSet=utf8;";
            //Console.Write(this.str);
            this.dbt = DBType.Mysql;
        }
        public BBCDBManager(string Version)
        {
            this.dbt = DBType.Mysql;
        }
        public DBType getDBType()
        {
            return dbt;
        }

        public string getConnString()
        {
            return str;
        }

        public void setDB(DBType s)
        {
        }
        public void setConnString(string s)
        {
            this.str = s;
        }
    }
}
