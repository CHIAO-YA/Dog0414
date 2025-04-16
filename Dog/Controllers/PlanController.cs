using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Dog.Controllers
{
    public class PlanController : ApiController
    {
        Models.Model1 db = new Models.Model1();

        [HttpGet]//取資料 取得所有Discount
        [Route("GET/user/plans")]
        public IHttpActionResult Getplans()
        {
            var Plans = db.Plans.Select(d => new
            {
                d.PlanID,
                d.PlanName,
                d.Liter, 
                d.Price,
                d.PlanKG,
                d.PlanDescription,
                d.PlanPeople
            }).ToList();

            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得",
                Plans
            });
        }
    }
}
