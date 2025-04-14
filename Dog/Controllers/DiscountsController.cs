using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Dog.Controllers
{
    public class DiscountsController : ApiController
    {
        Models.Model1 db = new Models.Model1();

        [HttpGet]//取資料 取得所有Discount
        [Route("GET/user/discounts")]
        public IHttpActionResult GetDiscount()
        {
            // 使用投影查詢，只選擇需要的屬性
            var Discount = db.Discount.Select(d => new
            {
                DiscountID = d.DiscountID,
                Months = d.Months,
                DiscountRate = d.DiscountRate // 不包含 Orders 導航屬性
            }).ToList();

            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得",
                Discount
            });
        }

    }
}
