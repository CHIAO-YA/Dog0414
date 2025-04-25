using Dog.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using isRock.LineBot;

namespace Dog.Controllers
{
    public class LineBotController : ApiController
    {
        // GET: 測試 API 是否正常運作
        [Route("api/linebot/test")]
        [HttpGet]
        public IHttpActionResult Test()
        {
            return Ok(new { message = "LINE Bot API 運作正常！" });
        }
        [Route("api/linebot/ping")]
        [HttpGet]
        public IHttpActionResult Ping()
        {
            return Ok("pong");
        }
        [Route("api/linebot/simple-webhook")]
        [HttpPost]
        public IHttpActionResult SimpleWebhook()
        {
            return Ok(new { message = "Success" });
        }


        //// POST: 處理 LINE 平台發送的 Webhook 事件
        //[Route("api/linebot/webhook")]
        //[HttpPost]
        //public async Task<IHttpActionResult> Webhook()
        //{
        //    try
        //    {
        //        string postData = await Request.Content.ReadAsStringAsync();
        //        System.Diagnostics.Debug.WriteLine($"收到的LINE Webhook數據: {postData}");

        //        if (!string.IsNullOrEmpty(postData) && postData.Contains("\"type\":\"message\""))
        //        {
        //            try
        //            {
        //                // 解析收到的 JSON 格式資料
        //                var receivedMsg = isRock.LineBot.Utility.Parsing(postData);
        //                System.Diagnostics.Debug.WriteLine("JSON解析成功");

        //                // 取得 LINE ChannelAccessToken
        //                string channelAccessToken = System.Configuration.ConfigurationManager.AppSettings["LineChannelAccessToken"];

        //                // 確保事件不為空
        //                if (receivedMsg.events != null && receivedMsg.events.Count > 0)
        //                {
        //                    var lineEvent = receivedMsg.events[0];
        //                    // 只處理文字訊息
        //                    if (lineEvent.type == "message" && lineEvent.message.type == "text")
        //                    {
        //                        string userMsg = lineEvent.message.text;
        //                        string replyToken = lineEvent.replyToken;

        //                        // 建立 LineBot SDK 要使用的 Bot 物件
        //                        var linebot = new isRock.LineBot.Bot(channelAccessToken);

        //                        // 回覆訊息
        //                        linebot.ReplyMessage(replyToken, "您說了: " + userMsg);
        //                        System.Diagnostics.Debug.WriteLine("回覆訊息成功");
        //                    }
        //                }
        //            }
        //            catch (Exception parseEx)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"處理訊息失敗: {parseEx.Message}");
        //            }
        //        }

        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        // 記錄錯誤，但仍返回OK
        //        System.Diagnostics.Debug.WriteLine($"處理Webhook時出錯: {ex.Message}");
        //        System.Diagnostics.Debug.WriteLine($"錯誤詳情: {ex.StackTrace}");
        //        return Ok(); // 不要返回InternalServerError
        //    }
        //}

