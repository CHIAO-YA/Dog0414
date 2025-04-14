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
            var query = db.OrderDetails.Include(o =>o.Orders).Include(o =>o).AsQueryable();

            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得用戶列表",
                statistics = new
                {
                    //total,
                    //Ongoing,
                    //Completed
                },
                //result
            });
        }




    }
}
