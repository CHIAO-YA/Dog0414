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
        [Route("api/linebot/basic-webhook")]
        [HttpPost]
        public async Task<IHttpActionResult> BasicWebhook()
        {
            try
            {
                string postData = await Request.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"收到的數據: {postData}");
                if (!string.IsNullOrEmpty(postData) && !postData.Contains("\"events\":[]"))
                {
                    try
                    {
                        // 解析收到的 JSON 格式資料
                        var receivedMsg = isRock.LineBot.Utility.Parsing(postData);
                        System.Diagnostics.Debug.WriteLine("JSON解析成功");
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"JSON解析失敗: {parseEx.Message}");
                    }
                }
                return Ok();
            }
            catch
            {
                return Ok(); // 即使出錯也返回Ok
            }
        }

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

                        // 取得 LINE ChannelAccessToken
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

                                // 回覆訊息
                                linebot.ReplyMessage(replyToken, "您說了: " + userMsg);
                                System.Diagnostics.Debug.WriteLine("回覆訊息成功");
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
    }
}