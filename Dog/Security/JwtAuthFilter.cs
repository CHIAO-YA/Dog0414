using Jose;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http;
using System.Web.Http.Filters;

namespace Dog.Security
{
    public class JwtAuthFilter : ActionFilterAttribute
    {
        // 加解密的 key，如果不一樣會無法成功解密
        private static readonly string secretKey = "ILoveCode";
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var request = actionContext.Request;

            // 檢查請求是否包含 JWT Token，且格式是否正確
            if (request.Headers.Authorization == null || request.Headers.Authorization.Scheme != "Bearer")
            {
                string messageJson = JsonConvert.SerializeObject(new { Status = false, Message = "請重新登入" }); // JwtToken 遺失，需導引重新登入
                var errorMessage = new HttpResponseMessage()
                {
                    ReasonPhrase = "JwtToken Lost",
                    Content = new StringContent(messageJson,
                                Encoding.UTF8,
                                "application/json")
                };
                throw new HttpResponseException(errorMessage); // Debug 模式會停在此行，點繼續執行即可
            }
            else
            {
                try
                {
                    // 有 JwtToken 且授權格式正確時執行，用 try 包住，因為如果有篡改可能解密失敗
                    // 解密後會回傳 Json 格式的物件 (即加密前的資料)
                    var jwtObject = GetToken(request.Headers.Authorization.Parameter);

                    // 檢查有效期限是否過期，如 JwtToken 過期，需導引重新登入
                    if (IsTokenExpired(jwtObject["Exp"].ToString()))
                    {
                        string messageJson = JsonConvert.SerializeObject(new { Status = false, Message = "請重新登入" }); // JwtToken 過期，需導引重新登入
                        var errorMessage = new HttpResponseMessage()
                        {
                            // StatusCode = System.Net.HttpStatusCode.Unauthorized, // 401
                            ReasonPhrase = "JwtToken Expired",
                            Content = new StringContent(messageJson,
                                Encoding.UTF8,
                                "application/json")
                        };
                        throw new HttpResponseException(errorMessage); // Debug 模式會停在此行，點繼續執行即可
                    }
                }
                catch (Exception)
                {
                    // 解密失敗
                    string messageJson = JsonConvert.SerializeObject(new { Status = false, Message = "請重新登入" }); // JwtToken 不符，需導引重新登入
                    var errorMessage = new HttpResponseMessage()
                    {
                        // StatusCode = System.Net.HttpStatusCode.Unauthorized, // 401
                        ReasonPhrase = "JwtToken NotMatch",
                        Content = new StringContent(messageJson,
                                Encoding.UTF8,
                                "application/json")
                    };
                    throw new HttpResponseException(errorMessage); // Debug 模式會停在此行，點繼續執行即可
                }
            }
            base.OnActionExecuting(actionContext);
        }

        //將 Token 解密取得夾帶的資料
        public static Dictionary<string, object> GetToken(string token)
        {
            return JWT.Decode<Dictionary<string, object>>(token, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
        }

        // 驗證 Token 是否過期
        public bool IsTokenExpired(string dateTime)
        {
            return Convert.ToDateTime(dateTime) < DateTime.Now;
        }
    }
}