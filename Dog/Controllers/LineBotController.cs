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

                                // 檢查是否包含問題關鍵字
                                if (userMsg.Contains("問題"))
                                {
                                    // 發送問題列表
                                    string questionList = "請選擇問題編號：\n" +
                                                          "1️⃣第一次使用開怎麼操作介面?\n" +
                                                          "2️⃣我可以修改收運時間嗎?\n" +
                                                          "3️⃣貼紙不見了怎麼辦?\n" +
                                                          "4️⃣收運地點要怎麼改?";
                                    linebot.ReplyMessage(replyToken, questionList);
                                }
                                // 處理用戶選擇的問題編號
                                else if (userMsg.Trim() == "1")
                                {
                                    string answer = "1.第一次使用開怎麼操作介面?\n\n" +
                                                    "傳說明圖上去";
                                    linebot.ReplyMessage(replyToken, answer);
                                }
                                else if (userMsg.Trim() == "2")
                                {
                                    string answer = "2.我可以修改收運時間嗎?\n\n" +
                                                    "你可以在「我的訂單」中點選「修改預約」來變更收運日期喔❗\n" +
                                                    "如有困難也可以聯繫客服幫你操作 😊";
                                    linebot.ReplyMessage(replyToken, answer);
                                }
                                else if (userMsg.Trim() == "3")
                                {
                                    string answer = "3.貼紙不見了怎麼辦?\n\n" +
                                                    "可以點選「補發 QR 貼紙」選項，我們會重新寄送📬，\n" +
                                                    "或你也可以「自行列印 ibon 版 QR」喔!";
                                    linebot.ReplyMessage(replyToken, answer);
                                }
                                else if (userMsg.Trim() == "4")
                                {
                                    string answer = "4.收運地點要怎麼改❓\n\n" +
                                                    "請到訂單詳情點選👉「修改收運資料」，更新地址、聯絡人或照片即可!";
                                    linebot.ReplyMessage(replyToken, answer);
                                }
                                else if (userMsg.Contains("方案"))
                                {
                                    string planInfo = "我們提供以下三種方案：\n\n" +
                                                     "👉小資方案 NT$299/月\n" +
                                                     "一般垃圾+回收+廚餘=25公升\n" +
                                                     "適合單身族/小家庭/低垃圾量用戶\n\n" +
                                                     "👉標準方案 NT$599/月\n" +
                                                     "一般垃圾+回收+廚餘=50公升\n" +
                                                     "適合一般家庭/小型聚會\n\n" +
                                                     "👉大容量方案 NT$899/月\n" +
                                                     "一般垃圾+回收+廚餘=75公升\n" +
                                                     "適合大家庭/小型辦公室/多垃圾量用戶";
                                    linebot.ReplyMessage(replyToken, planInfo);
                                }
                                else if (userMsg.Contains("聯絡") || userMsg.Contains("客服") ||
                                         userMsg.Contains("聯絡客服"))
                                {
                                    string contactInfo = "【客服中心】\n" +
                                                         "📲市話 08-7221123\n" +
                                                         "☎️電話 0976-767-767\n" +
                                                         "☀️服務時間 週一至週五 9:00~19:00";
                                    linebot.ReplyMessage(replyToken, contactInfo);
                                }
                                else
                                {
                                    string defaultMessage = "您可以點選選單或輸入關鍵字" +
                                                     "或聯繫客服查詢更多資訊👍";
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

        // 資料庫狀態變化 發送LINE訊息
        [Route("api/linebot/order-status-webhook")]
        [HttpPost]
        public IHttpActionResult OrderStatusWebhook(OrderStatusUpdateRequest request)
        {
            try
            {
                // 驗證請求
                if (request == null || string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Status))
                {
                    return BadRequest("無效的請求資料");
                }

                string channelAccessToken = System.Configuration.ConfigurationManager.AppSettings["LineChannelAccessToken"];
                var linebot = new isRock.LineBot.Bot(channelAccessToken);

                //switch (request.NotificationType)
                //{
                //    case "加入官方帳號通知":
                //        Welcome(linebot, request);
                //        break;
                //    case "訂單已結帳通知":
                //        PaymentCompleted(linebot, request);
                //        break;
                //    case "收運前1小時通知":
                //        OneHourBefore(linebot, request);
                //        break;
                //    case "收運進行中通知":
                //        SendOngoing(linebot, request);
                //        break;
                //    case "收運已抵達通知":
                //        SendArrived(linebot, request);
                //        break;
                //    case "收運已完成通知":
                //        SendCompleted(linebot, request);
                //        break;
                //    case "收運異常通知":
                //        SendAbnormal(linebot, request);
                //        break;
                //    case "訂閱到期通知":
                //        SendSubscriptionExpiring(linebot, request);
                //        break;
                //    default:
                //        return BadRequest("不支援的通知類型");
                //}

                return Ok(new { success = true, message = "通知已發送" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"處理訂單狀態通知出錯: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"錯誤詳情: {ex.StackTrace}");
                return Ok(new { success = false, message = ex.Message }); 
            }
        }

        // 收運提醒通知
        private void Welcome(isRock.LineBot.Bot linebot, OrderStatusUpdateRequest request)
        {
            string message = $"♻️ 我們是全台最貼心的垃圾收運平台！\n"+
                             $"只要簡單三步驟，讓你輕鬆垃圾：\n\n" +
                             $"① 下單預約\n"+
                             $"② 在垃圾袋貼上 QR Code\n"+
                             $"③ 等待專人到你收運\n\n" +
                             $"✨ 還有即時 LINE 通知提醒，垃圾處理更安心！\n"+
                             $"現在就點選下方按鈕開始使用吧👇" +
                             $"【開始使用】🔗" ;

            var actions = new List<isRock.LineBot.TemplateActionBase>();
            actions.Add(new isRock.LineBot.UriAction()
            {
                label = "開始使用🔗",
                uri = new Uri("https://lebuleduo.vercel.app/#/auth/line/callback")
            });

            var btnTemplate = new isRock.LineBot.ButtonsTemplate()
            {
               // thumbnailImageUrl = "https://您的GIF圖片網址.gif",
                text = message,
                title = "🎉 歡迎加入【垃不垃多 Lebuleduo】🎉 ",
                actions = actions
            };

           // linebot.PushMessage(request.UserId, message, btnTemplate);
        }

        // 提前一小時收運通知
        private void SendUpcomingNotice(isRock.LineBot.Bot linebot, OrderStatusUpdateRequest request)
        {
            string message = $"【收運提醒通知】(收運前1小時)\n" +
                             $"您的代收服務即將在1小時後開始，請確認垃圾已準備完畢。";
            linebot.PushMessage(request.UserId, message);
        }

        // 進行中收運通知
        private void SendInProgressNotice(isRock.LineBot.Bot linebot, OrderStatusUpdateRequest request)
        {
            string message = $"【Lebu-leduo 收運進行中】\n" +
                             $"我們正在趕往你指定的地點收運垃圾 🚛\n" +
                             $"請確認垃圾已擺放在指定位置，並貼好 QR Code 貼紙喔～🏷️";
            linebot.PushMessage(request.UserId, message);
        }

        // 已抵達收運地點
        private void SendArrivedNotice(isRock.LineBot.Bot linebot, OrderStatusUpdateRequest request)
        {
            string message = $"【Lebu-leduo 已抵達收運地點】\n" +
                             $"我們已抵達現場，正在為你收運垃圾 🚛\n" +
                             $"請稍等片刻，服務即將完成，感謝你的耐心與配合 😊";
            linebot.PushMessage(request.UserId, message);
        }

        // 已完成收運通知
        private void SendCompletedNotice(isRock.LineBot.Bot linebot, OrderStatusUpdateRequest request)
        {
            string message = $"【Lebu-leduo 收運完成】今天的垃圾已成功收運完畢 ✅\n" +
                             $"感謝你的配合，以下是現場照片供你確認～\n" +
                             $"📸 {request.ImageUrl ?? "(照片連結)"}";
            linebot.PushMessage(request.UserId, message);
        }

        // 異常收運通知
        private void SendExceptionNotice(isRock.LineBot.Bot linebot, OrderStatusUpdateRequest request)
        {
            string message = $"【Lebu-leduo 通知】我們今天找不到擺放的垃圾 😢\n" +
                             $"請確認垃圾是否擺放在指定地點，如需補收，請回覆客服或重新預約！";
            linebot.PushMessage(request.UserId, message);
        }

        // 異常超重補款通知
        private void SendOverweightNotice(isRock.LineBot.Bot linebot, OrderStatusUpdateRequest request)
        {
            string amount = request.Amount ?? "40";
            string message = $"【超出方案範圍】本次垃圾超重，需補款 ${amount} 元 💰\n" +
                             $"請點擊以下連結完成補款，服務將正常進行～\n" +
                             $"👉 {request.PaymentUrl ?? "[立即補款]"}";
            linebot.PushMessage(request.UserId, message);
        }

        // 下次收運通知
        private void SendNextTimeNotice(isRock.LineBot.Bot linebot, OrderStatusUpdateRequest request)
        {
            string nextDate = request.NextDate ?? "下週同一時間";
            string message = $"【下次收運通知】您的下次收運時間為：{nextDate}，請記得準備好垃圾喔～";
            linebot.PushMessage(request.UserId, message);
        }

        // 接收訂單狀態更新請求的模型
        public class OrderStatusUpdateRequest
        {
            public string UserId { get; set; }        // LINE用戶ID
            public string Status { get; set; }         // 訂單狀態
            public string OrderNumber { get; set; }    // 訂單編號
            public string ImageUrl { get; set; }       // 圖片URL（如有）
            public string Amount { get; set; }         // 金額（用於超重補款）
            public string PaymentUrl { get; set; }     // 支付連結（用於超重補款）
            public string NextDate { get; set; }       // 下次收運日期
        }
    }

}