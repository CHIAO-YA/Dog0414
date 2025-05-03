using Dog.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using isRock.LineBot;
using Dog.Models;
using Newtonsoft.Json;
using System.Configuration;

namespace Dog.Controllers
{
    public class LineBotController : ApiController
    {
        private string channelAccessToken = ConfigurationManager.AppSettings["LineChannelAccessToken"];
        private string channelSecret = ConfigurationManager.AppSettings["LineChannelSecret"];
        Models.Model1 db = new Models.Model1();
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

        [Route("api/linebot/webhook")]
        [HttpPost]
        public async Task<IHttpActionResult> Webhook()
        {
            try
            {
                string postData = await Request.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"收到的LINE Webhook數據: {postData}");

                if (!string.IsNullOrEmpty(postData))// && postData.Contains("\"type\":\"message\"")
                {
                    // 解析收到的 JSON 格式資料
                    var receivedMsg = isRock.LineBot.Utility.Parsing(postData);
                    System.Diagnostics.Debug.WriteLine("JSON解析成功");
                    var linebot = new isRock.LineBot.Bot(channelAccessToken);

                    // 確保事件不為空
                    if (receivedMsg.events != null && receivedMsg.events.Count > 0)
                    {
                        var lineEvent = receivedMsg.events[0];

                        // 只處理文字訊息
                        if (lineEvent.type == "message" && lineEvent.message.type == "text")
                        {
                            string userMsg = lineEvent.message.text;
                            string replyToken = lineEvent.replyToken;

                            string MessageUserId = lineEvent.source.userId;
                            SaveMessageUserId(MessageUserId);
                            var user = db.Users.FirstOrDefault(u => u.MessageuserId == MessageUserId);

                            if (userMsg.Contains("問題"))
                            {
                                ProcessMessage(userMsg, replyToken);
                            }
                            else if (userMsg.Contains("通知") || userMsg.Contains("1"))
                            {
                                int usersId = user.UsersID;
                                var buttonTemplate = new isRock.LineBot.ButtonsTemplate()
                                {
                                    text = "🎉🎯即時收到通知📲💬\n請點選以下按鈕進行綁定：",
                                    actions = new List<isRock.LineBot.TemplateActionBase>()
                                {
                                    new isRock.LineBot.UriAction()
                                    {
                                        label = "進入平台",
                                        uri = new Uri($"https://lebuleduo.vercel.app/?UsersID={usersId}")
                                    }
                                }
                                };
                                // 發送按鈕模板
                                linebot.ReplyMessage(replyToken, new isRock.LineBot.TemplateMessage(buttonTemplate));
                            }
                            else if (userMsg.Contains("提醒") || userMsg.Contains("2"))
                            {
                                // 創建一個最簡單的按鈕模板
                                var simpleButtonTemplate = new isRock.LineBot.ButtonsTemplate()
                                {
                                    title = "🐶Lebu-leduo 收運提醒通知",
                                    text = "♻️你今天的垃圾代收服務即將開始！" +
                                           "⏰收運時間 早上09:00-09:30\n"+
                                           "請將垃圾打包好並貼上貼紙，擺放在指定位置",
                                    thumbnailImageUrl = new Uri("https://raw.githubusercontent.com/CHIAO-YA/DogPhotourl/main/godphoto/0502.PNG"), // 替換成您的客服圖片URL
                                    actions = new List<isRock.LineBot.TemplateActionBase>()
                                    {
                                        new isRock.LineBot.UriAction()
                                        {
                                            label = "查看訂單",
                                            uri = new Uri("https://lebuleduo.vercel.app")
                                        }
                                    }
                                };

                                linebot.ReplyMessage(replyToken, new isRock.LineBot.TemplateMessage(simpleButtonTemplate));
                            }
                            else if (userMsg.Contains("方案"))
                            {
                                ProcessPlanInfo(replyToken);
                            }
                            else if (userMsg.Contains("聯絡") || userMsg.Contains("客服") ||
                                     userMsg.Contains("聯絡客服"))
                            {
                                var ButtonTemplate = new isRock.LineBot.ButtonsTemplate()
                                {
                                    title = "客服中心",
                                    text = "📲市話 08-7221123\n☎️電話 0976-767-767\n☀️服務時間 週一至週五 9:00~19:00",
                                    thumbnailImageUrl = new Uri("https://github.com/CHIAO-YA/DogPhotourl/blob/main/godphoto/line2.png?raw=true"), // 替換成您的客服圖片URL
                                    actions = new List<isRock.LineBot.TemplateActionBase>()
                                    {
                                        new isRock.LineBot.UriAction()
                                        {
                                            label = "撥打客服電話",
                                            uri = new Uri("tel:0976-767-767")
                                        },
                                        new isRock.LineBot.PostbackAction()
                                        {
                                            label = "線上諮詢",
                                            data = "ACTION=ONLINE_SERVICE",
                                            displayText = "我想要線上諮詢"
                                        }
                                    }
                                };

                                // 發送模板訊息
                                isRock.LineBot.Bot bot = new isRock.LineBot.Bot(channelAccessToken);
                                bot.ReplyMessage(replyToken, new isRock.LineBot.TemplateMessage(ButtonTemplate));
                            }
                            else
                            {
                                string defaultMessage = "您可以點選選單或輸入關鍵字" +
                                                 "或聯繫客服查詢更多資訊👍";
                                linebot.ReplyMessage(replyToken, defaultMessage);
                            }
                        }
                        // 處理 Postback 事件 - 處理所有問題按鈕點擊
                        //else if (lineEvent.type == "postback")
                        //{
                        //    var postbackData = lineEvent.postback.data;
                        //    string replyToken = lineEvent.replyToken;

                        //    var query = System.Web.HttpUtility.ParseQueryString(postbackData);
                        //    string action = query["ACTION"];
                        //    string id = query["ID"];

                        //    if (action == "FAQ")
                        //    {
                        //        string answer = "";
                        //        switch (id)
                        //        {
                        //            case "1":
                        //                answer = "Q.第一次使用開怎麼操作介面?\n\n" +
                        //                            "傳說明圖上去";
                        //                break;
                        //            case "2":
                        //                answer = "Q.我可以修改收運時間嗎?\n\n" +
                        //                            "你可以在「我的訂單」中點選「修改預約」來變更收運日期喔❗\n" +
                        //                            "如有困難也可以聯繫客服幫你操作 😊";
                        //                break;
                        //            case "3":
                        //                answer = "Q.貼紙不見了怎麼辦?\n\n" +
                        //                            "可以點選「補發 QR 貼紙」選項，我們會重新寄送📬，\n" +
                        //                            "或你也可以「自行列印 ibon 版 QR」喔!";
                        //                break;
                        //            case "4":
                        //                answer = "Q.收運地點要怎麼改❓\n\n" +
                        //                            "請到訂單詳情點選👉「修改收運資料」，更新地址、聯絡人或照片即可!";
                        //                break;
                        //            default:
                        //                answer = "抱歉，找不到相關的問題答案。";
                        //                break;
                        //        }
                        //        linebot.ReplyMessage(replyToken, answer);
                        //    }
                        //}
                        else if (lineEvent.type == "postback")
                        {
                            var postbackData = lineEvent.postback.data;
                            string replyToken = lineEvent.replyToken;

                            var query = System.Web.HttpUtility.ParseQueryString(postbackData);
                            string action = query["ACTION"];
                            string id = query["ID"];

                            if (action == "FAQ")
                            {
                                switch (id)
                                {
                                    case "1": // 第一次使用教學，有圖
                                        var messages1 = new List<isRock.LineBot.MessageBase>();
                                        messages1.Add(new isRock.LineBot.TextMessage("Q.第一次使用介面操作？請參考下圖："));
                                        messages1.Add(new isRock.LineBot.ImageMessage(
                                            new Uri("https://github.com/CHIAO-YA/DogPhotourl/blob/main/godphoto/3.PNG?raw=true"),
                                            new Uri("https://github.com/CHIAO-YA/DogPhotourl/blob/main/godphoto/3.PNG?raw=true")
                                        ));
                                        linebot.ReplyMessage(replyToken, messages1);
                                        break;

                                    case "2":
                                        linebot.ReplyMessage(replyToken,
                                            "Q.我可以修改收運時間嗎?\n\n你可以在「我的訂單」中點選「修改預約」來變更收運日期喔❗\n如有困難也可以聯繫客服幫你操作 😊");
                                        break;

                                    case "3":
                                        linebot.ReplyMessage(replyToken,
                                            "Q.貼紙不見了怎麼辦?\n\n可以點選「補發 QR 貼紙」選項，我們會重新寄送📬，\n或你也可以「自行列印 ibon 版 QR」喔!");
                                        break;

                                    case "4":
                                        linebot.ReplyMessage(replyToken,
                                            "Q.收運地點要怎麼改❓\n\n請到訂單詳情點選👉「修改收運資料」，更新地址、聯絡人或照片即可!");
                                        break;

                                    default:
                                        linebot.ReplyMessage(replyToken, "抱歉，找不到相關的問題答案。");
                                        break;
                                }
                            }
                        }

                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"處理Webhook時出錯: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"錯誤詳情: {ex.StackTrace}");
                return Ok();
            }
        }

        private void ProcessMessage(string userMsg, string replyToken)
        {
            var actions = new List<isRock.LineBot.TemplateActionBase>();
            actions.Add(new isRock.LineBot.PostbackAction()
            {
                label = "第一次使用開怎麼操作介面?",
                data = "ACTION=FAQ&ID=1",
                displayText = "第一次使用該怎麼操作介面?"
            });

            actions.Add(new isRock.LineBot.PostbackAction()
            {
                label = "我可以修改收運時間嗎?",
                data = "ACTION=FAQ&ID=2",
                displayText = "我可以修改收運時間嗎?"
            });

            actions.Add(new isRock.LineBot.PostbackAction()
            {
                label = "貼紙不見了怎麼辦?",
                data = "ACTION=FAQ&ID=3",
                displayText = "貼紙不見了怎麼辦 ?"
            });

            actions.Add(new isRock.LineBot.PostbackAction()
            {
                label = "收運地點要怎麼改?",
                data = "ACTION=FAQ&ID=4",
                displayText = "收運地點要怎麼改?"
            });

            // 創建按鈕模板
            var ButtonTemplate = new isRock.LineBot.ButtonsTemplate()
            {
                title = "常見問題",
                text = "請選擇您的問題",
                thumbnailImageUrl = new Uri("https://github.com/CHIAO-YA/DogPhotourl/blob/main/godphoto/%E7%85%A9%E6%83%B1.png?raw=true"), // 替換成你的FAQ圖片URL
                actions = actions
            };

            // 創建 TemplateMessage
            var ButtonMessage = new isRock.LineBot.TemplateMessage(ButtonTemplate);

            // 發送 TemplateMessage
            isRock.LineBot.Bot bot = new isRock.LineBot.Bot(channelAccessToken);
            bot.ReplyMessage(replyToken, ButtonMessage);
        }

        private void SaveMessageUserId(string messageUserId)
        {
            var existingUser = db.Users.FirstOrDefault(u => u.MessageuserId == messageUserId);
            if (existingUser == null)
            {
                db.Users.Add(new Models.Users
                {
                    MessageuserId = messageUserId,
                    CreatedAt = DateTime.Now,
                    IsOnline = false,
                    Roles = Role.使用者
                });
                db.SaveChanges();
            }
        }

        private void ProcessPlanInfo(string replyToken)
        {
            // 創建輪播模板 (Carousel Template)
            var columns = new List<isRock.LineBot.Column>();

            // 小資方案
            columns.Add(new isRock.LineBot.Column()
            {
                title = "小資方案｜1-2人適用",
                text = "NT$299/月起\n" +
                       "🍀適合租屋族/小家庭/低垃圾量用戶\n" +
                       "☑️每次收運 25L/5kg\n" +
                       "☑️一次搞定垃圾、回收、廚餘\n",
                //"☑️每週收運自由選\n" +
                //"☑️專屬QR碼追蹤任務\n" +
                //"☑️彈性調整預約時間\n"

                thumbnailImageUrl = new Uri("https://github.com/CHIAO-YA/DogPhotourl/blob/main/godphoto/%E8%B7%91%E8%B7%91%E7%8B%97%E7%8B%97.gif?raw=true"),
                actions = new List<isRock.LineBot.TemplateActionBase>()
                {
                    new isRock.LineBot.UriAction()
                    {
                        label = "選擇小資方案",
                        uri = new Uri("https://lebuleduo.vercel.app")
                    }
                }
            });

            // 標準方案
            columns.Add(new isRock.LineBot.Column()
            {
                title = "標準方案｜3~5人適用",
                text = "NT$ 599 /月起\n" +
                       "🍀適合一般家庭/共享租屋族\n" +
                       "☑️每次收運 50L/10kg\n" +
                       "☑️一次搞定垃圾、回收、廚餘\n",
                       //"☑️每週收運自由選\n" +
                       //"☑️專屬QR碼追蹤任務\n" +
                       //"☑️彈性調整預約時間",
                thumbnailImageUrl = new Uri("https://github.com/CHIAO-YA/DogPhotourl/blob/main/godphoto/0502.PNG?raw=true"),
                actions = new List<isRock.LineBot.TemplateActionBase>()
                {
                    new isRock.LineBot.UriAction()
                    {
                        label = "選擇標準方案",
                        uri = new Uri("https://lebuleduo.vercel.app")
                    }
                }
            });

            // 大容量方案
            columns.Add(new isRock.LineBot.Column()
            {
                title = "大容量方案｜6~10人適用",
                text = "NT$ 899 /月起\n" +
                        "🍀適合大家庭/小型商家\n" +
                        "☑️每次收運 75L/15kg\n" +
                        "☑️一次搞定垃圾、回收、廚餘\n",
                        //"☑️每週收運自由選\n" +
                        //"☑️專屬QR碼追蹤任務\n" +
                        //"☑️彈性調整預約時間",

                thumbnailImageUrl = new Uri("https://github.com/CHIAO-YA/DogPhotourl/blob/main/godphoto/0502.PNG?raw=true"),
                actions = new List<isRock.LineBot.TemplateActionBase>()
                {
                    new isRock.LineBot.UriAction()
                    {
                        label = "選擇大容量方案",
                        uri = new Uri("https://lebuleduo.vercel.app")
                    }
                }
            });

            // 創建輪播訊息
            var carouselTemplate = new isRock.LineBot.CarouselTemplate() { columns = columns };

            // 發送輪播訊息
            isRock.LineBot.Bot bot = new isRock.LineBot.Bot(channelAccessToken);
            bot.ReplyMessage(replyToken, new isRock.LineBot.TemplateMessage(carouselTemplate));
        }


    }

}
