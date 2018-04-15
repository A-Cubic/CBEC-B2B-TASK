using Com.Portsoft.Framework.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace core测试.Controllers
{
    public class DBManager : IType
    {
        private DBType dbt;
        private string str = "Database='erp';Data Source='118.190.125.175';User Id='root';Password='13161111';Character Set=utf8;port=13306;";
        //private static string str4 = "Database='xiaoman_s';Data Source='112.126.92.32';User Id='root';Password='H12#ds@15hSD';Character Set=utf8;";
        //private static string str9 = "Database='xiaoman_j';Data Source='112.126.92.32';User Id='root';Password='H12#ds@15hSD';Character Set=utf8;";
        //private static string str9 = "Database='xiaoman_j';Data Source='112.126.92.32';User Id='root';Password='H12#ds@15hSD';Character Set=utf8;";
        public DBManager()
        {
            this.dbt = DBType.Mysql;
        }
        public DBManager(string Version)
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
