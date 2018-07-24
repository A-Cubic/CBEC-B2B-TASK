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
        private string str = "1123";
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
