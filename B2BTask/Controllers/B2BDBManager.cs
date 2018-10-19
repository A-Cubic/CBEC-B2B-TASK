using Com.ACBC.Framework.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace core测试.Controllers
{
    public class B2BDBManager : IType
    {
        private DBType dbt;
        private string str = "1123";
        public B2BDBManager()
        {
#if DEBUG
            var url = System.Environment.GetEnvironmentVariable("FWQDBUrl", EnvironmentVariableTarget.User);
            var uid = System.Environment.GetEnvironmentVariable("MysqlDBUser", EnvironmentVariableTarget.User);
            var port = System.Environment.GetEnvironmentVariable("FWQDBPort", EnvironmentVariableTarget.User);
            var passd = System.Environment.GetEnvironmentVariable("FWQDBPassword", EnvironmentVariableTarget.User);
#endif
#if !DEBUG
            var url = System.Environment.GetEnvironmentVariable("MysqlDBUrl");
            var uid = System.Environment.GetEnvironmentVariable("MysqlDBUser");
            var port = System.Environment.GetEnvironmentVariable("MysqlDBPort");
            var passd = System.Environment.GetEnvironmentVariable("MysqlDBPassword");
            //this.str = "Database='llwell';Data Source='"+url+ "';User Id='" + uid + "';Password='" + passd + "';Character Set=utf8;port=" + port + ";";
#endif

            this.str = "Server=" + url
                     + ";Port=" + port
                     + ";Database=llwell;Uid=" + uid
                     + ";Pwd=" + passd
                     + ";CharSet=utf8;";
            //Console.Write(this.str);
            this.dbt = DBType.Mysql;
        }
        public B2BDBManager(string Version)
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
