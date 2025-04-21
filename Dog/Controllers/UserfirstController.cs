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
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;

namespace Dog.Controllers
{
    public class UserfirstController : ApiController
    {
        Models.Model1 db = new Models.Model1();

        [HttpGet]
        [Route("GET/user/dashboard/today/{UsersID}")]//客戶當天一筆訂單狀態
        public IHttpActionResult GetUserToday(int UsersID)
        {
            var user = db.Users.FirstOrDefault(u => u.UsersID == UsersID);
            if (user == null) { return NotFound(); }

            var today = DateTime.Today;
            var basicResult = new
            {
                usersID = user.UsersID,
                number = user.Number,
                name = user.LineName,
                date = today.ToString("yyyy/MM/dd") + "(" + GetChineseDayOfWeek(today) + ")"
            };
            //查詢當天訂單
            var todayOrderDetails = db.OrderDetails// 查詢該用戶當天的訂單
           .Where(od => od.Orders.UsersID == UsersID && DbFunctions.TruncateTime(od.ServiceDate) == today.Date).ToList();//找出所有UsersID的訂單中，日期是今天的訂單詳情記錄，並將結果轉換成列表形式

            //如果沒有訂單，返回基本信息
            if (!todayOrderDetails.Any())
            {
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = "今日無訂單",
                    result = new
                    {
                        basicResult.usersID,
                        basicResult.number,
                        basicResult.name,
                        basicResult.date,
                        statusMessage = "無訂單"
                    }
                });
            }
            //取第一筆有狀態訂單
            var todayOrder = todayOrderDetails.FirstOrDefault();
            // //當天有訂單時，只回傳有出現的狀態  
            var result = new Dictionary<string, object>
            {
                { "usersID", basicResult.usersID },
                { "number", basicResult.number },
                { "name", basicResult.name },
                { "date", basicResult.date },
                { "total", todayOrderDetails.Count },
                { "status", todayOrder.OrderStatus.ToString() }
            };
            // 如果有司機收運時間，則顯示
            if (todayOrder.DriverTimeStart.HasValue && todayOrder.DriverTimeEnd.HasValue)
            {
                result["driverTime"] = $"{todayOrder.DriverTimeStart.Value.ToString("HH:mm")}-{todayOrder.DriverTimeEnd.Value.ToString("HH:mm")}";
            }
            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得",
                result
            });
        }


        private string GetChineseDayOfWeek(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday: return "一";
                case DayOfWeek.Tuesday: return "二";
                case DayOfWeek.Wednesday: return "三";
                case DayOfWeek.Thursday: return "四";
                case DayOfWeek.Friday: return "五";
                case DayOfWeek.Saturday: return "六";
                case DayOfWeek.Sunday: return "日";
                default: return "";
            }
        }

        //[HttpGet]
        //[Route("GET/user/orders/{UsersID}")]//客戶訂單詳情//已排定運收日
        //public IHttpActionResult GetOrderDetails(int UsersID)
        //{
        //    var Orders = db.Orders.Include(o => o.Photo).Include(o => o.Plan).Include(o => o.OrderDetails).Where(od => od.UsersID == UsersID).ToList();
        //    if (!Orders.Any())
        //    {
        //        return Ok(new
        //        {
        //            statusCode = 200,
        //            status = true,
        //            message = "找不到符合條件的訂單",
        //            result = new object[] { } // 返回空陣列
        //        });
        //    }
        //    var result = Orders.Select(o => new
        //    {
        //        o.OrdersID,
        //        o.OrderNumber,
        //        o.Plan.PlanName,
        //        o.Plan.PlanKG,
        //        o.Plan.Liter,
        //        OrderImageUrl = o.Photo.Select(p => p.OrderImageUrl).ToList(),
        //        StartDate = o.StartDate.HasValue ? o.StartDate.Value.ToString("yyyy/MM/dd") : null,
        //        EndDate = o.EndDate.HasValue ? o.EndDate.Value.ToString("yyyy/MM/dd") : null,
        //        WeekDay = ConvertWeekDayToString(o.WeekDay),//星期幾
        //        o.Addresses,
        //        // 計算該訂單的總次數 (根據日期範圍和每週天數計算)
        //        TotalCount = CalculateTotalServiceCount(o.StartDate, o.EndDate, o.WeekDay),
        //        RemainingCount = CalculateRemainingServices(o.OrderDetails),
        //        OrderDetail = o.OrderDetails.Select(od => new
        //        {
        //            od.OrderDetailID,
        //            ServiceDate = od.ServiceDate.ToString("yyyy/MM/dd"),
        //            DriverTime = (od.DriverTimeStart.HasValue && od.DriverTimeEnd.HasValue) ? $"{od.DriverTimeStart.Value.ToString("HH:mm")}-{od.DriverTimeEnd.Value.ToString("HH:mm")}" : null,
        //            Status = od.OrderStatus.ToString()
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
        // 計算未來的剩餘訂單次數

        //[HttpGet]
        //[Route("GET/user/orders/{UsersID}/{OrdersID?}")]//客戶訂單詳情
        //public IHttpActionResult GetOrderDetails(int UsersID, int? OrdersID = null)
        //{
        //    // 檢查用戶是否存在
        //    if (!db.Users.Any(u => u.UsersID == UsersID)) { return NotFound(); }

        //    // 獲取用戶的所有訂單
        //    var ordersQuery = db.Orders
        //        .Include(o => o.Photo)
        //        .Include(o => o.Plan)
        //        .Include(o => o.OrderDetails)
        //        .Where(od => od.UsersID == UsersID);

        //    // 如果提供了特定訂單ID，則進一步過濾
        //    if (OrdersID.HasValue)
        //    {
        //        ordersQuery = ordersQuery.Where(o => o.OrdersID == OrdersID.Value);
        //    }

        //    var Orders = ordersQuery.ToList();

        //    if (!Orders.Any())
        //    {
        //        return Ok(new
        //        {
        //            statusCode = 200,
        //            status = true,
        //            message = "找不到符合條件的訂單",
        //            result = new object[] { } // 返回空陣列
        //        });
        //    }

        //    var result = Orders.Select(o =>
        //    {
        //        // 先找出下一個服務日期
        //        var nextServiceDate = o.OrderDetails
        //            .Where(od => (int)od.OrderStatus == (int)OrderStatus.未完成 && od.ServiceDate > DateTime.Now)
        //            .OrderBy(od => od.ServiceDate)
        //            .Select(od => od.ServiceDate)
        //            .FirstOrDefault();

        //        return new
        //        {
        //            o.OrdersID,
        //            o.OrderNumber,
        //            o.Plan.PlanName,
        //            o.Plan.PlanKG,
        //            o.Plan.Liter,
        //            OrderImageUrl = o.Photo.Select(p => p.OrderImageUrl).ToList(),
        //            StartDate = o.StartDate.HasValue ? o.StartDate.Value.ToString("yyyy/MM/dd") : null,
        //            EndDate = o.EndDate.HasValue ? o.EndDate.Value.ToString("yyyy/MM/dd") : null,
        //            WeekDay = ConvertWeekDayToString(o.WeekDay),//星期幾
        //            o.Addresses,
        //            // 計算該訂單的總次數 (根據日期範圍和每週天數計算)
        //            TotalCount = CalculateTotalServiceCount(o.StartDate, o.EndDate, o.WeekDay),
        //            RemainingCount = CalculateRemainingServices(o.OrderDetails),
        //            // 下次收運日
        //            NextServiceDate = nextServiceDate != DateTime.MinValue ? nextServiceDate.ToString("yyyy/MM/dd") : "無下次訂單",
        //            // 訂單詳情
        //            OrderDetail = o.OrderDetails.Select(od => new
        //            {
        //                od.OrderDetailID,
        //                ServiceDate = od.ServiceDate.ToString("yyyy/MM/dd"),
        //                DriverTime = (od.DriverTimeStart.HasValue && od.DriverTimeEnd.HasValue) ?
        //                    $"{od.DriverTimeStart.Value.ToString("HH:mm")}-{od.DriverTimeEnd.Value.ToString("HH:mm")}" : null,
        //                Status = od.OrderStatus.ToString()
        //            }).ToList()
        //        };
        //    }).ToList();

        //    return Ok(new
        //    {
        //        statusCode = 200,
        //        status = true,
        //        message = "成功取得",
        //        result
        //    });
        //}  //


        [HttpGet]
        [Route("GET/user/orders/{UsersID}/{OrdersID?}")]//客戶所有有效訂單+訂單詳情
        public IHttpActionResult GetUserOrder(int UsersID, int? OrdersID = null)
        {
            // 檢查用戶是否存在
            if (!db.Users.Any(u => u.UsersID == UsersID)) { return NotFound(); }
            // 獲取用戶的所有訂單，排除未付款訂單
            var ordersQuery = db.Orders
                .Include(o => o.Plan)
                .Include(o => o.OrderDetails)
            .Where(o => o.UsersID == UsersID && o.PaymentStatus != PaymentStatus.未付款 &&
            o.OrderDetails.Any(od => od.OrderStatus == OrderStatus.未排定 || 
            od.OrderStatus == OrderStatus.已排定 || od.OrderStatus == OrderStatus.前往中));
            if (OrdersID.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrdersID == OrdersID.Value);
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
                // 先找出下一個服務日期
                var nextServiceDate = o.OrderDetails
                .Where(od => (int)od.OrderStatus == (int)OrderStatus.已排定 &&
                od.ServiceDate > DateTime.Now).OrderBy(od => od.ServiceDate)
                .Select(od => od.ServiceDate).FirstOrDefault();
                return new
                {
                    o.OrdersID,
                    o.OrderNumber,
                    o.Plan.PlanName,
                    o.Plan.PlanKG,
                    o.Plan.Liter,
                    o.Addresses,
                    o.Notes,
                    WeekDay = ConvertWeekDayToString(o.WeekDay),
                    StartDate = o.StartDate.HasValue ? o.StartDate.Value.ToString("yyyy/MM/dd") : null,
                    EndDate = o.EndDate.HasValue ? o.EndDate.Value.ToString("yyyy/MM/dd") : null,
                    Photos = orderPhotos[o.OrdersID].Select(p => p.OrderImageUrl), // 取出照片網址
                    // 下次收運日 如果沒有符合條件的 ServiceDate，就會顯示 "無下次訂單"，確保 NextServiceDate 不會是 null 或 0001-01-01
                    NextServiceDate = nextServiceDate != DateTime.MinValue ? nextServiceDate.ToString("yyyy/MM/dd") : "無下次訂單",
                    // 剩餘次數 (所有的未完成狀態)
                    RemainingCount = CalculateRemainingServices(o.OrderDetails),
                    TotalCount = o.OrderDetails.Count(),
                    OrderDetails = o.OrderDetails.Select(od => new
                    {
                        od.OrderDetailID,
                        ServiceDate = od.ServiceDate.ToString("yyyy/MM/dd"),
                        DriverTime = (od.DriverTimeStart.HasValue && od.DriverTimeEnd.HasValue) ?
                            $"{od.DriverTimeStart.Value.ToString("HH:mm")}-{od.DriverTimeEnd.Value.ToString("HH:mm")}" : null,
                        Status = od.OrderStatus.ToString(),
                        od.DriverPhoto,
                        od.KG,
                        od.OngoingAt,
                        od.ArrivedAt,
                        od.CompletedAt,
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

        //[HttpGet]
        //[Route("GET/user/dashboard/End/{UsersID}/{OrdersID?}")]//客戶已結束訂單
        //public IHttpActionResult GetUserEndOrder(int UsersID, int? OrdersID = null)
        //{
        //    // 檢查用戶是否存在
        //    if (!db.Users.Any(u => u.UsersID == UsersID)) { return NotFound(); }
        //    // 獲取用戶的所有訂單// 過濾出已結束的訂單：
        //    // 1. 所有訂單詳情都是「已完成」或「已取消」 2. 或者所有服務日期都是過去日期
        //    var today = DateTime.Today;
        //    var EndOrders = db.Orders
        //       .Include(o => o.Plan)
        //        .Include(o => o.OrderDetails)
        //        .Where(o => o.UsersID == UsersID &&
        //        o.OrderDetails.All(od => od.OrderStatus == OrderStatus.已完成 ||
        //        od.OrderStatus == OrderStatus.已取消) ||
        //        o.OrderDetails.All(od => od.ServiceDate.Date < DateTime.Today));
        //    if (OrdersID.HasValue)
        //    {
        //        EndOrders = EndOrders.Where(o => o.OrdersID == OrdersID.Value);
        //    }
        //    var Orders = EndOrders.ToList();
        //    // 取得這些訂單的所有照片，避免多次查詢
        //    if (!Orders.Any())
        //    {
        //        return Ok(new
        //        {
        //            statusCode = 200,
        //            status = true,
        //            message = "找不到符合條件的訂單",
        //        });
        //    }
        //    // 先獲取訂單 ID 列表
        //    var orderIds = EndOrders.Select(o => o.OrdersID).ToList();
        //    // 然後使用這個列表來查詢照片
        //    var orderPhotos = db.Photo.Where(p => orderIds.Contains(p.OrdersID)).ToLookup(p => p.OrdersID);

        //    var result = EndOrders.Select(o => new
        //    {
        //        o.OrdersID,
        //        o.OrderNumber,
        //        o.Plan.PlanName,
        //        o.Plan.PlanKG,
        //        o.Plan.Liter,
        //        o.StartDate,
        //        o.EndDate,
        //        Photos = orderPhotos[o.OrdersID].Select(p => p.OrderImageUrl).ToList() // 取出照片網址
        //    }).ToList();
        //    return Ok(new
        //    {
        //        statusCode = 200,
        //        status = true,
        //        message = "成功取得",
        //        result
        //    });
        //}

        [HttpGet]
        [Route("GET/user/orders/completed/{UsersID}/{OrdersID?}")]//客戶已結束訂單+詳情
        public IHttpActionResult GetCompletedOrderDetails(int UsersID, int? OrdersID = null)
        {
            try
            {
                // 檢查用戶是否存在
                if (!db.Users.Any(u => u.UsersID == UsersID)) { return NotFound(); }
                //結束日期已過 或 訂單狀態是已完成或已取消
                var today = DateTime.Today;
                var EndOrders = db.Orders
                    .Include(o => o.Plan)
                    .Include(o => o.OrderDetails)
                    .Where(o => o.UsersID == UsersID &&
                    o.OrderDetails.All(od => od.OrderStatus == OrderStatus.已完成 ||
                        od.OrderStatus == OrderStatus.異常) ||
                    (o.EndDate.HasValue && o.EndDate.Value < today));
                if (OrdersID.HasValue)
                {
                    EndOrders = EndOrders.Where(o => o.OrdersID == OrdersID.Value);
                }
                var Orders = EndOrders.ToList();
                if (!Orders.Any())
                {
                    return Ok(new
                    {
                        statusCode = 200,
                        status = true,
                        message = "找不到符合條件的已結束訂單",
                    });
                }
                var orderIds = Orders.Select(o => o.OrdersID).ToList();
                var orderPhotos = db.Photo.Where(p => orderIds.Contains(p.OrdersID)).ToLookup(p => p.OrdersID);

                var result = Orders.Select(o => new
                {
                    o.OrdersID,
                    o.OrderNumber,
                    o.Plan.PlanName,
                    o.Plan.PlanKG,
                    o.Plan.Liter,
                    o.Notes,
                    Photos = orderPhotos[o.OrdersID].Select(p => p.OrderImageUrl).ToList(),
                    StartDate = o.StartDate.HasValue ? o.StartDate.Value.ToString("yyyy/MM/dd") : null,
                    EndDate = o.EndDate.HasValue ? o.EndDate.Value.ToString("yyyy/MM/dd") : null,
                    WeekDay = ConvertWeekDayToString(o.WeekDay),
                    o.Addresses,
                    TotalCount = o.OrderDetails.Count(),
                    //TotalCount = CalculateTotalServiceCount(o.StartDate, o.EndDate, o.WeekDay),
                    OrderDetail = o.OrderDetails.OrderByDescending(od => od.ServiceDate) // 按日期降序排列
                .Select(od => new
                {
                    od.OrderDetailID,
                    od.OrderDetailsNumber,
                    ServiceDate = od.ServiceDate.ToString("yyyy/MM/dd"),
                    DriverTime = (od.DriverTimeStart.HasValue && od.DriverTimeEnd.HasValue) ?
                               $"{od.DriverTimeStart.Value.ToString("HH:mm")}-{od.DriverTimeEnd.Value.ToString("HH:mm")}" : null,
                    Status = od.OrderStatus.ToString(),
                    //CanceledAt = od.CanceledAt.HasValue ? od.CanceledAt.Value.ToString("yyyy/MM/dd HH:mm") : null,
                    od.KG,
                    od.DriverPhoto,
                    Ongoing = od.OngoingAt,
                    Arrived = od.ArrivedAt,
                    Completed = od.CompletedAt,
                }).ToList()
                }).ToList();

                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = "成功取得已結束訂單",
                    result
                });
            }
            catch (Exception ex)
            {
                // 記錄錯誤
                System.Diagnostics.Debug.WriteLine("Error in GetCompletedOrderDetails: " + ex.Message);
                return InternalServerError(ex);
            }
        }

        private int CalculateRemainingServices(ICollection<OrderDetails> orderDetails)
        {
            return orderDetails.Count(od => (od.OrderStatus == OrderStatus.未排定 || od.OrderStatus == OrderStatus.已排定 || od.OrderStatus == OrderStatus.前往中 || od.OrderStatus == OrderStatus.已抵達) && od.ServiceDate > DateTime.Now);
            // 遍歷該訂單的所有服務詳情記錄只計算狀態為「未完成」或「前往中」的記錄數量
        }
        //// 計算該訂單的總次數
        //private int CalculateTotalServiceCount(DateTime? startDate, DateTime? endDate, string weekDayString)
        //{
        //    // 檢查空值
        //    if (!startDate.HasValue || !endDate.HasValue || string.IsNullOrEmpty(weekDayString) || startDate > endDate)
        //        return 0;

        //    var weekDays = weekDayString.Split(',')
        //                                .Select(d => int.Parse(d.Trim()))
        //                                .ToList();

        //    // 將星期日的 0 轉換為 .NET 的 DayOfWeek 中的 7 (星期日)
        //    for (int i = 0; i < weekDays.Count; i++)
        //    {
        //        if (weekDays[i] == 0)
        //            weekDays[i] = 7;
        //    }

        //    int count = 0;
        //    DateTime currentDate = startDate.Value; // 使用 .Value 來獲取 DateTime? 的值

        //    while (currentDate <= endDate.Value) // 使用 .Value 來獲取 DateTime? 的值
        //    {
        //        // .NET 中星期一是 1，星期日是 0，需要轉換一下
        //        int dayOfWeek = (int)currentDate.DayOfWeek;
        //        if (dayOfWeek == 0) dayOfWeek = 7; // 將星期日從 0 轉換為 7

        //        if (weekDays.Contains(dayOfWeek))
        //            count++;

        //        currentDate = currentDate.AddDays(1);
        //    }

        //    return count;
        //}
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
                    case "1": chineseDays.Add("星期一"); break;// 1 對應 星期一
                    case "2": chineseDays.Add("星期二"); break;
                    case "3": chineseDays.Add("星期三"); break;
                    case "4": chineseDays.Add("星期四"); break;
                    case "5": chineseDays.Add("星期五"); break;
                    case "6": chineseDays.Add("星期六"); break;
                    case "7": chineseDays.Add("星期日"); break;
                    default: break;// 對於未知值不添加任何內容
                }
            }
            return string.Join("、", chineseDays);
        }

        [HttpGet]
        [Route("GET/users/{UsersID}/orders/{OrdersID}/orderDetails/{OrderDetailID}")]//原定日期
        public IHttpActionResult GetOrderAppointment(int UsersID, int OrdersID, int OrderDetailID)
        {
            // 查詢特定用戶的特定訂單
            var order = db.Orders.FirstOrDefault(o => o.OrdersID == OrdersID && o.UsersID == UsersID);
            if (order == null) { return NotFound(); }

            // 找出指定的預約詳情
            var day = db.OrderDetails.FirstOrDefault(od => od.OrderDetailID == OrderDetailID && od.OrdersID == OrdersID);
            if (day == null) { return NotFound(); }

            // 查詢該訂單的所有任務日期 - 先加載到內存
            var orderDatesData = db.OrderDetails
                .Where(od => od.OrdersID == OrdersID)
                .OrderBy(od => od.ServiceDate)
                .ToList();


            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得",
                allOrderDates = orderDatesData.Select(od => new {
                    ServiceDate = od.ServiceDate.ToString("yyyy/MM/dd"),
                }),
                result = new
                {
                    day.OrdersID,
                    order.UsersID,

                    order.EndDate,
                    OrderStatus = day.OrderStatus.ToString(),
                    day.OrderDetailID,
                    OriginalDate = day.ServiceDate.ToString("yyyy/MM/dd"),
                    DriverTime = (day.DriverTimeStart.HasValue && day.DriverTimeEnd.HasValue) ? $"{day.DriverTimeStart.Value.ToString("HH:mm")}-{day.DriverTimeEnd.Value.ToString("HH:mm")}" : null,
                }
            });
        }


        [HttpPut]
        [Route("Put/users/{UsersID}/orders/{OrdersID}/orderDetails/{OrderDetailID}")]//更新收運日期 //更新特定用戶的特定訂單的收運日期
        public IHttpActionResult PutOrderDetailsnewday(int UsersID, int OrdersID, int OrderDetailID, [FromBody] JObject data)
        {
            var orderDetail = db.OrderDetails.Include(od => od.Orders).FirstOrDefault(od => od.OrdersID == OrdersID && od.OrderDetailID == OrderDetailID && od.Orders.UsersID == UsersID);
            // 解析從 Body 來的資料，並且嘗試取得 "newServiceDate" 這個欄位，轉換為 DateTime 物件。
            DateTime newServiceDate;
            // 檢查接收到的資料是否正確，以及 "newServiceDate" 是否能夠轉換為有效的日期格式
            if (data == null || !DateTime.TryParse(data["newServiceDate"]?.ToString(), out newServiceDate))
            {
                // 如果資料無效，回應錯誤訊息
                return BadRequest("請提供有效的日期格式 (yyyy/MM/dd)");
            }
            // 檢查是否為未來日期
            if (newServiceDate <= DateTime.Now)
            {
                return BadRequest("請選擇未來的日期，不能選擇過去的日期");
            }
            if (orderDetail == null) { return NotFound(); }
            // 更新訂單細節中的 ServiceDate 欄位為新的預約日期
            orderDetail.ServiceDate = newServiceDate;
            // 更新 UpdatedAt 欄位為當前時間
            orderDetail.UpdatedAt = DateTime.Now;
            db.SaveChanges();
            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功更新預約日期",
                result = new       // 回傳的結果
                {
                    orderDetail.OrderDetailID,
                    newServiceDate = newServiceDate.ToString("yyyy/MM/dd"),  // 回傳更新後的日期格式
                    orderDetail.OrderDetailsNumber  // 回傳更新後的訂單編號
                }
            });
        }

        [HttpGet]
        [Route("GET/user/RevisedMemInfo/{OrdersID}")]//修改收運資訊
        public IHttpActionResult GetRevisedMemInfo(int OrdersID)
        {
            var Orders = db.Orders.Include(o => o.Photo).Include(o => o.Plan).Include(o => o.Discount).Include(o => o.OrderDetails).Where(od => od.OrdersID == OrdersID).ToList();
            if (!Orders.Any())
            {
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = "找不到符合條件的訂單",
                    result = new object[] { } // 返回空陣列
                });
            }
            var result = Orders.Select(o => new
            {
                o.OrdersID,
                o.OrderNumber,
                o.Plan.PlanName,
                o.Plan.PlanKG,
                o.Plan.Liter,
                o.Discount.Months,
                WeekDay = ConvertWeekDayToString(o.WeekDay),//星期幾
                RemainingCount = CalculateRemainingServices(o.OrderDetails),
                StartDate = o.StartDate.HasValue ? o.StartDate.Value.ToString("yyyy/MM/dd") : null,
                EndDate = o.EndDate.HasValue ? o.EndDate.Value.ToString("yyyy/MM/dd") : null,
                o.OrderName,
                o.OrderPhone,
                o.Addresses,
                OrderImageUrl = o.Photo.Select(p => p.OrderImageUrl).ToList(),
                o.Notes
            }).ToList();
            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得",
                result
            });
        }

        [HttpPut]
        [Route("Put/user/RevisedMemInfo/{OrdersID}")]//更新收運資訊
        public async Task<IHttpActionResult> PutRevisedMemInfo(int OrdersID)
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return BadRequest("請使用multipart/form-data格式上傳圖片");
            }

            // 設定上傳路徑
            string uploadPath = HttpContext.Current.Server.MapPath("~/Uploads/");
            Directory.CreateDirectory(uploadPath); // 確保資料夾存在

            // 根據 OrdersID 和 UsersID 查詢訂單
            var order = db.Orders.Include(o => o.Photo).FirstOrDefault(o => o.OrdersID == OrdersID);
            if (order == null) { return NotFound(); }

            // 讀取multipart請求內容
            var provider = await Request.Content.ReadAsMultipartAsync();
            // 更新文字欄位
            var orderNameContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "OrderName");
            if (orderNameContent != null)
            {
                // 註解：讀取欄位值，非同步操作後加.Result取得結果
                string orderName = await orderNameContent.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(orderName))
                    order.OrderName = orderName;
            }

            // 註解：同樣方式處理其他表單欄位
            var orderPhoneContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "OrderPhone");
            if (orderPhoneContent != null)
            {
                string phone = await orderPhoneContent.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(phone))
                {
                    // 註解：電話格式驗證
                    if (!Regex.IsMatch(phone, @"^09\d{8}$"))
                    {
                        return BadRequest("電話號碼格式錯誤，請輸入有效的台灣手機號碼 (09xxxxxxxx)。");
                    }
                    order.OrderPhone = phone;
                }
            }

            // 註解：處理地址欄位
            var addressContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "Addresses");
            if (addressContent != null)
            {
                string address = await addressContent.ReadAsStringAsync();
                
                if (!string.IsNullOrEmpty(address))
                {
                    if (address.Length < 5)
                    {
                        return BadRequest("地址格式錯誤，請輸入有效的地址。");
                    }
                    order.Addresses = address;
                    string region = GetRegionFromAddress(order.Addresses);
                    if(!string.IsNullOrEmpty(region))
                    {
                        order.Region = region;
                    }
                }
            }

            // 註解：處理備註欄位
            var notesContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "Notes");
            if (notesContent != null)
            {
                string notes = await notesContent.ReadAsStringAsync();
                order.Notes = notes;
            }
            // 獲取當前時間
            var currentTime = DateTime.Now;
            // 找到所有相關照片
            var photosToDelete = db.Photo.Where(p => p.OrdersID == order.OrdersID).ToList();

            // 手動從資料庫中刪除這些照片
            foreach (var photo in photosToDelete)
            {
                db.Photo.Remove(photo);
            }

            //// 將新照片添加到資料庫（而不是添加到 order.Photo 集合）
            //foreach (var photo in order.Photo)
            //{
            //    photo.UpdatedAt = currentTime;
            //}
            //order.Photo.Clear(); // 清空現有的圖片資料
            foreach (var fileData in provider.Contents)
            {
                // 需要檢查是否為檔案欄位
                if (fileData.Headers.ContentDisposition.FileName == null)
                    continue; // 跳過非檔案欄位
                if (string.IsNullOrEmpty(fileData.Headers.ContentDisposition.FileName))
                    continue;
                var originalFileName = Path.GetFileName(fileData.Headers.ContentDisposition.FileName.Trim('"'));
                var fileExtension = Path.GetExtension(originalFileName).ToLower();
                if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(fileExtension))
                {
                    return BadRequest("只接受 jpg、jpeg、png 或 gif 格式的圖片");
                }
                // 檢查檔案大小
                var fileBytes = await fileData.ReadAsByteArrayAsync();
                if (fileBytes.Length > 5 * 1024 * 1024) // 5MB
                {
                    return BadRequest("圖片大小不能超過 5MB");
                }
                // 生成新檔名避免衝突
                var newFileName = Guid.NewGuid().ToString() + fileExtension;
                var savedFilePath = Path.Combine(uploadPath, newFileName);
                // 儲存檔案到指定位置
                File.WriteAllBytes(savedFilePath, fileBytes);

                var virtualPath = "/Uploads/" + newFileName; // 存成網址路徑
                                                             // 儲存新的圖片記錄
                order.Photo.Add(new Photo
                {
                    OrdersID = order.OrdersID,
                    OrderImageUrl = virtualPath,
                    CreatedAt = currentTime,
                    UpdatedAt = currentTime
                });
            }
            // 更新訂單的 UpdatedAt 欄位為當前時間
            order.UpdatedAt = DateTime.Now;
            db.SaveChanges();
            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功更新收運資訊",
                result = new
                {
                    order.OrdersID,
                    order.OrderName,
                    order.OrderPhone,
                    order.Addresses,
                    OrderImageUrl = order.Photo.Select(p => p.OrderImageUrl).ToList(), // 返回更新後的圖片 URL
                    order.Notes
                }
            });
        }

        //[HttpPut]
        //[Route("Put/user/ReviseordersMemInfo/{OrdersID}")]//更新收運資訊
        //public IHttpActionResult PutReviseOrderMemInfo(int OrdersID, int UsersID, [FromBody] JObject data)
        //{
        //    // 根據 OrdersID 和 UsersID 查詢訂單
        //    var order = db.Orders.Include(o => o.Photo).FirstOrDefault(o => o.OrdersID == OrdersID && o.UsersID == UsersID); if (order == null) { return NotFound(); }

        //    // 更新訂單資訊：OrderName、OrderPhone、Addresses、OrderImageUrl 和 note
        //    if (data["OrderName"] != null) order.OrderName = data["OrderName"].ToString();
        //    // 驗證 OrderPhone 是否為有效的電話號碼（例如：台灣 09 開頭的 10 位數）
        //    if (data["OrderPhone"] != null)
        //    {
        //        string phone = data["OrderPhone"].ToString();
        //        if (!Regex.IsMatch(phone, @"^09\d{8}$"))
        //        {
        //            return BadRequest("電話號碼格式錯誤，請輸入有效的台灣手機號碼 (09xxxxxxxx)。");
        //        }
        //        order.OrderPhone = phone;
        //    }
        //    // 驗證 Addresses 是否為有效的地址，這裡只是簡單檢查長度
        //    if (data["Addresses"] != null)
        //    {
        //        string address = data["Addresses"].ToString();
        //        if (string.IsNullOrWhiteSpace(address) || address.Length < 5)
        //        {
        //            return BadRequest("地址格式錯誤，請輸入有效的地址。");
        //        }
        //        order.Addresses = address;
        //    }
        //    if (data["Notes"] != null) order.Notes = data["Notes"].ToString();
        //    // 獲取當前時間
        //    var currentTime = DateTime.Now;
        //    if (data["OrderImageUrl"] != null)
        //    {
        //        // 假設傳來的是圖片的 URL
        //        var imageUrls = data["OrderImageUrl"].ToObject<List<string>>();
        //        // 清空現有的圖片資料前先更新時間戳
        //        foreach (var photo in order.Photo)
        //        {
        //            photo.UpdatedAt = currentTime;
        //        }
        //        order.Photo.Clear(); // 清空現有的圖片資料
        //        foreach (var imageUrl in imageUrls)
        //        {
        //            order.Photo.Add(new Photo { OrderImageUrl = imageUrl }); // 這是圖片的儲存方式
        //        }
        //    }
        //    // 更新訂單的 UpdatedAt 欄位為當前時間
        //    order.UpdatedAt = DateTime.Now;
        //    db.SaveChanges();
        //    return Ok(new
        //    {
        //        statusCode = 200,
        //        status = true,
        //        message = "成功更新收運資訊",
        //        result = new
        //        {
        //            order.OrdersID,
        //            order.OrderName,
        //            order.OrderPhone,
        //            order.Addresses,
        //            OrderImageUrl = order.Photo.Select(p => p.OrderImageUrl).ToList(), // 返回更新後的圖片 URL
        //            order.Notes
        //        }
        //    });
        //}


        //[HttpGet]
        //[Route("GET/user/orders/completed/{UsersID}")]//客戶已結束訂單詳情
        //public IHttpActionResult GetCompletedOrderDetails(int UsersID)
        //{
        //    // 查詢特定用戶的已結束訂單
        //    var Orders = db.Orders
        //        .Include(o => o.Photo).Include(o => o.Plan).Include(o => o.OrderDetails).Where(o => o.UsersID == UsersID && (o.OrderDetails.All(od => od.OrderStatus == OrderStatus.已完成 || od.OrderStatus == OrderStatus.已取消) || o.EndDate < DateTime.Today)).ToList();
        //    if (!Orders.Any())
        //    {
        //        return Ok(new
        //        {
        //            statusCode = 200,
        //            status = true,
        //            message = "找不到符合條件的已結束訂單",
        //            result = new object[] { } // 返回空陣列
        //        });
        //    }
        //    var result = Orders.Select(o => new
        //    {
        //        o.OrdersID,
        //        o.OrderNumber,
        //        o.Plan.PlanName,
        //        o.Plan.PlanKG,
        //        o.Plan.Liter,
        //        OrderImageUrl = o.Photo.Select(p => p.OrderImageUrl).ToList(),
        //        StartDate = o.StartDate.HasValue ? o.StartDate.Value.ToString("yyyy/MM/dd") : null,
        //        EndDate = o.EndDate.HasValue ? o.EndDate.Value.ToString("yyyy/MM/dd") : null,
        //        WeekDay = ConvertWeekDayToString(o.WeekDay),
        //        o.Addresses,
        //        TotalCount = CalculateTotalServiceCount(o.StartDate, o.EndDate, o.WeekDay),
        //        OrderDetail = o.OrderDetails
        //            .OrderByDescending(od => od.ServiceDate) // 按日期降序排列
        //            .Select(od => new
        //            {
        //                od.OrderDetailID,
        //                ServiceDate = od.ServiceDate.ToString("yyyy/MM/dd"),
        //                DriverTime = (od.DriverTimeStart.HasValue && od.DriverTimeEnd.HasValue) ?
        //                           $"{od.DriverTimeStart.Value.ToString("HH:mm")}-{od.DriverTimeEnd.Value.ToString("HH:mm")}" : null,
        //                Status = od.OrderStatus.ToString(),
        //                CanceledAt = od.CanceledAt.HasValue ? od.CanceledAt.Value.ToString("yyyy/MM/dd HH:mm") : null
        //            }).ToList()
        //    }).ToList();
        //    return Ok(new
        //    {
        //        statusCode = 200,
        //        status = true,
        //        message = "成功取得已結束訂單",
        //        result
        //    });
        //}


        [HttpGet]
        [Route("GET/orders/completed/{orderDetailID}")]//客戶已結束訂單明細
        public IHttpActionResult GetCompletedOrderDetail(int orderDetailID)
        {
            // 查詢特定的訂單詳情，包括所有相關資訊
            var orderDetail = db.OrderDetails
                .Include(od => od.Orders)          // 包含訂單資訊
                .Include(od => od.Orders.Plan)    // 包含方案資訊
                .Include(od => od.Orders.Photo)     // 包含司機上傳的照片
                .FirstOrDefault(od => od.OrderDetailID == orderDetailID);
            if (orderDetail == null) { return NotFound(); }
            // 確認訂單狀態為已完成
            if (orderDetail.OrderStatus != OrderStatus.已完成)
            { return BadRequest("該訂單尚未完成"); }
            // 組織狀態時間線數據
            var statusTimeline = new List<object>();

            // 添加前往中狀態及其時間戳到時間線
            if (orderDetail.OngoingAt.HasValue)
            {
                statusTimeline.Add(new
                {
                    status = "前往中",
                    time = orderDetail.OngoingAt.Value.ToString("yyyy/MM/dd HH:mm:ss")
                });
            }

            // 添加完成收運狀態及其時間戳到時間線
            if (orderDetail.CompletedAt.HasValue)
            {
                statusTimeline.Add(new
                {
                    status = "完成收運",
                    time = orderDetail.CompletedAt.Value.ToString("yyyy/MM/dd HH:mm:ss")
                });
            }

            // 獲取上傳的照片
            var Photos = orderDetail.Orders.Photo.Select(p => p.OrderImageUrl).ToList();

            // 構建回應
            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功獲取已完成訂單詳情",
                result = new
                {
                    orderDetail.OrderDetailID,
                    orderDetail.OrderDetailsNumber,
                    serviceDate = orderDetail.ServiceDate.ToString("yyyy/MM/dd"),
                    //serviceTime = $"{orderDetail.ServiceDate.ToString("HH:mm")}-{orderDetail.ServiceDate.AddHours(1).ToString("HH:mm")}",
                    //status = "已結束",
                    orderDetail.Orders.Plan.PlanName,
                    orderDetail.Orders.Plan.PlanKG,
                    orderDetail.Orders.Plan.Liter,
                    orderDetail.KG,
                    notes = orderDetail.Orders.Notes ?? "-",// 備註
                    statusTimeline,// 狀態時間線
                    Photos,// 上傳的照片
                }
            });
        }

        [HttpPost]
        [Route("POST/user/orders")]//新增訂單
        public async Task<IHttpActionResult> CreateOrder()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return BadRequest("請使用multipart/form-data格式上傳資料");
            }
            // 設定上傳路徑
            string uploadPath = HttpContext.Current.Server.MapPath("~/Uploads/");
            Directory.CreateDirectory(uploadPath); // 確保資料夾存在
                                                   // 讀取multipart請求內容
            var provider = await Request.Content.ReadAsMultipartAsync();
           
            // 從表單欄位建立Orders物件
            var orders = new Orders();

            // 讀取文字欄位
            var userIdContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "UsersID");
            if (userIdContent != null)
            {
                string userIdStr = await userIdContent.ReadAsStringAsync();
                if (int.TryParse(userIdStr, out int userId))
                    orders.UsersID = userId;
                else
                    return BadRequest("UsersID格式錯誤");
            }
            else
            {
                return BadRequest("UsersID為必填欄位");
            }

            var planIdContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "PlanID");
            if (planIdContent != null)
            {
                string planIdStr = await planIdContent.ReadAsStringAsync();
                if (int.TryParse(planIdStr, out int planId))
                    orders.PlanID = planId;
                else
                    return BadRequest("PlanID格式錯誤");
            }
            else
            {
                return BadRequest("PlanID為必填欄位");
            }

            // 讀取地址欄位並提取區域
            var addressesContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "Addresses");
            if (addressesContent != null)
            {
                orders.Addresses = await addressesContent.ReadAsStringAsync();

                // 提取區域，假設有一個方法可以將地址轉換為區域
                string region = GetRegionFromAddress(orders.Addresses);
                if (!string.IsNullOrEmpty(region))
                {
                    orders.Region = region;  // 設置提取的區域
                }
                else
                {
                    return BadRequest("無法從地址中提取區域");
                }
            }
            else
            {
                return BadRequest("Addresses為必填欄位");
            }

            // 讀取其他必要欄位
            //var monthsContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "Months");
            //if (monthsContent != null)
            //{
            //    string monthsStr = await monthsContent.ReadAsStringAsync();
            //    if (int.TryParse(monthsStr, out int months))
            //    {
            //        var discount = db.Discount.FirstOrDefault(d => d.DiscountID == orders.DiscountID);
            //        if (discount != null)
            //        {
            //            discount.Months = months;  // 更新 Months 值
            //        }
            //    }
            //    else
            //        return BadRequest("Months格式錯誤");
            //}
            var discountIdContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "DiscountID");
            if (discountIdContent != null)
            {
                string discountIdStr = await discountIdContent.ReadAsStringAsync();
                if (int.TryParse(discountIdStr, out int discountId))
                    orders.DiscountID = discountId;
                else
                    return BadRequest("DiscountID格式錯誤");
            }
            // 讀取 LinePay 的付款方式（選填）
            var linePayMethodContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "LinePayMethod");
            if (linePayMethodContent != null)
            {
                orders.LinePayMethod = await linePayMethodContent.ReadAsStringAsync();
            }

            var orderNameContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "OrderName");
            if (orderNameContent != null)
            {
                orders.OrderName = await orderNameContent.ReadAsStringAsync();
            }

            var orderPhoneContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "OrderPhone");
            if (orderPhoneContent != null)
            {
                orders.OrderPhone = await orderPhoneContent.ReadAsStringAsync();
            }

            var addressContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "Addresses");
            if (addressesContent != null)
            {
                orders.Addresses = await addressesContent.ReadAsStringAsync();
            }

            var weekDayContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "WeekDay");
            if (weekDayContent != null)
            {
                string weekDay = await weekDayContent.ReadAsStringAsync();
                string validWeekDays = ValidateAndFormatWeekDays(weekDay);
                if (validWeekDays == null)
                    return BadRequest("WeekDay 格式錯誤，請使用 1~7 代表星期日到星期六，並用逗號分隔。");

                orders.WeekDay = validWeekDays;
            }
            else
            {
                return BadRequest("WeekDay為必填欄位");
            }

            var startDateContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "StartDate");
            if (startDateContent != null)
            {
                string startDateStr = await startDateContent.ReadAsStringAsync();
                if (DateTime.TryParse(startDateStr, out DateTime startDate))
                    orders.StartDate = startDate;
                else
                    return BadRequest("StartDate格式錯誤");
            }
            else
            {
                return BadRequest("StartDate為必填欄位");
            }

            var notesContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "Notes");
            if (notesContent != null)
            {
                orders.Notes = await notesContent.ReadAsStringAsync();
            }

            // **統一使用 UTC 時間**
            var currentTimeUtc = DateTime.Now;
            orders.CreatedAt = currentTimeUtc;
            orders.UpdatedAt = currentTimeUtc;

            // **計算 EndDate**
            var Months = db.Discount.FirstOrDefault(d => d.DiscountID == orders.DiscountID);
            if (Months != null && orders.StartDate.HasValue)
            {
                // 根據月份計算天數
                int days = Months.Months * 30; // 1個月=30天, 3個月=90天, 6個月=180天
                orders.EndDate = orders.StartDate.Value.AddDays(days);
            }

            // **轉換成台灣時間**
            //TimeZoneInfo taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            //DateTime createdAtTaiwan = TimeZoneInfo.ConvertTimeFromUtc(orders.CreatedAt.Value, taiwanTimeZone);
            //DateTime updatedAtTaiwan = TimeZoneInfo.ConvertTimeFromUtc(orders.UpdatedAt.Value, taiwanTimeZone);

            // 設置訂單編號
            orders.OrderNumber = GetOrderNumber(0);

            // 先保存訂單
            db.Orders.Add(orders);
            db.SaveChanges();

            // 計算訂單總金額並更新
            decimal totalAmount = CalculateTotalAmount(orders.OrdersID);
            // 重新獲取最新的訂單信息，確保包含最新的總金額
            orders = db.Orders.Find(orders.OrdersID);
            orders.PaymentStatus = PaymentStatus.未付款; // 設置初始付款狀態
            orders.LinePayMethod = "LinePay";


            // **推算每個服務日期**
            var serviceDates = GetServiceDates(orders.StartDate.Value, orders.EndDate.Value, orders.WeekDay);

            // 新增 OrderDetails
            foreach (var serviceDate in serviceDates)
            {
                var orderDetail = new OrderDetails
                {
                    OrdersID = orders.OrdersID, // 設置訂單 ID
                    ServiceDate = serviceDate, // 設置服務日期
                    OrderStatus = OrderStatus.未排定, // 設置初始狀態
                    CreatedAt = orders.CreatedAt, // 添加创建时间
                    UpdatedAt = orders.UpdatedAt,  // 添加更新时间
                                                   // 明確設置這些欄位為 null
                    UnScheduled = null,
                    OngoingAt = null,
                    ArrivedAt = null,
                    CompletedAt = null,
                    //Scheduled = null
                };
                string datePart = serviceDate.ToString("MMdd");
                orderDetail.OrderDetailsNumber = $"{orders.OrderNumber}-{datePart}";
                db.OrderDetails.Add(orderDetail); // 新增至 OrderDetails 表
            }


            // 建立一個陣列來存儲所有圖片的路徑
            List<string> uploadedPaths = new List<string>();
           

            // 處理圖片上傳
            foreach (var fileData in provider.Contents)
            {
                // 需要檢查是否為檔案欄位
                if (string.IsNullOrEmpty(fileData.Headers.ContentDisposition.FileName))
                    continue; // 跳過非檔案欄位

                var originalFileName = Path.GetFileName(fileData.Headers.ContentDisposition.FileName.Trim('"'));
                var fileExtension = Path.GetExtension(originalFileName).ToLower();

                if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(fileExtension))
                {
                    return BadRequest("只接受 jpg、jpeg、png 或 gif 格式的圖片");
                }

                // 檢查檔案大小
                var fileBytes = await fileData.ReadAsByteArrayAsync();
                if (fileBytes.Length > 5 * 1024 * 1024) // 5MB
                {
                    return BadRequest("圖片大小不能超過 5MB");
                }

                // 生成新檔名避免衝突
                var newFileName = Guid.NewGuid().ToString() + fileExtension;
                var savedFilePath = Path.Combine(uploadPath, newFileName);

                // 儲存檔案到指定位置
                File.WriteAllBytes(savedFilePath, fileBytes);

                var virtualPath = "/Uploads/" + newFileName; // 存成網址路徑

                // 儲存新的圖片記錄
                var photo = new Photo
                {
                    OrdersID = orders.OrdersID,
                    OrderImageUrl = virtualPath,
                    CreatedAt = orders.CreatedAt,
                    UpdatedAt = orders.UpdatedAt
                };
                db.Photo.Add(photo);
            }

            db.SaveChanges();

            return Ok(new
            {
                StatusCode = 200,
                status = "新增成功",
                orders,
            });
        }

        [HttpPost]
        [Route("POST/user/orders/json")]//新增訂單
        public IHttpActionResult CreateOrder([FromBody] OrderWithPhoto OrderWithPhoto)
        {
            if (!ModelState.IsValid) //檢查請求是否有效
            {
                return BadRequest(ModelState); //如果請求無效，回傳 400 錯誤
            }
            var orders = OrderWithPhoto.Order;
            string validWeekDays = ValidateAndFormatWeekDays(orders.WeekDay);
            if (validWeekDays == null) { return BadRequest("WeekDay 格式錯誤，請使用 1~7 代表星期日到星期六，並用逗號分隔。"); }
            orders.WeekDay = validWeekDays; // 轉換後的格式

            // **統一使用 UTC 時間**
            var currentTimeUtc = DateTime.UtcNow;
            orders.CreatedAt = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);
            orders.UpdatedAt = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);

            // **計算 EndDate**
            var Months = db.Discount.FirstOrDefault(d => d.DiscountID == orders.DiscountID);
            if (Months != null && orders.StartDate.HasValue)
            {
                // 根據月份計算天數
                int days = Months.Months * 30; // 1個月=30天, 3個月=90天, 6個月=180天
                orders.EndDate = orders.StartDate.Value.AddDays(days);
            }
            // **轉換成台灣時間**
            TimeZoneInfo taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            DateTime createdAtTaiwan = TimeZoneInfo.ConvertTimeFromUtc(orders.CreatedAt.Value, taiwanTimeZone);
            DateTime updatedAtTaiwan = TimeZoneInfo.ConvertTimeFromUtc(orders.UpdatedAt.Value, taiwanTimeZone);
            // 設置訂單編號
            orders.OrderNumber = GetOrderNumber(0);

            // 先保存訂單
            db.Orders.Add(orders);
            db.SaveChanges();
            // 計算訂單總金額並更新
            decimal totalAmount = CalculateTotalAmount(orders.OrdersID);
            // **推算每個服務日期**
            var serviceDates = GetServiceDates(orders.StartDate.Value, orders.EndDate.Value, orders.WeekDay);

            // 新增 OrderDetails
            foreach (var serviceDate in serviceDates)
            {
                var orderDetail = new OrderDetails
                {
                    OrdersID = orders.OrdersID, // 設置訂單 ID
                    ServiceDate = serviceDate, // 設置服務日期
                    OrderStatus = OrderStatus.未排定, // 設置初始狀態
                    CreatedAt = orders.CreatedAt, // 添加创建时间
                    UpdatedAt = orders.UpdatedAt,  // 添加更新时间
                                                   // 明確設置這些欄位為 null
                    UnScheduled = null,
                    OngoingAt = null,
                    ArrivedAt = null,
                    CompletedAt = null,
                    //Scheduled = null
                };
                //orderDetail.GenerateOrderDetailsNumber(); // 根據日期生成訂單詳細編號
                string datePart = serviceDate.ToString("MMdd");
                orderDetail.OrderDetailsNumber = $"O{orders.OrderNumber}-{datePart}";
                db.OrderDetails.Add(orderDetail); // 新增至 OrderDetails 表

            }

            // 處理照片
            if (OrderWithPhoto.Photo != null && !string.IsNullOrEmpty(OrderWithPhoto.Photo.OrderImageUrl))
            {
                var photo = new Photo
                {
                    OrdersID = orders.OrdersID,
                    OrderImageUrl = OrderWithPhoto.Photo.OrderImageUrl,
                    CreatedAt = orders.CreatedAt,
                    UpdatedAt = orders.UpdatedAt
                };
                db.Photo.Add(photo);
            }
            db.SaveChanges();
            return Ok(new
            {
                StatusCode = 200,
                status = "新增成功",
                orders,
                PaymentStatus = PaymentStatus.未付款, // 設置初始付款狀態

            });
        }

        // 假設這是一個簡單的地址到區域的轉換方法，實際上你可能需要依賴地理編碼 API
        //private string GetRegionFromAddress(string address)
        //{
        //    // 使用正規表達式找出「某某區」
        //    var match = System.Text.RegularExpressions.Regex.Match(address, @"\p{IsCJKUnifiedIdeographs}+區");
        //    if (match.Success)
        //    {
        //        return match.Value;  // 例如「鳳山區」
        //    }
        //    return null;
        //}
        private string GetRegionFromAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            // 查找"區"的位置
            int indexOfDistrict = address.IndexOf("區");

            // 如果找到"區"且位置允許提取前面至少兩個字符
            if (indexOfDistrict >= 2)
            {
                // 只提取"區"及其前面的兩個字符，不包含市或縣
                return address.Substring(indexOfDistrict - 2, 3); // 這會返回 "鳳山區"
            }

            // 處理其他情況...
            return null;
        }

        public class OrderWithPhoto
        {
            public Orders Order { get; set; }
            public Photo Photo { get; set; }
        }
        // 驗證並格式化 WeekDay 欄位
        private string ValidateAndFormatWeekDays(string weekDay)//WeekDay合併字串
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

        //private List<DateTime> GetTargetDates(List<Orders> orders, DateTime today)
        //{
        //    List<DateTime> targetDates = new List<DateTime>();// 建立一個清單來存放所有符合條件的日期
        //    foreach (var order in orders)// 逐筆處理訂單
        //    {
        //        // 確保這筆訂單的開始日期、結束日期有值，且有指定星期幾
        //        if (order.StartDate.HasValue && order.EndDate.HasValue && order.WeekDay != null)
        //        {
        //            // 把開始跟結束日期取出來（只取日期，不含時間）
        //            DateTime startDate = order.StartDate.Value.Date;
        //            DateTime endDate = order.EndDate.Value.Date;
        //            // 將 WeekDay 的字串（例如 "1,3,5"）轉成整數清單 [1, 3, 5]
        //            List<int> weekDays = order.WeekDay.Split(',').Select(s => int.Parse(s)).ToList();
        //            // 從開始日期跑到結束日期，每天都檢查一次
        //            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        //            {
        //                // 如果這一天的星期幾跟訂單指定的一樣，就加進清單
        //                if (weekDays.Contains((int)date.DayOfWeek))
        //                {
        //                    targetDates.Add(date);
        //                }
        //            }
        //        }
        //    }
        //    return targetDates;// 回傳所有符合條件的日期
        //}


        private string GetOrderNumber(int OrdersID)//自動產生唯一編號不重複OrderNumber
        {
            string prefix = "O";
            string number;
            Random rand = new Random();
            do
            {
                int num = rand.Next(0, 10000); // 0000 ~ 9999
                number = prefix + num.ToString("D4");
            } while (db.Orders.Any(u => u.OrderNumber == number)); // 避免重複

            return number;
        }

        private decimal CalculateTotalAmount(int OrdersID, bool saveToDatabase = true)
        {
            using (var db = new Models.Model1())
            {
                var order = db.Orders.Include("Plan").Include("Discount").FirstOrDefault(o => o.OrdersID == OrdersID);
                if (order == null) throw new Exception($"訂單 ID {OrdersID} 不存在");
                int weekday = 0;// 計算週內收集次數 (WeekDayCount)
                {
                    weekday = order.WeekDay.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length;
                }
                //? 如果不是null會繼續 ?? 是 null則 Price會被視為null不會拋出錯誤
                decimal price = order.Plan?.Price ?? 0;
                int months = order.Discount?.Months ?? 1;
                decimal discountRate = order.Discount?.DiscountRate ?? 1m;// 這裡的 1m 表示一個 decimal 類型的 1
                decimal weeklyAmount = price * weekday;
                decimal subtotal = weeklyAmount * months;
                decimal discountedAmount = subtotal * discountRate;
                decimal totalAmount = Math.Round(discountedAmount, 0);
                if (saveToDatabase)
                {
                    order.TotalAmount = totalAmount;
                    db.SaveChanges();
                }
                return totalAmount;
            }
        }



        private List<DateTime> GetServiceDates(DateTime startDate, DateTime endDate, string weekDays)//計算ServiceDate
        {
            var serviceDates = new List<DateTime>(); // 用來儲存符合條件的服務日期
            var daysOfWeek = weekDays.Split(',').Select(d => int.Parse(d.Trim())).ToList(); // 轉換成列表儲存指定的服務週日（如星期一、星期三等）

            for (var date = startDate; date <= endDate; date = date.AddDays(1)) // 從 startDate 開始，逐天增加直到 endDate
            {
                // 判斷今天是否是指定的週日（1=星期一, 2=星期二, ..., 7=星期日）
                // `date.DayOfWeek` 會返回 0（星期天）到 6（星期六），因此我們需要處理 `0` 為星期天的情況
                if (daysOfWeek.Contains((int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek))
                {
                    serviceDates.Add(date); // 如果今天是服務日，將日期加入列表
                }
            }
            return serviceDates; // 返回所有符合條件的服務日期
        }
    }
}
