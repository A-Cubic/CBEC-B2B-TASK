using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Com.Portsoft.Framework.Database;
using System.Data;

namespace core测试.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            if (id == 99)
            {
                string ss = "select now() as now";
                DatabaseOperation.TYPE = new DBManager();
                DataTable dt = DatabaseOperation.ExecuteSelectDS(ss, "1").Tables[0];
                return dt.Rows[0][0].ToString();
            }
            if (id==999)
            {
                

                MySqlConnection my = new MySqlConnection();
                my.ConnectionString = "Database='erp';Data Source='118.190.125.175';User Id='root';Password='13161111';Character Set=utf8;port=13306;";
                MySqlCommand msqlCommand = new MySqlCommand();
                msqlCommand.Connection = my;
                try
                {
                    //define the command text  
                    msqlCommand.CommandText = "select now() as now";
                    my.Open();
                    MySqlDataReader msqlReader = msqlCommand.ExecuteReader();
                    while (msqlReader.Read())
                    {
                        return msqlReader.GetString(msqlReader.GetOrdinal("now"));
                    }

                    return "error";
                }
                catch (Exception)
                {

                }
                finally
                {
                    my.Close();
                }
                
            }
            return "value";
        }
        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
            
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
