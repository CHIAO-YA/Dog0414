using isRock.LineBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;


namespace Dog.Security
{
    public class LineBotServices
    {
        private readonly string _channelAccessToken;
        private readonly string _channelSecret;
       // private readonly LineBot _config;

        public LineBotServices(LineBot.LineBotConfig config)
        {
            _channelAccessToken = config.ChannelAccessToken;
            _channelSecret = config.ChannelSecret;
        }


        // 處理收到的訊息
        public async Task<object> HandleWebhookAsync(isRock.LineBot.ReceivedMessage receivedMessage)
        {
            var lineEvent = receivedMessage.events.FirstOrDefault();
            if (lineEvent == null) return null;

            var userId = lineEvent.source.userId;
            var bot = new isRock.LineBot.Bot(_channelAccessToken);

            try
            {
                // 處理不同類型的事件
                switch (lineEvent.type)
                {
                    case "message":
                        if (lineEvent.message.type == "text")
                        {
                            return await HandleTextMessage(lineEvent, bot);
                        }
                        break;
                    case "follow":
                        return HandleFollowEvent(lineEvent, bot);
                    case "unfollow":
                        // 處理取消追蹤事件
                        break;
                    case "join":
                        // 處理加入群組事件
                        break;
                    case "leave":
                        // 處理離開群組事件
                        break;
                    case "postback":
                        // 處理 postback 事件
                        break;
                }
            }
            catch (Exception ex)
            {
                // 處理例外狀況
                bot.PushMessage(userId, "抱歉，發生了一些錯誤。");
                System.Diagnostics.Debug.WriteLine($"LINE Bot 錯誤: {ex.Message}");
            }

            return null;
        }

        // 處理文字訊息
        private async Task<object> HandleTextMessage(Event lineEvent, isRock.LineBot.Bot bot)
        {
            var userId = lineEvent.source.userId;
            var userMessage = lineEvent.message.text;
            var replyToken = lineEvent.replyToken;

            // 根據用戶訊息內容回應不同的訊息
            if (userMessage.Contains("你好") || userMessage.Contains("hello"))
            {
                bot.ReplyMessage(replyToken, "您好！很高興為您服務！");
            }
            else if (userMessage.Contains("功能") || userMessage.Contains("help"))
            {
                bot.ReplyMessage(replyToken, "我可以提供以下服務：\n1. 回答常見問題\n2. 提供最新消息\n3. 接收您的意見回饋");
            }
            else
            {
                bot.ReplyMessage(replyToken, $"我收到了您的訊息：{userMessage}");
            }

            return await Task.FromResult<object>(true);
        }

        // 處理追蹤事件
        private object HandleFollowEvent(Event lineEvent, isRock.LineBot.Bot bot)
        {
            var userId = lineEvent.source.userId;
            var replyToken = lineEvent.replyToken;

            // 歡迎訊息
            bot.ReplyMessage(replyToken, "感謝您加入我們！我們將為您提供最優質的服務。");

            return null;
        }

        // 推送訊息給特定用戶
        public void PushMessage(string userId, string message)
        {
            var bot = new isRock.LineBot.Bot(_channelAccessToken);
            bot.PushMessage(userId, message);
        }
    }
}