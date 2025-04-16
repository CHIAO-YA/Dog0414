using Dog.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Dog.Controllers
{
    public class TodayController : ApiController
    {
        Models.Model1 db = new Models.Model1();

        [HttpGet]
        [Route("GET/deliver/orders/{UsersID}/today")]//一個接單員取得當天資料/狀態/筆數
        //public IHttpActionResult GetToday(int UsersID)
        //{
        //    try
        //    {
        //        DateTime today = DateTime.Now.Date;
        //        // 查找該接單員（Driver）的資料
        //        var Driver = db.Users.FirstOrDefault(U => U.UsersID == UsersID && U.Number.StartsWith("D"));
        //        // 查詢符合日期範圍的訂單
        //        var orders = db.Orders.Where(o => o.StartDate <= today && o.EndDate >= today).ToList();
        //        // 使用 GetTargetDates 方法來取得符合條件的所有日期
        //        var targetDates = GetTargetDates(orders, today);
        //        // 統計當天符合的訂單狀態
        //        var orderStatusCount = new Dictionary<OrderStatus, int>
        //        {
        //            { OrderStatus.未完成, 0 },
        //            { OrderStatus.前往中, 0 },
        //            { OrderStatus.已完成, 0 },
        //            { OrderStatus.異常回報, 0 },
        //            { OrderStatus.已取消, 0 }
        //        };
        //        // 遍歷所有符合條件的訂單
        //        foreach (var order in orders)
        //        {
        //            // 檢查這筆訂單的日期是否包含在 targetDates 中
        //            if (targetDates.Contains(order.StartDate.Value.Date))
        //            {
        //                // 根據訂單的狀態進行統計
        //                if (order.OrderStatus != null)
        //                {
        //                    orderStatusCount[(OrderStatus)order.OrderStatus]++;
        //                }
        //            }
        //        }
        //        return Ok(new
        //        {
        //            StatusCode = 200,
        //            status = true,
        //            message = "查詢該員工當天資料",
        //            ID = Driver.UsersID,
        //            Name = Driver.LineName,
        //            Number = Driver.Number,
        //            Todat = today,
        //            OrdersByDate = orderStatusCount
        //        });

        //    }
        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }
        //}
        private List<DateTime> GetTargetDates(List<Orders> orders, DateTime today)
        {
            List<DateTime> targetDates = new List<DateTime>();// 建立一個清單來存放所有符合條件的日期
            foreach (var order in orders)// 逐筆處理訂單
            {
                // 確保這筆訂單的開始日期、結束日期有值，且有指定星期幾
                if (order.StartDate.HasValue && order.EndDate.HasValue && order.WeekDay != null)
                {
                    // 把開始跟結束日期取出來（只取日期，不含時間）
                    DateTime startDate = order.StartDate.Value.Date;
                    DateTime endDate = order.EndDate.Value.Date;
                    // 將 WeekDay 的字串（例如 "1,3,5"）轉成整數清單 [1, 3, 5]
                    List<int> weekDays = order.WeekDay.Split(',').Select(s => int.Parse(s)).ToList();
                    // 從開始日期跑到結束日期，每天都檢查一次
                    for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        // 如果這一天的星期幾跟訂單指定的一樣，就加進清單
                        if (weekDays.Contains((int)date.DayOfWeek))
                        {
                            targetDates.Add(date);
                        }
                    }
                }
            }
            return targetDates;// 回傳所有符合條件的日期
        }
    }
}
