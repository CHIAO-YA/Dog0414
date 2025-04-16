using Dog.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.Entity;
using System.Globalization;
using System.Web.Http.Results;

namespace Dog.Controllers
{
    public class UserfirstController : ApiController
    {
        Models.Model1 db = new Models.Model1();
        //使用 UsersID 作為參數  顯示該用戶的所有資訊
        //使用 OrdersID 作為參數  只關注特定一筆訂單

        [HttpGet]
        [Route("GET/user/dashboard/today/{UsersID}")]//客戶當天一筆訂單狀態
        public IHttpActionResult GetUserToday(int UsersID)
        {
            var name = db.Users.FirstOrDefault(u => u.UsersID == UsersID).LineName;//取得用戶名稱
            var today = DateTime.Today;//取得今天日期
            //補一個收訂單的時間

            var todayOrderDetails = db.OrderDetails// 查詢該用戶當天的訂單
           .Where(od => od.Orders.UsersID == UsersID && DbFunctions.TruncateTime(od.ServiceDate) == today.Date).ToList();//找出所有UsersID的訂單中，日期是今天的訂單詳情記錄，並將結果轉換成列表形式

            // 如果當天沒有訂單，回傳「今日無訂單」//當某個狀態為 0 時，可以選擇不回傳該欄位
            if (!todayOrderDetails.Any())
            {
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = "今日無訂單",
                    result = new
                    {
                        name,
                        date = today.ToString("yyyy/MM/dd"),
                        statusMessage = "無訂單"
                    }
                });
            }
            // 取得第一筆訂單詳情
            var todayOrder = todayOrderDetails.FirstOrDefault();
            //當天有訂單時，只回傳有出現的狀態  
            var statusResult = new Dictionary<string, object>
            {
                { "name", name },
                { "date", today.ToString("yyyy/MM/dd") },
                { "total", todayOrderDetails.Count }
            };