        // POST: 處理 LINE 平台發送的 Webhook 事件
        [Route("api/linebot/webhook")]
        [HttpPost]
        public async Task<IHttpActionResult> Webhook()
        {
            try
            {
                string postData = await Request.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"收到的LINE Webhook數據: {postData}");

                if (!string.IsNullOrEmpty(postData) && postData.Contains("\"type\":\"message\""))
                {
                    try
                    {
                        // 解析收到的 JSON 格式資料
                        var receivedMsg = isRock.LineBot.Utility.Parsing(postData);
                        System.Diagnostics.Debug.WriteLine("JSON解析成功");

                        string channelAccessToken = System.Configuration.ConfigurationManager.AppSettings["LineChannelAccessToken"];

                        // 確保事件不為空
                        if (receivedMsg.events != null && receivedMsg.events.Count > 0)
                        {
                            var lineEvent = receivedMsg.events[0];
                            // 只處理文字訊息
                            if (lineEvent.type == "message" && lineEvent.message.type == "text")
                            {
                                string userMsg = lineEvent.message.text;
                                string replyToken = lineEvent.replyToken;

                                // 建立 LineBot SDK 要使用的 Bot 物件
                                var linebot = new isRock.LineBot.Bot(channelAccessToken);

                                // 根據收到的訊息回覆相應的通知
                                if (userMsg.Contains("問題"))
                                {
                                    string completedMessage = "5/10任務已完成！\n" +
                                                              "代收已於 5/10 上午 10:15:29 完成收運，詳情可至「我的訂單」查看收運流程。";
                                    linebot.ReplyMessage(replyToken, completedMessage);
                                }
                                else if (userMsg.Contains("補款"))
                                {
                                    string paymentReminderMessage = "一則需要補款的通知 (NT$80元)\n" +
                                                                    "請至訂單頁面進行補款，謝謝。";
                                    linebot.ReplyMessage(replyToken, paymentReminderMessage);
                                }
                                // 在處理文字訊息的部分
                                else if (userMsg.Contains("方案") || userMsg == "查看方案")
                                {
                                    // 調用獨立的方法來處理方案相關邏輯
                                    //ReplyPlanCarousel(linebot, replyToken);
                                }
                                else
                                {
                                    string defaultMessage = "請輸入指令，或聯繫客服查詢更多資訊。";
                                    linebot.ReplyMessage(replyToken, defaultMessage);
                                }
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"處理訊息失敗: {parseEx.Message}");
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                // 記錄錯誤，但仍返回OK
                System.Diagnostics.Debug.WriteLine($"處理Webhook時出錯: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"錯誤詳情: {ex.StackTrace}");
                return Ok(); // 不要返回InternalServerError
            }
        }

        // 回覆方案輪播訊息的獨立方法
        //private void ReplyPlanCarousel(isRock.LineBot.Bot linebot, string replyToken)
        //{
        //    try
        //    {
        //        // 創建小資方案卡片
        //        var smallPlan = new isRock.LineBot.CarouselColumn
        //        {
        //            title = "小資方案 NT$299/月",
        //            text = "一般垃圾+回收+廚餘=25公升\n適合相居族/小家庭/低垃圾量用戶",
        //            thumbnailImageUrl = new Uri("https://i.imgur.com/您的圖片ID.jpg"), // 請更換為實際圖片URL
        //            actions = new List<isRock.LineBot.ActionBase>
        //            {
        //                new isRock.LineBot.MessageAction
        //                {
        //                    label = "選擇此方案",
        //                    text = "我要選擇小資方案"
        //                }
        //            }
        //        };

        //        // 創建標準方案卡片
        //        var standardPlan = new isRock.LineBot.CarouselColumn
        //        {
        //            title = "標準方案 NT$599/月",
        //            text = "一般垃圾+回收+廚餘=50公升\n適合一般家庭用戶",
        //            thumbnailImageUrl = new Uri("https://i.imgur.com/您的圖片ID.jpg"), // 請更換為實際圖片URL
        //            actions = new List<isRock.LineBot.ActionBase>
        //            {
        //                new isRock.LineBot.MessageAction
        //                {
        //                    label = "選擇此方案",
        //                    text = "我要選擇標準方案"
        //                }
        //            }
        //        };

        //        // 創建大量方案卡片
        //        var largePlan = new isRock.LineBot.CarouselColumn
        //        {
        //            title = "大量方案 NT$999/月",
        //            text = "一般垃圾+回收+廚餘=75公升\n適合大家庭/高垃圾產出用戶",
        //            thumbnailImageUrl = new Uri("https://i.imgur.com/您的圖片ID.jpg"), // 請更換為實際圖片URL
        //            actions = new List<isRock.LineBot.ActionBase>
        //            {
        //                new isRock.LineBot.MessageAction
        //                {
        //                    label = "選擇此方案",
        //                    text = "我要選擇大量方案"
        //                }
        //            }
        //        };

        //        // 創建輪播訊息
        //        var carousel = new isRock.LineBot.TemplateMessage
        //        {
        //            altText = "方案介紹",
        //            template = new isRock.LineBot.CarouselTemplate
        //            {
        //                columns = new List<isRock.LineBot.CarouselColumn>
        //                {
        //                    smallPlan,
        //                    standardPlan,
        //                    largePlan
        //                }
        //            }
        //        };

        //        // 回覆輪播訊息
        //        linebot.ReplyMessage(replyToken, carousel);
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"發送方案訊息時出錯: {ex.Message}");
        //        linebot.ReplyMessage(replyToken, "抱歉，無法顯示方案資訊，請稍後再試。");
        //    }
        //}
    }
}