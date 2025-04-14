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
    public class JwtAuthUtil
    {
        //private readonly Model1 db = new Model1(); // DB 連線
        private static readonly string secretKey = "ILoveCode"; // 從 appSettings 取出
       
        //產生 JWT Token，包含會員 ID、帳號、暱稱，並設定 30 分鐘後過期
        public string GenerateToken(int id, string account, string name)
        {
            var payload = new Dictionary<string, object>
            {
                { "Id",id },
                { "Account", account },
                { "NickName", name },
                { "Exp", DateTime.Now.AddMonths(6).ToString() } // JwtToken過期時效設定 30 分
            };
            // 產生 JwtToken
            return JWT.Encode(payload, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
        }

        // 刷新 Token 的有效期限，保留原有的會員資訊，但更新過期時間
        public string ExpRefreshToken(Dictionary<string, object> tokenData)
        {
            string secretKey = WebConfigurationManager.AppSettings["TokenKey"];
            // payload 從原本 token 傳遞的資料沿用，並刷新效期
            var payload = new Dictionary<string, object>
            {
                { "Id", (int)tokenData["Id"] },
                { "Account", tokenData["Account"].ToString() },
                { "Name", tokenData["Name"].ToString() },
                { "Exp", DateTime.Now.AddMinutes(30).ToString() } // JwtToken過期時效刷新設定 30 分
            };
            //產生刷新時效的 JwtToken
            return JWT.Encode(payload, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
        }
        ///產生無效 JwtToken 清空資料  強制登出或取消
        public string RevokeToken()
        {
            string secretKey = "RevokeToken"; // 故意用不同的 key 生成
            var payload = new Dictionary<string, object>
            {
                { "Id", 0 },
                { "Account", "None" },
                { "Name", "None" },
                { "Exp", DateTime.Now.AddDays(-15).ToString() } // 使 JwtToken 過期 失效
            };

            // 產生失效的 JwtToken
            return JWT.Encode(payload, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
        }
        // 解析 JWT Token，取得裡面的 Payload 資料
        public static Dictionary<string, object> GetPayload(string token)
        {
            return JWT.Decode<Dictionary<string, object>>(token, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
        }
    }
}