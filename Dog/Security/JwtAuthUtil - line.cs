using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Configuration;
using Dog.Models;
using Jose;


namespace Dog.Security
{
    public class LineJwtAuthUtil
    {
        //private readonly Model1 db = new Model1(); // DB 連線
        private static readonly string secretlineKey = "ILoveCode"; // 從 appSettings 取出
       
        //產生 JWT Token，包含會員 ID、帳號、暱稱，並設定 30 分鐘後過期
        public string GetToken(string lineId,string userRole)
        {
            var payload = new Dictionary<string, object>
            {
                { "lineId",lineId },
                { "userRole", userRole },
                { "Exp", DateTime.Now.AddMinutes(30).ToString() } // JwtToken過期時效設定 30 分
            };
            // 產生 JwtToken
            return JWT.Encode(payload, Encoding.UTF8.GetBytes(secretlineKey), JwsAlgorithm.HS512);
        }

        // 刷新 Token 的有效期限，保留原有的會員資訊，但更新過期時間
        public string LineExpRefreshToken(Dictionary<string, object> tokenData)
        {
            string secretlineKey = WebConfigurationManager.AppSettings["TokenKey"];
            // payload 從原本 token 傳遞的資料沿用，並刷新效期
            var payload = new Dictionary<string, object>
            {
                { "lineId", tokenData["lineId"].ToString() },
                { "userRole", tokenData["userRole"].ToString() },
                { "Exp", DateTime.Now.AddMinutes(30).ToString() } // JwtToken過期時效刷新設定 30 分
            };
            //產生刷新時效的 JwtToken
            return JWT.Encode(payload, Encoding.UTF8.GetBytes(secretlineKey), JwsAlgorithm.HS512);
        }
        ///產生無效 JwtToken 清空資料  強制登出或取消
        public string LineRevokeToken()
        {
            string secretlineKey = "RevokeToken"; // 故意用不同的 key 生成
            var payload = new Dictionary<string, object>
            {
                { "lineId", 0 },
                { "userRole", "None" },
                { "Exp", DateTime.Now.AddDays(-15).ToString() } // 使 JwtToken 過期 失效
            };

            // 產生失效的 JwtToken
            return JWT.Encode(payload, Encoding.UTF8.GetBytes(secretlineKey), JwsAlgorithm.HS512);
        }
        // 解析 JWT Token，取得裡面的 Payload 資料
        public static Dictionary<string, object> GetlinePayload(string token)
        {
            return JWT.Decode<Dictionary<string, object>>(token, Encoding.UTF8.GetBytes(secretlineKey), JwsAlgorithm.HS512);
        }
    }
}