using Dog.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.ComponentModel.DataAnnotations;
using isRock.LineBot;


namespace Dog.Controllers
{
    public class DogController : ApiController
    {
        Models.Model1 db = new Models.Model1();

        //[HttpGet]
        //[Route("GET/driver/today/{DriverID}")]  // 查詢某個司機今天的所有訂單
        //public IHttpActionResult GetDriverToday(int DriverID)
        //{
        //    // 檢查接單者是否存在
        //    var Driver = db.Users.FirstOrDefault(u => u.UsersID == DriverID && u.Roles == Role.接單員);
        //    if (Driver == null) { return NotFound(); }
        //    //只查「今天 00:00」~「明天 00:00」之間的資料 就是今天一整天的資料
        //    var Today = DateTime.Today;// 今天的 00:00
        //    var Tomorrow = Today.AddDays(1);// 明天的 00:00

        //    // 查詢當天該接單者負責的所有訂單
        //    var DriverToday = db.OrderDetails.Include(od => od.Orders.Photo).Where(od => od.DriverID == DriverID && od.ServiceDate >= Today && od.ServiceDate < Tomorrow).ToList();

        //    // 根據訂單狀態統計數量
        //    var TodayActiveStatus = new
        //    {
        //        UnScheduled = DriverToday.Count(od => od.OrderStatus == OrderStatus.未排定),
        //        Scheduled = DriverToday.Count(od => od.OrderStatus == OrderStatus.已排定),
        //        Ongoing = DriverToday.Count(od => od.OrderStatus == OrderStatus.前往中),
        //        Arrived = DriverToday.Count(od => od.OrderStatus == OrderStatus.已抵達),
        //        Total = DriverToday.Count(od=> od.OrderStatus == OrderStatus.未排定 || 
        //                                       od.OrderStatus == OrderStatus.已排定||
        //                                       od.OrderStatus == OrderStatus.前往中 ||
        //                                       od.OrderStatus == OrderStatus.已抵達)
        //    };
        //    var TodayCompletedStatus = new
        //    {
        //        Completed = DriverToday.Count(od => od.OrderStatus == OrderStatus.已完成),
        //        Abnormal = DriverToday.Count(od => od.OrderStatus == OrderStatus.異常),
        //        Total = DriverToday.Count(od => od.OrderStatus == OrderStatus.已完成 ||
        //                                       od.OrderStatus == OrderStatus.異常 ||
        //                                       od.OrderStatus == OrderStatus.已取消)
        //    };

        //    // 如果沒有訂單，返回基本信息
        //    if (!DriverToday.Any())
        //    {
        //        return Ok(new
        //        {
        //            statusCode = 200,
        //            status = true,
        //            message = "今日無訂單",
        //            result = new
        //            {
        //                DriverID,
        //                Number = Driver.Number.Trim(),
        //                DriverName = Driver.LineId.Trim(),
        //                Today = Today.ToString("yyyy/MM/dd"),
        //                TodayActiveStatus,
        //                TodayCompletedStatus
        //            }
        //        });
        //    }
        //    // 組合結果
        //    var result = new
        //    {
        //        DriverID,
        //        Number = Driver.Number.Trim(),
        //        DriverName = Driver.LineId.Trim(),
        //        Today = Today.ToString("yyyy/MM/dd"),
        //        Total = DriverToday.Count,
        //        TodayActiveStatus,
        //        TodayCompletedStatus,
        //        Orders = DriverToday.Select(od => new
        //        {
        //            od.OrderDetailID,
        //            ServiceTime = od.DriverTimeStart.HasValue ? od.DriverTimeStart.Value.ToString("HH:mm") : null,
        //            od.OrderDetailsNumber,
        //            od.Orders.Addresses,
        //            CustomerNumber = od.Orders.Users.Number.Trim(),
        //            CustomerName = od.Orders.OrderName,
        //            od.Orders.Notes,
        //            Photo = od.Orders.Photo.Select(p => p.OrderImageUrl).ToList(),
        //            Status = od.OrderStatus.ToString(),
        //            od.Orders.Plan.PlanName,
        //            od.Orders.Plan.PlanKG,
        //            od.Orders.Plan.Liter
        //        })
        //    };

        //    return Ok(new
        //    {
        //        statusCode = 200,
        //        status = true,
        //        message = "成功取得",
        //        result
        //    });
        //}


