using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Dog.Controllers
{
    public class AdminController : ApiController
    {
        Models.Model1 db = new Models.Model1();

        [HttpGet]
        [Route("GET/admin/delivers")]//指定日期內所有接單員（司機）的訂單狀況
        public IHttpActionResult GetAdmin(DateTime? date = null)
        {
            //var result = db.Admins.ToList();
            return Ok(new
            {
                StatusCode = 200,
                status = "成功取得",
                //result
            });
        }
    }
}
