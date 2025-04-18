﻿using Dog.Models;
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
        [Route("GET/driver/today/{DriverID}/{OrderDetailID?}")]  // 查詢某個司機今天的所有訂單
        public IHttpActionResult GetDriverTodayorder(int DriverID, int? OrderDetailID = null)
        {
            // 檢查接單者是否存在
            var Driver = db.Users.FirstOrDefault(u => u.UsersID == DriverID && u.Roles == Role.接單員);
            if (Driver == null) { return NotFound(); }

            var Today = DateTime.Today;// 今天的 00:00
            var Tomorrow = Today.AddDays(1);// 明天的 00:00

            // 查詢當天該接單者負責的所有訂單
            var DriverToday = db.OrderDetails.Include(od => od.Orders.Photo)
                                 .Where(od => od.DriverID == DriverID && 
                                             od.ServiceDate >= Today &&
                                             od.ServiceDate < Tomorrow);
            if(OrderDetailID.HasValue)
            {
                DriverToday = DriverToday.Where(od => od.OrderDetailID == OrderDetailID.Value);
            }
             var DriverTodayorder = DriverToday.ToList();
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
            if(OrderDetailID.HasValue && !DriverTodayorder.Any())
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
                    message = "今日無訂單",
                    result = new
                    {
                        DriverID,
                        Number = Driver.Number.Trim(),
                        DriverName = Driver.LineId.Trim(),
                        Today = Today.ToString("yyyy/MM/dd"),
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
                DriverName = Driver.LineId.Trim(),
                Today = Today.ToString("yyyy/MM/dd"),
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
                    od.Orders.Notes,
                    Photo = od.Orders.Photo.Select(p => p.OrderImageUrl).ToList(),
                    Status = od.OrderStatus.ToString(),
                    od.Orders.Plan.PlanName,
                    od.Orders.Plan.PlanKG,
                    od.Orders.Plan.Liter
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

        //[HttpGet]
        //[Route("GET/driver/orders/{OrderDetailID}")]//訂單詳情
        //public IHttpActionResult GetOrderDetail(int OrderDetailID)
        //{
        //    var orderDetail = db.OrderDetails.Include(od => od.Orders.Plan).Include(od => od.Orders.Photo).FirstOrDefault(od => od.OrderDetailID == OrderDetailID);

        //    if (orderDetail == null)
        //    {
        //        return NotFound();
        //    }

        //    var result = new
        //    {
        //        orderDetail.OrderDetailID,
        //        orderDetail.OrderDetailsNumber,
        //        ServiceTimeStart = orderDetail.DriverTimeStart.HasValue ? orderDetail.DriverTimeStart.Value.ToString("HH:mm") : null,
        //        orderDetail.Orders.Addresses,
        //        orderDetail.Orders.OrderName,
        //        orderDetail.Orders.Users.Number,
        //        orderDetail.Orders.OrderPhone,
        //        OrderImageUrl = orderDetail.Orders.Photo.Select(p => p.OrderImageUrl).ToList(),
        //        orderDetail.Orders.Notes,
        //        Status = orderDetail.OrderStatus.ToString(),
        //        orderDetail.Orders.Plan.PlanName,
        //        orderDetail.Orders.Plan.PlanKG,
        //        orderDetail.Orders.Plan.Liter,
        //        orderDetail.DriverPhoto,
        //        orderDetail.KG,
        //    };
        //    return Ok(new
        //    {
        //        statusCode = 200,
        //        status = true,
        //        message = "成功取得",
        //        result
        //    });


        //}

        
        [HttpPut]
        [Route("driver/orders/{OrderDetailID}/status")]//改變訂單狀態OrderStatus
        public IHttpActionResult UpdateOrderStatus(int OrderDetailID, int statusValue)
        {
            if (!Enum.IsDefined(typeof(OrderStatus), statusValue))
            {
                return BadRequest("無效的訂單狀態");
            }
            var orderDetail = db.OrderDetails.FirstOrDefault(od => od.OrderDetailID == OrderDetailID);
            if (orderDetail == null)
            {
                return NotFound();
            }

            var newStatus = (OrderStatus)statusValue;
            orderDetail.OrderStatus = newStatus;
            orderDetail.UpdatedAt = DateTime.Now;
            var currentTime = DateTime.Now;

            // 根據狀態更新對應的時間欄位
            switch (newStatus)
            {
                case OrderStatus.已排定:
                    orderDetail.ScheduledAt = currentTime;
                    break;
                case OrderStatus.前往中:
                    orderDetail.OngoingAt = currentTime;
                    break;
                case OrderStatus.已抵達:
                    orderDetail.ArrivedAt = currentTime;
                    break;
                case OrderStatus.已完成:
                case OrderStatus.異常:  // 異常狀態也記錄在已完成時間
                    orderDetail.CompletedAt = currentTime;
                    break;
                    // 未排定和已排定狀態不在這個API中處理，它們在其他地方設置
            }

            db.SaveChanges();

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

            // 6. 【統一記錄當前時間】
            var currentTime = DateTime.Now;

            // 7. 更新重量與時間
            OrderDetail.KG = kg;
            OrderDetail.UpdatedAt = currentTime;
            OrderDetail.CompletedAt = currentTime;

            // 檢查是否超過計畫重量【KG > 計畫重量】→ 狀態改為 異常回報
            bool isOverWeight = kg > OrderDetail.Orders.Plan.PlanKG;
            if (isOverWeight)
            {
                // 異常回報
                OrderDetail.OrderStatus = OrderStatus.異常;
                OrderDetail.ReportedAt = currentTime;// 填 ReportedAt
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
                OrderDetail.CompletedAt = currentTime;
                // 清除異常回報時間（如果有的話）
                OrderDetail.ReportedAt = null;
            }

            // 8. 處理圖片（可選）
            var fileContent = provider.Contents.FirstOrDefault(c => c.Headers.ContentDisposition.FileName != null);
            if (fileContent == null)
            {
                return BadRequest("請上傳圖片，圖片為必填項");
            }

            var fileName = fileContent.Headers.ContentDisposition.FileName.Trim('"');
            var fileExtension = Path.GetExtension(fileName).ToLower();

            // 檢查檔案類型
            if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(fileExtension)) return BadRequest("只接受 jpg、jpeg、png、gif 格式");


            // 讀取檔案內容
            var fileBytes = await fileContent.ReadAsByteArrayAsync();

            // 檢查檔案大小
            if (fileBytes.Length > 5 * 1024 * 1024) return BadRequest("圖片大小不能超過 5MB");

            // 生成新檔名
            var newFileName = Guid.NewGuid().ToString() + fileExtension;
            var savedFilePath = Path.Combine(uploadPath, newFileName);

            // 儲存檔案
            File.WriteAllBytes(savedFilePath, fileBytes);
            var virtualPath = "/DriverPhotos/" + newFileName;

            // 查詢是否已存在照片
            var photo = db.DriverPhoto.FirstOrDefault(p => p.OrderDetailID == OrderDetailID);

            if (photo != null)//【圖片重複上傳】→ 更新圖片，只填 UpdatedAt
            {
                // 更新現有照片
                photo.DriverImageUrl = virtualPath;
                photo.UpdatedAt = currentTime;
            }
            else//【圖片第一次上傳】→ 新增圖片記錄【填 CreatedAt, UpdatedAt】
            {
                db.DriverPhoto.Add(new DriverPhoto
                {
                    OrderDetailID = OrderDetailID,
                    DriverImageUrl = virtualPath,
                    CreatedAt = currentTime,
                    UpdatedAt = currentTime
                });
            }


            // 儲存所有變更
            db.SaveChanges();

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
                    CommonIssues = OrderDetail.CommonIssues != null ? Enum.GetName(typeof(CommonIssues), OrderDetail.CommonIssues) : null, // 將 CommonIssues 轉換為文字
                    OrderDetail.IssueDescription
                }
            });
        }
    }
}
