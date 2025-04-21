using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Dog.Security.LineBot;
using isRock.LineBot;

namespace Dog.Security
{
    public class LineBot
    {
        // LINE 機器人設定
        public class LineBotConfig
        {
            public string ChannelSecret { get; set; }
            public string ChannelAccessToken { get; set; }
        }

        // 簡單的訊息記錄模型
        public class MessageLog
        {
            public int Id { get; set; }
            public string UserId { get; set; }
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
            public string ReplyMessage { get; set; }
        }

    }
}