        [HttpGet]
        [Route("GET/driver/day/{DriverID}/{Date?}/{OrderDetailID?}")]  // 查詢某個司機今天的所有訂單
        public IHttpActionResult GetDriverTodayorder(int DriverID, string Date = null, int? OrderDetailID = null)
        {
            // 檢查接單者是否存在
            var Driver = db.Users.FirstOrDefault(u => u.UsersID == DriverID && u.Roles == Role.接單員);
            if (Driver == null) { return NotFound(); }

            DateTime Day;//默認為今天
            if (string.IsNullOrEmpty(Date) || !DateTime.TryParse(Date, out Day))
            {
                Day = DateTime.Today; // 如果沒有提供日期或日期格式錯誤，使用今天
            }
            else
            {
                Day = DateTime.Parse(Date).Date; // 確保只取日期部分
            }

            var nextDay = Day.AddDays(1); // 目標日期的下一天

            // 查詢特定日期的訂單
            var driverOrders = db.OrderDetails.Include(od => od.Orders.Photo).Include(od => od.DriverPhoto)
                                      .Where(od => od.DriverID == DriverID &&
                                                  od.ServiceDate >= Day &&
                                                  od.ServiceDate < nextDay);
            if (OrderDetailID.HasValue)
            {
                driverOrders = driverOrders.Where(od => od.OrderDetailID == OrderDetailID.Value);
            }
            var DriverTodayorder = driverOrders.ToList();

            // 根據訂單狀態統計數量
            var TodayActiveStatus = new
            {
                UnScheduled = DriverTodayorder.Count(od => od.OrderStatus == OrderStatus.未排定),
                Scheduled = DriverTodayorder.Count(od => od.OrderStatus == OrderStatus.已排定),
                Ongoing = DriverTodayorder.Count(od => od.OrderStatus == OrderStatus.前往中),
                Arrived = DriverTodayorder.Count(od => od.OrderStatus == OrderStatus.已抵達),
                Total = DriverTodayorder.Count(od => od.OrderStatus == OrderStatus.未排定 ||
                                               od.OrderStatus == OrderStatus.已排定 ||
                                               od.OrderStatus == OrderStatus.前往中 ||
                                               od.OrderStatus == OrderStatus.已抵達)
            };
            var TodayCompletedStatus = new
            {
                Completed = DriverTodayorder.Count(od => od.OrderStatus == OrderStatus.已完成),
                Abnormal = DriverTodayorder.Count(od => od.OrderStatus == OrderStatus.異常),
                Total = DriverTodayorder.Count(od => od.OrderStatus == OrderStatus.已完成 ||
                                               od.OrderStatus == OrderStatus.異常)
            };
            if (OrderDetailID.HasValue && !DriverTodayorder.Any())
            {
                return NotFound();
            }
            // 如果沒有訂單，返回基本信息
            if (!DriverTodayorder.Any())
            {
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = $"{Day.ToString("yyyy/MM/dd")}無訂單",
                    result = new
                    {
                        DriverID,
                        Number = Driver.Number.Trim(),
                        DriverName = Driver.LineName.Trim(),
                        Day = Day.ToString("yyyy/MM/dd"),
                        TodayActiveStatus,
                        TodayCompletedStatus
                    }
                });
            }
            // 組合結果
            var result = new
            {
                DriverID,
                Number = Driver.Number.Trim(),
                DriverName = Driver.LineName.Trim(),
                Day = Day.ToString("yyyy/MM/dd"),
                TodayActiveStatus,
                TodayCompletedStatus,
                Orders = DriverTodayorder.Select(od => new
                {
                    od.OrderDetailID,
                    ServiceTime = od.DriverTimeStart.HasValue ? od.DriverTimeStart.Value.ToString("HH:mm") : null,
                    od.OrderDetailsNumber,
                    od.Orders.Addresses,
                    CustomerNumber = od.Orders.Users.Number.Trim(),
                    CustomerName = od.Orders.OrderName,
                    od.DriverTimeStart,
                    od.DriverTimeEnd,
                    od.Orders.Notes,
                    od.CommonIssues,
                    od.IssueDescription,
                    Photo = od.Orders.Photo.Select(p => p.OrderImageUrl).ToList(),
                    DriverPhotos = od.DriverPhoto.Select(p => p.DriverImageUrl).ToList(),
                    Status = od.OrderStatus.ToString(),
                    od.Orders.Plan.PlanName,
                    od.Orders.Plan.PlanKG,
                    od.Orders.Plan.Liter,
                    od.KG,
                })
            };

            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得",
                result
            });
        }

        //[HttpPut]
        //[Route("driver/orders/status/{OrderDetailID}")]//改變訂單狀態OrderStatus
        //public IHttpActionResult UpdateOrderStatus(int OrderDetailID, [FromBody] UpdateStatusRequest request)
        //{
        //    if (!Enum.IsDefined(typeof(OrderStatus), request.OrderStatus))
        //    {
        //        return BadRequest("無效的訂單狀態");
        //    }
        //    var orderDetail = db.OrderDetails.FirstOrDefault(od => od.OrderDetailID == OrderDetailID);
        //    if (orderDetail == null)
        //    {
        //        return NotFound();
        //    }
        //    var oldStatus = orderDetail.OrderStatus;
        //    var newStatus = (OrderStatus)request.OrderStatus;
        //    orderDetail.OrderStatus = newStatus;
        //    DateTime taiwanNow = GetTaiwanTime();
        //    orderDetail.UpdatedAt = taiwanNow;

        //    switch (newStatus)
        //    {
        //        case OrderStatus.已排定:
        //            orderDetail.ScheduledAt = taiwanNow;
        //            break;
        //        case OrderStatus.前往中:
        //            orderDetail.OngoingAt = taiwanNow;
        //            break;
        //        case OrderStatus.已抵達:
        //            orderDetail.ArrivedAt = taiwanNow;
        //            break;
        //        case OrderStatus.已完成:
        //        case OrderStatus.異常:
        //            orderDetail.CompletedAt = taiwanNow;
        //            break;
        //    }
        //    db.SaveChanges();
        //    if (oldStatus != newStatus)
        //    {
        //        // 獲取訂單和用戶信息
        //        var order = db.Orders.FirstOrDefault(o => o.OrdersID == orderDetail.OrdersID);
        //        if (order != null)
        //        {
        //            var user = db.Users.FirstOrDefault(u => u.UsersID == order.UsersID);
        //            if (user != null && !string.IsNullOrEmpty(user.MessageuserId))
        //            {
        //                System.Diagnostics.Debug.WriteLine($"用戶 LineId: {user.MessageuserId}");
        //                // 取得 Channel Access Token
        //                string channelAccessToken = System.Configuration.ConfigurationManager.AppSettings["LineChannelAccessToken"];
        //                var linebot = new isRock.LineBot.Bot(channelAccessToken);

        //                var cleanMessageuserId = user.MessageuserId
        //               .Trim()
        //               .Replace("\n", "")
        //               .Replace("\r", "")
        //               .Replace(" ", "");
        //                // 決定通知類型
        //                //string notificationType = "";
        //                //string messageContent = "";
        //                switch (newStatus)
        //                {
        //                    case OrderStatus.前往中:
        //                        //notificationType = "收運進行中通知";
        //                        linebot.PushMessage(cleanMessageuserId, $"【🐾垃不垃多Lebuleduo】\n👉收運進行中\n\n🚛我們正在趕往你指定的地點收運垃圾\n📍請確認垃圾已擺放在指定位置，並貼好 QR Code 貼紙!");
        //                        break;
        //                    case OrderStatus.已抵達:
        //                        //notificationType = "收運已抵達通知";
        //                        linebot.PushMessage(cleanMessageuserId, $"【🐾垃不垃多Lebuleduo】\n👉收運已抵達\n\n🚛我們已抵達現場，正在為你收運垃圾 \n✨請稍等片刻，服務即將完成，感謝你的耐心與配合。");
        //                        break;
        //                    case OrderStatus.已完成:
        //                        //notificationType = "收運已完成通知";

        //                        linebot.PushMessage(cleanMessageuserId, $"📋【Lebu-leduo 收運完成】📸\n今天的垃圾已成功收運完畢 ✅\n感謝你的配合。");
        //                        break;
        //                    case OrderStatus.異常:
        //                        //notificationType = "收運異常通知";
        //                        linebot.PushMessage(cleanMessageuserId, $"【Lebu-leduo 通知】\n\n我們今天找不到擺放的垃圾 😢\n請確認垃圾是否擺放在指定地點，如需補收，請回覆客服或重新預約！");
        //                        break;
        //                }
        //            }
        //        }
        //    }
        //    return Ok(new
        //    {
        //        statusCode = 200,
        //        status = true,
        //        message = "成功更新訂單狀態",
        //        result = new
        //        {
        //            orderDetail.OrderDetailID,
        //            orderDetail.OrderStatus
        //        }
        //    });
        //}
        [HttpPut]
        [Route("driver/orders/status/{OrderDetailID}")]
        public IHttpActionResult UpdateOrderStatus(int OrderDetailID, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                if (!Enum.IsDefined(typeof(OrderStatus), request.OrderStatus))
                {
                    return BadRequest("無效的訂單狀態");
                }

                var orderDetail = db.OrderDetails.FirstOrDefault(od => od.OrderDetailID == OrderDetailID);
                if (orderDetail == null)
                {
                    return NotFound();
                }

                var oldStatus = orderDetail.OrderStatus;
                var newStatus = (OrderStatus)request.OrderStatus;
                orderDetail.OrderStatus = newStatus;
                DateTime taiwanNow = GetTaiwanTime();
                orderDetail.UpdatedAt = taiwanNow;

                // 更新時間戳記的代碼保持不變...
                switch (newStatus)
                {
                    case OrderStatus.已排定:
                        orderDetail.ScheduledAt = taiwanNow;
                        break;
                        // 其他case...
                }

                db.SaveChanges();

                // 將Line消息發送包裝在單獨的try-catch中
                string lineErrorMessage = null;
                if (oldStatus != newStatus)
                {
                    try
                    {
                        // 獲取訂單和用戶信息
                        var order = db.Orders.FirstOrDefault(o => o.OrdersID == orderDetail.OrdersID);
                        if (order != null)
                        {
                            var user = db.Users.FirstOrDefault(u => u.UsersID == order.UsersID);
                            if (user != null && !string.IsNullOrEmpty(user.MessageuserId))
                            {
                                System.Diagnostics.Debug.WriteLine($"用戶 LineId: {user.MessageuserId}");
                                // 取得 Channel Access Token
                                string channelAccessToken = System.Configuration.ConfigurationManager.AppSettings["LineChannelAccessToken"];
                                var linebot = new isRock.LineBot.Bot(channelAccessToken);

                                var cleanMessageuserId = user.MessageuserId
                                    .Trim()
                                    .Replace("\n", "")
                                    .Replace("\r", "")
                                    .Replace(" ", "");

                                System.Diagnostics.Debug.WriteLine($"清理後的 LineId: {cleanMessageuserId}");

                                // 決定通知類型
                                string messageContent = "";
                                switch (newStatus)
                                {
                                    case OrderStatus.前往中:
                                        messageContent = $"【🐾垃不垃多Lebuleduo】\n👉收運進行中\n\n🚛我們正在趕往你指定的地點收運垃圾\n📍請確認垃圾已擺放在指定位置，並貼好 QR Code 貼紙!";
                                        break;
                                        // 其他case...
                                }

                                // 如果有消息內容需要發送
                                if (!string.IsNullOrEmpty(messageContent))
                                {
                                    System.Diagnostics.Debug.WriteLine($"準備發送消息: {messageContent.Substring(0, Math.Min(20, messageContent.Length))}...");

                                    // 嘗試發送Line消息
                                    var pushResult = linebot.PushMessage(cleanMessageuserId, messageContent);
                                    System.Diagnostics.Debug.WriteLine($"Line消息發送結果: {pushResult}");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("用戶或Line ID為空");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("找不到相關訂單");
                        }
                    }
                    catch (Exception lineEx)
                    {
                        // 捕獲Line消息發送錯誤，但不中斷API執行
                        lineErrorMessage = $"Line消息發送錯誤: {lineEx.Message}";
                        if (lineEx.InnerException != null)
                        {
                            lineErrorMessage += $" - 內部錯誤: {lineEx.InnerException.Message}";
                        }
                        System.Diagnostics.Debug.WriteLine(lineErrorMessage);
                        System.Diagnostics.Debug.WriteLine($"堆疊追蹤: {lineEx.StackTrace}");
                    }
                }

                // 根據Line消息發送結果決定響應
                if (lineErrorMessage != null)
                {
                    // 返回成功更新訂單狀態，但包含Line消息錯誤資訊
                    return Ok(new
                    {
                        statusCode = 200,
                        status = true,
                        message = "已更新訂單狀態，但Line消息發送失敗",
                        lineError = lineErrorMessage,
                        result = new
                        {
                            orderDetail.OrderDetailID,
                            orderDetail.OrderStatus
                        }
                    });
                }
                else
                {
                    // 完全成功的情況
                    return Ok(new
                    {
                        statusCode = 200,
                        status = true,
                        message = "成功更新訂單狀態",
                        result = new
                        {
                            orderDetail.OrderDetailID,
                            orderDetail.OrderStatus
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // 捕獲所有其他非預期錯誤
                System.Diagnostics.Debug.WriteLine($"UpdateOrderStatus出現錯誤: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆疊追蹤: {ex.StackTrace}");

                // 返回詳細的錯誤資訊而非一般500錯誤
                return InternalServerError(new Exception($"處理訂單狀態更新時出錯: {ex.Message}", ex));
            }
        }

        [HttpPut]
        [Route("PUT/driver/orders/{OrderDetailID}/weight")]//垃圾收運量
        public async Task<IHttpActionResult> UpdateOrderWeight(int OrderDetailID)
        {
            // 1. 檢查 multipart/form-data
            if (!Request.Content.IsMimeMultipartContent()) return BadRequest("請使用 multipart/form-data 格式");

            // 2. 查詢 OrderDetail
            var OrderDetail = db.OrderDetails.Include(od => od.Orders).Include(od => od.Orders.Plan).FirstOrDefault(od => od.OrderDetailID == OrderDetailID);
            if (OrderDetail == null) return NotFound();

            // 3. 處理 multipart 表單 
            var provider = await Request.Content.ReadAsMultipartAsync();
            string uploadPath = HttpContext.Current.Server.MapPath("~/DriverPhotos/");// 設定上傳路徑
            Directory.CreateDirectory(uploadPath);// 確保資料夾存在

            // 4. 讀取與檢查重量欄位（準備進行後續判斷 KG 是否超標）
            var KG = await provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "KG")?.ReadAsStringAsync();
            if (string.IsNullOrEmpty(KG) || !decimal.TryParse(KG, out decimal kg)) return BadRequest("請輸入有效的重量數字");

            // 5. 嘗試讀取異常欄位（可選）
            // 嘗試讀取 CommonIssues，如果未找到則設為 null
            var commonIssuesContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "CommonIssues");
            string CommonIssues = commonIssuesContent != null ? await commonIssuesContent.ReadAsStringAsync() : null;

            // 嘗試讀取 IssueDescription，如果未找到則設為 null
            var issueDescriptionContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.Name.Trim('"') == "IssueDescription");
            string IssueDescription = issueDescriptionContent != null ? await issueDescriptionContent.ReadAsStringAsync() : null;

            // 取得台灣時間
            DateTime taiwanNow = GetTaiwanTime();
            // 7. 更新重量與時間
            OrderDetail.KG = kg;
            OrderDetail.UpdatedAt = taiwanNow;
            OrderDetail.CompletedAt = taiwanNow;

            // 檢查是否超過計畫重量【KG > 計畫重量】→ 狀態改為 異常回報
            bool isOverWeight = kg > OrderDetail.Orders.Plan.PlanKG;
            if (isOverWeight)
            {
                // 異常回報
                OrderDetail.OrderStatus = OrderStatus.異常;
                OrderDetail.ReportedAt = taiwanNow;// 填 ReportedAt
                if (!string.IsNullOrEmpty(CommonIssues))// 填 CommonIssues
                {
                    if (Enum.TryParse<CommonIssues>(CommonIssues, out var parsedIssue))
                    {
                        OrderDetail.CommonIssues = parsedIssue;
                    }
                }

                OrderDetail.IssueDescription = IssueDescription;// 填 IssueDescription
            }
            else//【KG ≤ 計畫重量】→ 狀態改為 CompletedAt已完成
            {
                OrderDetail.OrderStatus = OrderStatus.已完成;
                OrderDetail.CompletedAt = taiwanNow;
                // 清除異常回報時間（如果有的話）
                OrderDetail.ReportedAt = null;
            }

            // 8. 處理圖片（可選）
            var fileContents = provider.Contents.Where(c => c.Headers.ContentDisposition.FileName != null).ToList();
            if (fileContents.Count == 0)
            {
                return BadRequest("請上傳圖片，圖片為必填項");
            }

            // 刪除現有的所有照片記錄
            var existingPhotos = db.DriverPhoto.Where(p => p.OrderDetailID == OrderDetailID).ToList();
            foreach (var photo in existingPhotos)
            {
                // 刪除實際檔案（如果需要的話）
                string oldPhotoPath = photo.DriverImageUrl;
                if (!string.IsNullOrEmpty(oldPhotoPath))
                {
                    string oldPhotoFullPath = HttpContext.Current.Server.MapPath("~" + oldPhotoPath);
                    if (File.Exists(oldPhotoFullPath))
                    {
                        try
                        {
                            File.Delete(oldPhotoFullPath);
                        }
                        catch (Exception ex)
                        {
                            // 記錄錯誤但不中斷操作
                            System.Diagnostics.Debug.WriteLine($"刪除舊照片失敗: {ex.Message}");
                        }
                    }
                }

                // 從數據庫刪除記錄
                db.DriverPhoto.Remove(photo);
            }

            // 添加新上傳的所有照片
            foreach (var fileContent in fileContents)
            {
                var fileName = fileContent.Headers.ContentDisposition.FileName.Trim('"');
                var fileExtension = Path.GetExtension(fileName).ToLower();

                // 檢查檔案類型
                if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(fileExtension))
                    return BadRequest("只接受 jpg、jpeg、png、gif 格式");

                // 讀取檔案內容
                var fileBytes = await fileContent.ReadAsByteArrayAsync();

                // 檢查檔案大小
                if (fileBytes.Length > 10 * 1024 * 1024)
                    return BadRequest("圖片大小不能超過 10MB");

                // 生成新檔名
                var newFileName = Guid.NewGuid().ToString() + fileExtension;
                var savedFilePath = Path.Combine(uploadPath, newFileName);

                // 儲存檔案
                File.WriteAllBytes(savedFilePath, fileBytes);
                var virtualPath = "/DriverPhotos/" + newFileName;

                // 添加新照片記錄
                db.DriverPhoto.Add(new DriverPhoto
                {
                    OrderDetailID = OrderDetailID,
                    DriverImageUrl = virtualPath,
                    CreatedAt = taiwanNow,
                    UpdatedAt = taiwanNow
                });
            }

            // 保存所有變更（包括刪除舊照片和添加新照片）
            db.SaveChanges();
            // 重新加載 OrderDetail 的 DriverPhoto 集合
            db.Entry(OrderDetail).Collection(od => od.DriverPhoto).Load();

            var order = OrderDetail.Orders; // 已經包含在查詢中
            var user = db.Users.FirstOrDefault(u => u.UsersID == order.UsersID);

            // 記錄可能的LINE錯誤訊息
            string lineErrorMessage = null;

            if (user != null && !string.IsNullOrEmpty(user.MessageuserId))
            {
                try // 在這裡添加 try-catch 來處理LINE訊息發送的錯誤
                {
                    // 取得 Channel Access Token
                    string channelAccessToken = System.Configuration.ConfigurationManager.AppSettings["LineChannelAccessToken"];
                    var linebot = new isRock.LineBot.Bot(channelAccessToken);

                    var cleanMessageuserId = user.MessageuserId
                   .Trim()                       // 移除前後空格
                   .Replace("\n", "")            // 移除換行符
                   .Replace("\r", "")            // 移除回車符
                   .Replace(" ", "")             // 移除空格
                   .Replace("\t", "");           // 移除tab

                    // 記錄發送前資訊，有助於調試
                    System.Diagnostics.Debug.WriteLine($"準備發送LINE訊息給: {cleanMessageuserId}");


                    string message;
                    if (isOverWeight)
                    {
                        message = $"【Lebu-leduo 通知】我們今天找到垃圾重量超出預期 😢\n" +
                                  $"訂單編號：{order.OrderNumber}\n" +
                                  $"重量：{OrderDetail.KG} KG（超過 {OrderDetail.Orders.Plan.PlanKG} KG）\n" +
                                  $"請確認垃圾是否符合計畫重量，如需詳細說明，請回覆客服！";
                    }
                    else
                    {
                        message = $"【🐾垃不垃多Lebuleduo】\n" +
                                  $"👉收運已完成\n\n" +
                                  $"📌訂單編號：{order.OrderNumber}\n" +
                                  $"✅今天垃圾已成功收運完畢\n" +
                                  $"♻️垃圾重量：{OrderDetail.KG} 公斤\n" +
                                  $"✨感謝你的配合。";
                    }
                    // linebot.PushMessage(cleanMessageuserId, message);
                    // 發送LINE訊息
                    var result = linebot.PushMessage(cleanMessageuserId, message);
                    System.Diagnostics.Debug.WriteLine($"LINE訊息發送結果: {result}");
                }
                catch (Exception lineEx)
                {
                    // 捕獲LINE訊息發送錯誤，但不中斷API執行
                    lineErrorMessage = $"LINE訊息發送錯誤: {lineEx.Message}";
                    if (lineEx.InnerException != null)
                    {
                        lineErrorMessage += $" - 內部錯誤: {lineEx.InnerException.Message}";
                    }

                    // 記錄錯誤，但繼續執行
                    System.Diagnostics.Debug.WriteLine(lineErrorMessage);
                    System.Diagnostics.Debug.WriteLine($"錯誤堆疊: {lineEx.StackTrace}");
                }
            }
            // 根據是否有LINE錯誤返回不同的響應
            if (lineErrorMessage != null)
            {
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = isOverWeight ? "重量超出計畫，已標記為異常回報" : "成功更新重量",
                    lineError = lineErrorMessage, // 返回LINE錯誤訊息
                    result = new
                    {
                        OrderDetail.OrderDetailID,
                        OrderDetail.KG,
                        OrderStatus = OrderDetail.OrderStatus.ToString(),
                        IsOverWeight = isOverWeight,
                        OrderDetail.UpdatedAt,
                        OrderDetail.ReportedAt,
                        OrderDetail.CompletedAt,
                        CommonIssues = OrderDetail.CommonIssues != null ? Enum.GetName(typeof(CommonIssues), OrderDetail.CommonIssues) : null,
                        OrderDetail.IssueDescription,
                        DriverPhoto = OrderDetail.DriverPhoto.Select(p => p.DriverImageUrl).ToList(),
                    }
                });
            }
            else
            {
                // 原始的返回響應
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = isOverWeight ? "重量超出計畫，已標記為異常回報" : "成功更新重量",
                    result = new
                    {
                        OrderDetail.OrderDetailID,
                        OrderDetail.KG,
                        OrderStatus = OrderDetail.OrderStatus.ToString(),
                        IsOverWeight = isOverWeight,
                        OrderDetail.UpdatedAt,
                        OrderDetail.ReportedAt,
                        OrderDetail.CompletedAt,
                        CommonIssues = OrderDetail.CommonIssues != null ? Enum.GetName(typeof(CommonIssues), OrderDetail.CommonIssues) : null,
                        OrderDetail.IssueDescription,
                        DriverPhoto = OrderDetail.DriverPhoto.Select(p => p.DriverImageUrl).ToList(),
                    }
                });
            }
        }
        //return Ok(new
        //{
        //    statusCode = 200,
        //    status = true,
        //    message = isOverWeight ? "重量超出計畫，已標記為異常回報" : "成功更新重量",
        //    result = new
        //    {
        //        OrderDetail.OrderDetailID,
        //        OrderDetail.KG,
        //        OrderStatus = OrderDetail.OrderStatus.ToString(),
        //        IsOverWeight = isOverWeight,
        //        OrderDetail.UpdatedAt,
        //        OrderDetail.ReportedAt,
        //        OrderDetail.CompletedAt,
        //        CommonIssues = OrderDetail.CommonIssues != null ? Enum.GetName(typeof(CommonIssues), OrderDetail.CommonIssues) : null, // 將 CommonIssues 轉換為文字
        //        OrderDetail.IssueDescription,
        //        DriverPhoto = OrderDetail.DriverPhoto.Select(p => p.DriverImageUrl).ToList(),
        //    }
        //});


        public class UpdateStatusRequest
        {
            public OrderStatus OrderStatus { get; set; }
        }
        private DateTime GetTaiwanTime()
        {
            TimeZoneInfo taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
        }

    }
}
