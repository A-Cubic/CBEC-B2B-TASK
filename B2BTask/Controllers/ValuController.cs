﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using Com.ACBC.Framework.Database;

namespace core测试.Controllers
{
    [Route("api/[controller]")]
    public class ValuController : Controller
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
                DatabaseOperationWeb.TYPE = new B2BDBManager();
                DataTable dt = DatabaseOperationWeb.ExecuteSelectDS(ss, "1").Tables[0];
                return dt.Rows[0][0].ToString();
            }
            if (id==999)
            {
                
                
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
