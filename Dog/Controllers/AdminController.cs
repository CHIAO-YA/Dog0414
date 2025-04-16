using Dog.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Razor.Tokenizer.Symbols;
using System.Data.Entity;//EF6 使用 System.Data.Entity 命名空間  //EF Core 使用 Microsoft.EntityFrameworkCore 命名空間
namespace Dog.Controllers
{
    public class AdminController : ApiController
    {
        Models.Model1 db = new Models.Model1();

        [HttpGet]
        [Route("GET/admin/current")]//管理員
        [Authorize] // 確保這個 API 需要身份驗證
        public IHttpActionResult GetCurrentAdmin()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return Content(HttpStatusCode.Unauthorized, new { success = false, message = "未授權的請求" });
            }
            // 獲取用戶 ID
            var userIdClaim = identity.FindFirst("Id") ?? identity.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Content(HttpStatusCode.Unauthorized, new { success = false, message = "無效的令牌" });
            }

            int adminId = int.Parse(userIdClaim.Value);
            // 查詢管理員資料
            var admin = db.Employee.FirstOrDefault(e => e.EmployeeID == adminId);
            if (admin == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得管理員資料",
                result = new
                {
                    admin.EmployeeID,
                    admin.Name
                }
            });
        }

        [HttpGet]
        [Route("GET/admin/delivers")]//代收員管理
        public IHttpActionResult GetDrivers(DateTime? date = null, string status = null, string keyword = null)
        {
            DateTime filterDate = date ?? DateTime.Today;
            var startDate = filterDate.Date;
            var endDate = startDate.AddDays(1);
            var query = db.Users.Where(u => u.Roles == Role.接單員 || u.Number.StartsWith("D"));

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(u => u.Number.Contains(keyword) || u.LineName.Contains(keyword) ||
                (u.Number.Replace("D", "").Contains(keyword)) || (u.UsersID.ToString().Contains(keyword)));
            }
            if (!string.IsNullOrEmpty(status))//全部狀態（下拉選項）上下班
            {
                bool isOnline = status == "派遣中";
                query = query.Where(u => u.IsOnline == isOnline);
            }
            var drivers = query.ToList();
            // 查詢該代收員在指定日期的所有訂單
            var result = drivers.Select(d =>
            {
                var driverOrders = db.OrderDetails
                .Where(ob => ob.DriverID == d.UsersID &&
                ob.ServiceDate >= startDate &&
                ob.ServiceDate < endDate).ToList();

                string workStatus = d.IsOnline ? "派遣中" : "休假中";
                return new
                {
                    DriverNumber = d.Number.Trim(),
                    LineName = d.LineName.Trim(),
                    ServiceDate = filterDate.ToString("yyyy/MM/dd"),
                    workStatus,
                    total = driverOrders.Count,
                    pending = driverOrders.Count(od => od.OrderStatus == OrderStatus.未排定),
                    completed = driverOrders.Count(od => od.OrderStatus == OrderStatus.已完成),
                    Reported = driverOrders.Count(od => od.OrderStatus == OrderStatus.異常)
                };
            }).ToList();

            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得代收員列表",
                date = startDate.ToString("yyyy-MM-dd"),
                result
            });
        }


        [HttpGet]
        [Route("GET/admin/Users")]//用戶管理
        public IHttpActionResult GetUsers(string UserID = null, string keyword = null, string status = null)
        {
            var query = db.Users
                .Include(u => u.Orders)
                .Where(u => u.Roles == Role.使用者 || u.Number.StartsWith("U"));

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(u => u.Number.Contains(keyword) ||
                                    u.LineName.Contains(keyword) ||
                                    u.Orders.Any(o => o.OrderName.Contains(keyword) ||
                                    (u.Number.Replace("U", "").Contains(keyword)) ||
                                    (u.UsersID.ToString().Contains(keyword))));
            }
            var Users = query.ToList();

            int total = 0;
            int active = 0;
            int inactive = 0;

            var result = Users.Select(u =>
            {// 檢查用戶是否有進行中的訂單
                bool hasactiveorders = db.OrderDetails.Any(od => od.Orders.UsersID == u.UsersID &&
                (od.OrderStatus == OrderStatus.未排定 || od.OrderStatus == OrderStatus.前往中));
                var userStatus = hasactiveorders ? "使用中" : "已停用";
                total++;
                if (userStatus == "使用中") active++;
                else inactive++;

                var firstOrder = u.Orders.FirstOrDefault();
                return new
                {
                    UsersNumber = u.Number.Trim(),
                    UsersName = firstOrder?.OrderName?.Trim(),
                    LineID = u.LineName.Trim(),
                    OrderPhone = firstOrder?.OrderPhone?.Trim(),
                    status = userStatus,
                    Orders = u.Orders.OrderByDescending(o => o.CreatedAt)
                    .Select(o => new
                    {
                        OrderNumber = o.OrderNumber.Trim(),
                        Plan = o.Plan.PlanName.Trim(),
                        RemainingCount = CalculateRemainingServices(o.OrderDetails),
                        TotalCount = o.OrderDetails.Count(),
                    }).ToList()

                };
            }).ToList();
            // 如果有傳入 status 參數，就根據它來過濾
            if (!string.IsNullOrEmpty(status))
            {
                result = result.Where(u => u.status == status).ToList();
            }

            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得用戶列表",
                statistics = new
                {
                    total,
                    active,
                    inactive
                },
                result
            });
        }
        private int CalculateRemainingServices(ICollection<OrderDetails> orderDetails)
        {
            return orderDetails.Count(od => (od.OrderStatus == OrderStatus.未排定 || od.OrderStatus == OrderStatus.前往中) && od.ServiceDate > DateTime.Now);
            // 遍歷該訂單的所有服務詳情記錄只計算狀態為「未完成」或「前往中」的記錄數量
        }


        [HttpGet]
        [Route("GET/admin/orders")]//訂單管理
        public IHttpActionResult GetOrderDetails(string OrderDetailID = null, string keyword = null, string staatus = null)
        {
            var query = db.Orders.Include(o => o.OrderDetails).Include(o => o.Plan).Include(o => o.Discount).AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(o => o.OrderNumber.Contains(keyword) ||
                o.OrderName.Contains(keyword) ||
                o.Plan.PlanName.Contains(keyword));
            }
            //if(!string.IsNullOrEmpty(staatus))
            //{
            //    query = query.Where(o => DetermineOrderStatus(o.OrderDetails, o.EndDate) == staatus);
            //}
            var Orders = query.ToList();

            var result = Orders.Select(o =>new
            {
                o.OrdersID,
                OrderNumber = o.OrderNumber,
                OrderName = o.OrderName,
                OrderStatus = DetermineOrderStatus(o.OrderDetails, o.EndDate),
                remainingCount = CalculateRemainingServices(o.OrderDetails),
                TotalCount = o.OrderDetails.Count(),
                o.Plan.PlanName,
                CollectionMethod = "定點收運",
                o.Discount.Months,
                WeekDay = ConvertWeekDayToString(o.WeekDay),
                CreatedAt = o.CreatedAt.HasValue ? o.CreatedAt.Value.ToString("yyyy/MM/dd") : ""
            }).ToList();

            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得用戶列表",
                result
            });
        }

        // 將數字格式的星期轉換為中文文字（例如："1,3,5" 轉換為 "星期一、星期三、星期五"）
        private string ConvertWeekDayToString(string weekDayString)
        {
            if (string.IsNullOrEmpty(weekDayString)) return string.Empty;// 檢查是否為空或null是則返回空字串

            var days = weekDayString.Split(',');// 使用逗號分隔輸入的字串，獲取各個星期數字
            var chineseDays = new List<string>();
            foreach (var day in days)
            {
                switch (day.Trim())//去除空格//根據數字 轉換 對應星期
                {
                    case "1": chineseDays.Add("一"); break;// 1 對應 星期一
                    case "2": chineseDays.Add("二"); break;
                    case "3": chineseDays.Add("三"); break;
                    case "4": chineseDays.Add("四"); break;
                    case "5": chineseDays.Add("五"); break;
                    case "6": chineseDays.Add("六"); break;
                    case "7": chineseDays.Add("日"); break;
                    default: break;// 對於未知值不添加任何內容
                }
            }
            return string.Join("、", chineseDays);
        }

        private string DetermineOrderStatus(ICollection<OrderDetails> orderDetails, DateTime? endDate)
        {
            if (orderDetails == null || !orderDetails.Any())
                return "未知"; // 如果不需要"未知"，可以返回其他默認狀態

            // 如果有任何訂單詳情是異常的，整個訂單視為異常
            if (orderDetails.Any(od => od.OrderStatus == OrderStatus.異常))
                return OrderStatus.異常.ToString();

            // 訂單狀態是已完成或已取消，或結束日期已過，就視為已結束
            var today = DateTime.Today;
            if (orderDetails.All(od => od.OrderStatus == OrderStatus.已完成 || od.OrderStatus == OrderStatus.已取消) ||
                (endDate.HasValue && endDate.Value < today))
                return "已結束"; // 這裡可以返回 "已完成" 或 "已取消" 取決於您的需求

            // 如果有進行中的訂單，返回進行中
            if (orderDetails.Any(od => od.OrderStatus == OrderStatus.前往中 || od.OrderStatus == OrderStatus.已抵達))
                return OrderStatus.前往中.ToString();

            // 如果有已排定但未開始的訂單
            if (orderDetails.Any(od => od.OrderStatus == OrderStatus.已排定))
                return OrderStatus.已排定.ToString();

            // 默認情況，應該是未排定
            return OrderStatus.未排定.ToString();
        }

        [HttpGet]
        [Route("GET/admin/OrderDetails/{OrdersID}/{OrderDetailID?}")]//客戶所有有效訂單+訂單詳情
        public IHttpActionResult GetUserOrder(int OrdersID, int? OrderDetailID = null)
        {
            // 檢查用戶是否存在
            if (!db.Orders.Any(o => o.OrdersID == OrdersID)) { return NotFound(); }
            // 獲取用戶的所有訂單，排除未付款訂單
            var ordersQuery = db.Orders
                .Include(o => o.Plan)
                .Include(o => o.Discount)
            .Where(o => o.OrdersID == OrdersID && o.PaymentStatus != PaymentStatus.未付款 &&
            o.OrderDetails.Any(od => od.OrderStatus == OrderStatus.未排定 || od.OrderStatus == OrderStatus.前往中));
            if (OrderDetailID.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDetails.Any(od => od.OrderDetailID == OrderDetailID.Value));
            }
            var Orders = ordersQuery.ToList();
            if (!Orders.Any())
            {
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = "找不到符合條件的訂單",
                });
            }
            // 取得這些訂單的所有照片
            var orderIds = Orders.Select(o => o.OrdersID).ToList();
            var orderPhotos = db.Photo.Where(p => orderIds.Contains(p.OrdersID)).ToLookup(p => p.OrdersID);
            // 查詢該用戶的訂單，只要訂單詳情中有任何一筆是「未完成」或「前往中」狀態，就視為有效訂單
            // 並且確保訂單狀態不是未付款
            var result = Orders.Select(o =>
            {

                return new
                {
                    o.OrdersID,
                    o.OrderNumber,
                    o.CreatedAt,
                    OrderStatus = DetermineOrderStatus(o.OrderDetails, o.EndDate),
                    o.OrderName,
                    o.OrderPhone,
                    o.Addresses,
                    CollectionMethod = "放置固定地點",
                    o.Notes,
                    Photos = orderPhotos[o.OrdersID].Select(p => p.OrderImageUrl), // 取出照片網址

                    o.Plan.PlanName,
                    o.Plan.PlanKG,
                    o.Plan.Liter,
                    StartDate = o.StartDate.HasValue ? o.StartDate.Value.ToString("yyyy/MM/dd") : null,
                    EndDate = o.EndDate.HasValue ? o.EndDate.Value.ToString("yyyy/MM/dd") : null,
                    o.Discount.Months,
                    WeekDay = ConvertWeekDayToString(o.WeekDay),

                    o.LinePayMethod,
                    o.TotalAmount,
                    o.LinePayConfirmedAt,
                    o.LinePayTransactionId,

                    TotalCount = o.OrderDetails.Count(),

                    OrderDetails = o.OrderDetails.Select(od => new
                    {
                        od.OrderDetailID,
                        ServiceDate = od.ServiceDate.ToString("yyyy/MM/dd"),
                        DriverTime = (od.DriverTimeStart.HasValue && od.DriverTimeEnd.HasValue) ?
                            $"{od.DriverTimeStart.Value.ToString("HH:mm")}-{od.DriverTimeEnd.Value.ToString("HH:mm")}" : null,
                        Status = od.OrderStatus.ToString(),
                    }).ToList()
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

    }
}
