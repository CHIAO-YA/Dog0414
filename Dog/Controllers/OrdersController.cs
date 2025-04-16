using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Dog.Migrations;
using Dog.Models;

namespace Dog.Controllers
{
    public class OrdersController : ApiController
    {
        Models.Model1 db = new Models.Model1();

        [HttpGet]//取資料 取得所有訂單的 地址（Addresses）、經度（Longitude）、緯度（Latitude）
        [Route("locations")]
        public IHttpActionResult GetOrdersLotions()
        {   //取orders表中區要的欄位
            var result = db.Orders.ToList();
            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得",
                result
            });// 回傳 200 OK，並附上訂單資料
        }

        [HttpGet]//取單筆資料
        [Route("locations/{id}")]
        public IHttpActionResult GetOrdersLotion(int id)
        {
            var orders = db.Orders.FirstOrDefault(o => o.OrdersID == id);
            var result = new
            {
                Status = HttpStatusCode.OK,
                msg = "成功取得",
               orders
            };
            if (orders == null) { return NotFound(); }//找不到資料
            return Ok(result);
        }

        [HttpPost]
        [Route("locations")]//新增訂單
        public IHttpActionResult CreateOrder([FromBody] Orders orders)
        {
            if (!ModelState.IsValid) //檢查請求是否有效
            {
                return BadRequest(ModelState); //如果請求無效，回傳 400 錯誤
            }
            string validWeekDays = ValidateAndFormatWeekDays(orders.WeekDay);
            if (validWeekDays == null)
            {
                return BadRequest("WeekDay 格式錯誤，請使用 1~7 代表星期日到星期六，並用逗號分隔。");
            }
            orders.WeekDay = validWeekDays; // 轉換後的格式
            // **統一使用 UTC 時間**
            var currentTimeUtc = DateTime.UtcNow;
            orders.CreatedAt = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);
            orders.UpdatedAt = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);

            // **計算 EndDate**
            var discount = db.Discount.FirstOrDefault(d => d.DiscountID == orders.DiscountID);
            if (discount != null && orders.StartDate.HasValue)
            {
                orders.EndDate = orders.StartDate.Value.AddMonths(discount.Months);
            }
            // **轉換成台灣時間**
            TimeZoneInfo taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            DateTime createdAtTaiwan = TimeZoneInfo.ConvertTimeFromUtc(orders.CreatedAt.Value, taiwanTimeZone);
            DateTime updatedAtTaiwan = TimeZoneInfo.ConvertTimeFromUtc(orders.UpdatedAt.Value, taiwanTimeZone);
            //DateTime? reportedAtTaiwan = orders.ReportedAt.HasValue
            //? TimeZoneInfo.ConvertTimeFromUtc(orders.ReportedAt.Value, taiwanTimeZone)
            //: (DateTime?)null;

            //var result = new
            //{
            //    Status = HttpStatusCode.OK,
            //    msg = "成功",
            //    OrderName = orders.OrderName,
            //    OrderPhone = orders.OrderPhone,
            //    Addresses = orders.Addresses,
            //    Longitude = orders.Longitude,
            //    Latitude = orders.Latitude,
            //    Notes = orders.Notes,
            //    WeekDay = orders.WeekDay,//合併字
            //    StartDate = orders.StartDate,
            //    EndDate = orders.EndDate,
            //    CreatedAt = createdAtTaiwan,
            //    UpdatedAt = updatedAtTaiwan,
            //    OrderStatus = orders.OrderStatus.ToString(),//enum
            //    PaymentStatus = orders.PaymentStatus.ToString(),//enum
            //    KG = orders.KG,
            //    IssueDescription = orders.IssueDescription,
            //    ReportedAt = orders.ReportedAt// 只有非 null 才會轉換
            //};
            db.Orders.Add(orders);
            db.SaveChanges();
            return Ok(new
            {
                StatusCode = 200,
                status = "新增成功",
                orders
            });
        }
        // 驗證並格式化 WeekDay 欄位
        private string ValidateAndFormatWeekDays(string weekDay)
        {
            if (string.IsNullOrEmpty(weekDay)) return null;

            // 允許的星期數字 (1=星期一, ..., 7=星期日)
            var validDays = new HashSet<string> { "1", "2", "3", "4", "5", "6", "7" };

            // 分割輸入的字串
            var days = weekDay.Split(',').Select(d => d.Trim()).ToList();

            // 確保所有數字都在 1~7 範圍內
            if (days.All(d => validDays.Contains(d)))
            {
                return string.Join(",", days.OrderBy(d => d)); // 重新排序
            }

            return null; // 格式錯誤
        }

        [HttpPut]//更新某筆訂單的 地址、經緯度
        [Route("locations/{id}")]
        public IHttpActionResult UpdateOrder(int id, [FromBody] Orders orders)
        {
            var Data = db.Orders.FirstOrDefault(o => o.OrdersID == id);
            if (!ModelState.IsValid) //檢查請求是否有效
            {
                return BadRequest(ModelState); //如果請求無效，回傳 400 錯誤
            }
            string validWeekDays = ValidateAndFormatWeekDays(orders.WeekDay);
            if (validWeekDays == null)
            {
                return BadRequest("WeekDay 格式錯誤，請使用 1~7 代表星期日到星期六，並用逗號分隔。");
            }
            orders.WeekDay = validWeekDays; // 轉換後的格式
            // 使用 UTC 時間
            var currentTimeUtc = DateTime.UtcNow;
            orders.CreatedAt = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);
            orders.UpdatedAt = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);

            // **如果 ReportedAt 為 null，不要賦值**
            if (orders.ReportedAt.HasValue)
            {
                orders.ReportedAt = DateTime.SpecifyKind(orders.ReportedAt.Value, DateTimeKind.Utc);
            }

            // **計算 EndDate**
            var discount = db.Discount.FirstOrDefault(d => d.DiscountID == orders.DiscountID);
            if (discount != null && orders.StartDate.HasValue)
            {
                orders.EndDate = orders.StartDate.Value.AddMonths(discount.Months);
            }

            // **轉換成台灣時間**
            TimeZoneInfo taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            DateTime createdAtTaiwan = TimeZoneInfo.ConvertTimeFromUtc(orders.CreatedAt.Value, taiwanTimeZone);
            DateTime updatedAtTaiwan = TimeZoneInfo.ConvertTimeFromUtc(orders.UpdatedAt.Value, taiwanTimeZone);
            DateTime? reportedAtTaiwan = orders.ReportedAt.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(orders.ReportedAt.Value, taiwanTimeZone)
            : (DateTime?)null;

            // 更新可變更的欄位
            Data.OrderName = orders.OrderName;
            Data.OrderPhone = orders.OrderPhone;
            Data.Addresses = orders.Addresses;
            Data.Longitude = orders.Longitude;
            Data.Latitude = orders.Latitude;
            Data.Notes = orders.Notes;
            Data.WeekDay = orders.WeekDay;
            Data.StartDate = orders.StartDate;// 使用者選擇的開始日期
            Data.EndDate = orders.EndDate;
            Data.UpdatedAt = currentTimeUtc; // 只更新 UpdatedAt，不動 CreatedAt
            Data.OrderStatus = orders.OrderStatus;
            Data.PaymentStatus = orders.PaymentStatus;
            Data.KG = orders.KG;
            Data.IssueDescription = orders.IssueDescription;

            // **只有 ReportedAt 有值時才更新**
            if (orders.ReportedAt.HasValue)
            {
                Data.ReportedAt = orders.ReportedAt;
            }

            var result = new
            {
                Status = HttpStatusCode.OK,
                msg = "成功編輯",
                id = id,
                OrderName = Data.OrderName,
                OrderPhone = Data.OrderPhone,
                Addresses = Data.Addresses,
                Longitude = Data.Longitude,
                Latitude = Data.Latitude,
                Notes = Data.Notes,
                WeekDay = Data.WeekDay,
                StartDate = Data.StartDate,
                EndDate = Data.EndDate,
                UpdatedAt = updatedAtTaiwan,
                OrderStatus = Data.OrderStatus.ToString(), // enum 轉字串
                PaymentStatus = Data.PaymentStatus.ToString(), // enum 轉字串
                KG = Data.KG,
                IssueDescription = Data.IssueDescription,
                ReportedAt = reportedAtTaiwan // 只有非 null 才會轉換
            };
            db.SaveChanges();
            return Ok(result);
        }

        [HttpDelete]//刪除某筆訂單
        [Route("locations/{id}")]
        public IHttpActionResult DeleteOrder(int id)
        {
            var orders = db.Orders.FirstOrDefault(o => o.OrdersID == id);
            if (orders == null) { return NotFound(); }
            db.Orders.Remove(orders);
            db.SaveChanges();
            return Ok(new { Status = HttpStatusCode.OK, msg = "成功刪除", id = id });
        }
    }
}