            // 依據訂單狀態，加入對應的狀態描述
            switch (todayOrder.OrderStatus)
            {
                case OrderStatus.未完成:
                    statusResult["status"] = "未完成";
                    break;
                case OrderStatus.前往中:
                    statusResult["status"] = "前往中";
                    break;
                case OrderStatus.已完成:
                    statusResult["status"] = "已完成";
                    break;
                case OrderStatus.異常回報:
                    statusResult["status"] = "異常回報";
                    break;
                case OrderStatus.已取消:
                    statusResult["status"] = "已取消";
                    break;
            }

            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得",
                result = statusResult
            });
        }
        [HttpGet]
        [Route("GET/user/dashboard/New/{UsersID}")]//客戶所有有效訂單
        public IHttpActionResult GetUserOrder(int UsersID)
        {   // 查詢該用戶的訂單，只要訂單詳情中有任何一筆是「未完成」或「前往中」狀態，就視為有效訂單
            var ValidOrders = db.Orders.Include("OrderDetails").Include("Plan").Where(o => o.UsersID == UsersID && o.OrderDetails.Any(od => od.OrderStatus == OrderStatus.未完成 || od.OrderStatus == OrderStatus.前往中)).ToList();
            // 取得這些訂單的所有照片，避免多次查詢
            var orderPhotos = db.Photo.Where(p => ValidOrders.Select(o => o.OrdersID).Contains(p.OrdersID)).ToLookup(p => p.OrdersID);
        var result = ValidOrders.Select(o =>
        {
            // 先找出下一個服務日期
            var nextServiceDate = o.OrderDetails.Where(od => (int)od.OrderStatus == (int)OrderStatus.未完成 && od.ServiceDate > DateTime.Now).OrderBy(od => od.ServiceDate).Select(od => od.ServiceDate).FirstOrDefault();
            //PlanName.NextServiceDate.StartDate.EndDate.剩餘的次數
            return new
            {
                o.OrdersID,
                o.OrderNumber,
                o.Plan.PlanName,
                o.StartDate,
                o.EndDate,
                Photos = orderPhotos[o.OrdersID].Select(p => p.OrderImageUrl).ToList(), // 取出照片網址
                // 下次收運日 如果沒有符合條件的 ServiceDate，就會顯示 "無下次訂單"，確保 NextServiceDate 不會是 null 或 0001-01-01
                NextServiceDate = nextServiceDate != DateTime.MinValue ? nextServiceDate.ToString("yyyy/MM/dd") : "無下次訂單",
                // 剩餘次數 (所有的未完成狀態)
                RemainingServices = o.OrderDetails.Count(od => (int)od.OrderStatus == (int)OrderStatus.未完成),
            };
        })
        .ToList();
            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得",
                result
            });
        }
        [HttpGet]
        [Route("GET/user/dashboard/End/{UsersID}")]//客戶已結束訂單
        public IHttpActionResult GetUserEndOrder(int UsersID)
        {
            // 查詢該用戶的訂單，只要訂單詳情是「已完成」或「已取消」狀態，就視為已結束訂單
            var EndOrders = db.Orders.Include("OrderDetails").Include("Plan").Where(o => o.UsersID == UsersID && o.OrderDetails.Any(od => od.OrderStatus == OrderStatus.已完成 || od.OrderStatus == OrderStatus.已取消)).ToList();
            // 取得這些訂單的所有照片，避免多次查詢
            var orderPhotos = db.Photo.Where(p => EndOrders.Select(o => o.OrdersID).Contains(p.OrdersID)).ToLookup(p => p.OrdersID);
            if (!EndOrders.Any())
            {
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = "找不到符合條件的訂單",
                    result = new object[] { } // 返回空陣列
                });
            }
            var result = EndOrders.Select(o => new
            {
                o.OrdersID,
                o.OrderNumber,
                o.Plan.PlanName,
                o.StartDate,
                o.EndDate,
                Photos = orderPhotos[o.OrdersID].Select(p => p.OrderImageUrl).ToList() // 取出照片網址
            }).ToList();
            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得",
                result
            });
        }

        //----------------------------------------------

        //[HttpGet]
        //[Route("GET /user/orders/{id}")]//客戶訂單詳情//已排定運收日
        //public IHttpActionResult GetOrderDetails(int id)
        //{
        //    var orderDetails = db.OrderDetails.Include("Orders").Include("Orders.Plan").Include("Orders.Discount").Include("Orders.Users").Where(od => od.OrdersID == id).ToList();
        //    if (!orderDetails.Any())
        //    {
        //        return Ok(new
        //        {
        //            statusCode = 200,
        //            status = true,
        //            message = "找不到符合條件的訂單",
        //            result = new object[] { } // 返回空陣列
        //        });
        //    }
        //    var result = orderDetails.Select(od => new
        //    {
        //        o.OrderNumber,
        //        o.Plan.PlanName,
        //        Photos = db.Photo.Where(p => p.OrdersID == o.OrdersID).Select(p => p.OrderImageUrl).ToList(), // 取出照片網址
        //        o.StartDate,
        //        o.EndDate,
        //        o.weekDay,
        //        o.addresses,
        //        總次數 = o.Orders.OrderDetails.Count,
        //        RemainingServices = o.OrderDetails.Count(od => (int)od.OrderStatus == (int)OrderStatus.未完成),//剩餘次數
        //        orderDetails = o.OrderDetails.Select(od => new
        //        {
        //            od.ServiceDate,
        //            od.OrderStatus,
        //             //時間
        //        }).ToList()

        //    }).ToList();
        //    return Ok(new
        //    {
        //        statusCode = 200,
        //        status = true,
        //        message = "成功取得",
        //        result
        //    });
        //}

        //[HttpPut]
        //[Route("PUT/user/orders/{id}")]//修改收運日期、姓名、地址、聯絡方式//更新訂單狀態
        //public IHttpActionResult PutOrderDetails(int id, OrderDetails orderDetails)
        //{
        //    var order = db.Orders.Include("OrderDetails").FirstOrDefault(o => o.OrdersID == id);
        //    if (order == null)
        //    {
        //        return Ok(new
        //        {
        //            statusCode = 200,
        //            status = false,
        //            message = "找不到符合條件的訂單"
        //        });
        //    }
        //    // 更新訂單狀態
        //    orderDetails.OrderStatus = OrderStatus.前往中;
        //    db.Entry(orderDetails).State = EntityState.Modified;
        //    db.SaveChanges();
        //    return Ok(new
        //    {
        //        statusCode = 200,
        //        status = true,
        //        message = "成功更新"
        //    });
        //}

        //[HttpPost]
        //[Route("POST/user/orders")]//新增訂單
        //public IHttpActionResult CreateOrder([FromBody] Orders orders)
        //{
        //    if (!ModelState.IsValid) //檢查請求是否有效
        //    {
        //        return BadRequest(ModelState); //如果請求無效，回傳 400 錯誤
        //    }
        //    string validWeekDays = ValidateAndFormatWeekDays(orders.WeekDay);
        //    if (validWeekDays == null)
        //    {
        //        return BadRequest("WeekDay 格式錯誤，請使用 1~7 代表星期日到星期六，並用逗號分隔。");
        //    }
        //    orders.WeekDay = validWeekDays; // 轉換後的格式
        //    // **統一使用 UTC 時間**
        //    var currentTimeUtc = DateTime.UtcNow;
        //    orders.CreatedAt = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);
        //    orders.UpdatedAt = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);

        //    // **計算 EndDate**
        //    var discount = db.Discount.FirstOrDefault(d => d.DiscountID == orders.DiscountID);
        //    if (discount != null && orders.StartDate.HasValue)
        //    {
        //        orders.EndDate = orders.StartDate.Value.AddMonths(discount.Months);
        //    }
        //    // **轉換成台灣時間**
        //    TimeZoneInfo taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        //    DateTime createdAtTaiwan = TimeZoneInfo.ConvertTimeFromUtc(orders.CreatedAt.Value, taiwanTimeZone);
        //    DateTime updatedAtTaiwan = TimeZoneInfo.ConvertTimeFromUtc(orders.UpdatedAt.Value, taiwanTimeZone);
        //    db.Orders.Add(orders);
        //    db.SaveChanges();
        //    return Ok(new
        //    {
        //        StatusCode = 200,
        //        status = "新增成功",
        //        orders
        //    });
        //}
        //// 驗證並格式化 WeekDay 欄位
        //private string ValidateAndFormatWeekDays(string weekDay)//WeekDay合併字串
        //{
        //    if (string.IsNullOrEmpty(weekDay)) return null;

        //    // 允許的星期數字 (1=星期一, ..., 7=星期日)
        //    var validDays = new HashSet<string> { "1", "2", "3", "4", "5", "6", "7" };

        //    // 分割輸入的字串
        //    var days = weekDay.Split(',').Select(d => d.Trim()).ToList();

        //    // 確保所有數字都在 1~7 範圍內
        //    if (days.All(d => validDays.Contains(d)))
        //    {
        //        return string.Join(",", days.OrderBy(d => d)); // 重新排序
        //    }

        //    return null; // 格式錯誤
        //}


    }
}
