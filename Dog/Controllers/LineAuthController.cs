using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Dog.Models;
using Dog.Migrations;
using Newtonsoft.Json.Linq;
using Dog.Security;
using System.Configuration;
using static System.Net.WebRequestMethods;
using System.Text;
using System.Data.Entity;

namespace Dog.Controllers
{
    public class LineAuthController : ApiController
    {
        Models.Model1 db = new Models.Model1();

        [HttpGet]
        [Route("Callback")]
        public IHttpActionResult Callback(string code, string state)
        {
            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得",
                code,
                state
            });
        }

        [HttpPost]
        [Route("auth/line/callback")] // 接收前端傳來的LINE授權碼
        public async Task<IHttpActionResult> LineLoginFromFrontend(LineLoginRequest request)  
        {//公開 非同步  從「HTTP 請求的 body」
            //// 檢查請求是否有效
            if (request.code == null || string.IsNullOrEmpty(request.code))
            {
                return BadRequest("授權碼不能為空");
            }

            // 從配置檔或環境設定獲取LINE的應用程式資訊
            string channelId = "2007121127";
            string channelSecret = "d7c30599e53dc2aa970728521d61d2c3";
            bool isDev = true;
            //string redirectUri = isDev
            //    ? "http://localhost:5173/auth/line/callback"
            //    : "https://lebuleduo.vercel.app/auth/line/callback";

            string redirectUri = "https://lebuleduo.vercel.app/#/auth/line/callback";//LINE 授權過程中使用的重定向 URL
            ////http://localhost:5173/#/auth/line/callback
            ////https://lebuleduo.vercel.app/#/auth/line/callback
            try
            {
                //「我拿使用者從 LINE 那邊登入完後得到的授權碼 code，去跟 LINE 官方說：
                //『嗨，我有這個授權碼，請給我一個 access token，這樣我就能取得這位使用者的基本資料了！』
                string tokenUrl = "https://api.line.me/oauth2/v2.1/token";//LINE 交換 Token 的 API
                var client = new HttpClient();//建立一個 HTTP 用戶端，準備發送請求給 LINE
                var postData = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },//拿「授權碼」來換 Token
                    { "code", request.code }, // 前端傳來的授權碼
                    { "redirect_uri", redirectUri },//LINE 授權過程中使用的重定向 URL
                    { "client_id", channelId },
                    { "client_secret", channelSecret }
                };

                // 向LINE發送HTTP請求
                var content = new FormUrlEncodedContent(postData);//postData 資料打包成一種 HTTP 可以理解的 x-www-form-urlencoded 這種格式
                var response = await client.PostAsync(tokenUrl, content);//非同步地發送一個 POST 請求(LINE API 的網址/打包好的表單資料)
                var responseBody = await response.Content.ReadAsStringAsync();//把 LINE 回傳的結果讀成文字（JSON 格式）
                //await 表示這是一個需要等待的動作，等 LINE 回應以後才會往下執行
                if (!response.IsSuccessStatusCode)
                {
                    // 如果LINE API回傳錯誤
                    return BadRequest($"交換access token失敗: {responseBody}, {redirectUri}, {request.code}");
                }

                // 步驟2: 解析回傳的JSON 字串 responseBody資料，取出access token
                var tokenData = JsonConvert.DeserializeObject<dynamic>(responseBody);//用 dynamic，你不用先定義一個 class，只要知道資料裡有哪些欄位就能用
                string accessToken = tokenData.access_token;//解析出來的 tokenData 裡，把 access_token 抓出來存到變數 accessToken 裡

                // 步驟3: 使用access token向LINE請求用戶資料
                // 有了存取權杖，現在可以獲取用戶的個人資訊
                string userProfileUrl = "https://api.line.me/v2/profile";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var profileResponse = await client.GetAsync(userProfileUrl);

                if (!profileResponse.IsSuccessStatusCode)
                {
                    return BadRequest("獲取用戶資料失敗");
                }
                //從 HTTP 回應中讀取資料  用非同步的方式//從伺服器回傳的資料中，把使用者的個人資料讀成一個字串，存進 profileData 變數裡
                var profileData = await profileResponse.Content.ReadAsStringAsync();

                // 步驟4: 解析用戶資料，取出需要的資訊
                var userData = JObject.Parse(profileData);
                string lineId = (string)userData["userId"];
                string lineName = (string)userData["displayName"];
                string linePicUrl = (string)userData["pictureUrl"];

                //解析state，決定用戶角色(1 = 使用者, 2 = 接單員)
                // 前端會傳遞表示角色的state參數
                int userRole = request.role == "customer" ? 1 : 2;
                int UsersID = 0;
                // 步驟5: 儲存用戶資料到資料庫
                using (var db = new Model1())
                {
                    try
                    {
                        // 檢查該LINE ID是否已存在於資料庫中
                        var LineIdRoles = db.Users .Where(u => u.LineId == lineId)
                            .Select(u => (int)u.Roles) .ToList();
                        
                        if (!LineIdRoles.Any())
                        {
                            // 如果用戶完全不存在，建立新用戶
                            var newUser = new Users
                            {
                                LineId = lineId,
                                LineName = lineName,
                                LinePicUrl = linePicUrl,
                                CreatedAt = DateTime.Now,
                                Roles = (Role)userRole,
                                Number = GetUserNumber(userRole),
                                IsOnline = userRole == 2 ? true : false,
                            };
                            db.Users.Add(newUser);
                            await db.SaveChangesAsync();
                            UsersID = newUser.UsersID;
                        }
                        else if (!LineIdRoles.Contains(userRole))
                        {
                            // 如果用戶已存在但沒有這個角色，為其新增這個角色
                            var newRoleUser = new Users
                            {
                                LineId = lineId,
                                LineName = lineName,
                                LinePicUrl = linePicUrl,
                                CreatedAt = DateTime.Now,
                                Roles = (Role)userRole,
                                Number = GetUserNumber(userRole),
                                IsOnline = userRole == 2 ? true : false,
                            };
                            db.Users.Add(newRoleUser);
                            await db.SaveChangesAsync();//非同步地儲存
                            UsersID = newRoleUser.UsersID;
                        }
                        else
                        {
                            // 如果用戶已有此角色，取得現有用戶ID
                            UsersID = db.Users .Where(u => u.LineId == lineId &&
                            u.Roles == (Role)userRole).Select(u => u.UsersID).FirstOrDefault();
                        }

                    }
                    catch (Exception ex)
                    {
                        return InternalServerError(ex);
                    }
                }
                // 步驟6: 產生JWT Token，這是用戶在後續訪問API時的身份憑證
                LineJwtAuthUtil jwtAuthUtil = new LineJwtAuthUtil();
                string jwtToken = jwtAuthUtil.GetToken(lineId, "customer");

                // 根據角色準備回傳資訊
                string roleName = request.role;

                // 步驟7: 返回成功的訊息和令牌給前端
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = "成功取得並儲存用戶資料",
                    profileData = userData,
                    token = jwtToken,
                    role = userRole,
                    roleName,
                    LineId = lineId,
                    UsersID
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        //[HttpGet]
        //[Route("auth/line-login")]//獲取 LINE 用戶資料
        //public async Task<IHttpActionResult> Callback(string code, string state)
        //{
        //    string aa = "";
        //    string channelId = "2007121127";
        //    string channelSecret = "d7c30599e53dc2aa970728521d61d2c3";
        //    string redirectUri = ConfigurationManager.AppSettings["redirect"];
        //    // Step 1: 使用授權碼換取 access token
        //    string tokenUrl = "https://api.line.me/oauth2/v2.1/token";
        //    var client = new HttpClient();
        //    var postData = new Dictionary<string, string>
        //    {
        //    { "grant_type", "authorization_code" },
        //    { "code", code },//左邊LINE API 規定的參數名稱，右邊是從URL取得的參數
        //    { "redirect_uri", redirectUri },
        //    { "client_id", channelId },
        //    { "client_secret", channelSecret }
        //    };
        //    //向 LINE API 發送 HTTP POST 請求取得回應內容
        //    var content = new FormUrlEncodedContent(postData);
        //    var response = await client.PostAsync(tokenUrl, content);//非同步請求，等待 response（回應物件）回來再繼續執行
        //    aa += "01, ";
        //    var responseBody = await response.Content.ReadAsStringAsync();//LINE 回傳的 JSON 資料
        //    aa += "02, ";
        //    if (response.IsSuccessStatusCode)
        //    {
        //        // Step 2: 解析 access token
        //        var tokenData = JsonConvert.DeserializeObject<dynamic>(responseBody);
        //        string accessToken = tokenData.access_token;
        //        aa += "03, ";
        //        // Step 3: 使用 access token 請求用戶資料
        //        string userProfileUrl = "https://api.line.me/v2/profile";
        //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        //        var profileResponse = await client.GetAsync(userProfileUrl);
        //        var profileData = await profileResponse.Content.ReadAsStringAsync();
        //        aa += "04, ";
        //        // Step 4: 解析用戶資料
        //        var userData = JObject.Parse(profileData);
        //        string lineId = (string)userData["userId"];
        //        string lineName = (string)userData["displayName"];
        //        string linePicUrl = (string)userData["pictureUrl"];

        //        // 解析 state，決定角色 (1 = 使用者, 2 = 接單員)
        //        int userRole = int.TryParse(state, out int role) && role == 2 ? 2 : 1;
        //        aa += "05, ";
        //        using (var dbContext = new Model1())
        //        {
        //            try
        //            {
        //                // 檢查該 LINE ID 是否已存在於資料庫
        //                var existingRoles = dbContext.Users.Where(u => u.LineId == lineId).Select(u => (int)u.Roles).ToList();
        //                // 如果用戶資料不存在，則新增資料
        //                if (!existingRoles.Any())
        //                {
        //                    var newUser = new Users
        //                    {
        //                        LineId = lineId,
        //                        LineName = lineName,
        //                        LinePicUrl = linePicUrl,
        //                        CreatedAt = DateTime.Now,
        //                        Roles = (Role)userRole, // 直接存入當前角色
        //                        Number = GetUserNumber(userRole)
        //                    };
        //                    dbContext.Users.Add(newUser);
        //                    await dbContext.SaveChangesAsync();
        //                    Console.WriteLine("新增用戶：" + userRole);
        //                }
        //                else if (!existingRoles.Contains(userRole))
        //                {
        //                    // 如果該用戶已有其他角色，但沒有這個角色，則新增一筆資料
        //                    var newRoleEntry = new Users
        //                    {
        //                        LineId = lineId,
        //                        LineName = lineName,
        //                        LinePicUrl = linePicUrl,
        //                        CreatedAt = DateTime.Now,
        //                        Roles = (Role)userRole,
        //                        Number = GetUserNumber(userRole)
        //                    };
        //                    dbContext.Users.Add(newRoleEntry);
        //                    await dbContext.SaveChangesAsync();
        //                    Console.WriteLine("新增：" + userRole);
        //                }
        //                else
        //                {
        //                    Console.WriteLine("用戶已擁有：" + userRole);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine("❌ 資料庫儲存失敗：" + ex.Message);
        //                return InternalServerError(ex);
        //            }
        //        }

        //        // ✅ 產生 JWT Token
        //        LineJwtAuthUtil jwtAuthUtil = new LineJwtAuthUtil();
        //        string jwtToken = jwtAuthUtil.GetToken(lineId, userRole.ToString());
        //        // 根據 userRole 決定角色名稱
        //        string roleName = userRole == 1 ? "customer" : "deliver";
        //        string redirectUrl = userRole == 1 ? "https://localhost:5173/customer" : "https://localhost:5173/deliver";
        //        // Step 6: 返回成功的訊息
        //        return Ok(new
        //        {
        //            statusCode = 200,
        //            status = true,
        //            message = "成功取得並儲存用戶資料",
        //            profileData,
        //            token = jwtToken,
        //            role = userRole,       // 保留數字值
        //            roleName = roleName,    // 添加文字表示給前端使用
        //            redirectUrl = redirectUrl
        //        });
        //    }
        //    else
        //    {
        //        return Ok(new
        //        {
        //            aa,
        //            responseBody,
        //            x = "交換 access token 失敗",
        //        });

        //        //return BadRequest("交換 access token 失敗");
        //    }
        //}

        public class LineLoginRequest
        {
            public string code { get; set; } // LINE授權碼
            public string role { get; set; } // 表示角色的狀態參數
        }

        private string GetUserNumber(int role)//根據角色自動產生唯一編號不重複
        {
            string only = role == 2 ? "D" : "U";
            string number;
            Random rand = new Random();
            
            do
            {
                int num = rand.Next(0, 10000); // 0000 ~ 9999
                number = only + num.ToString("D4");
            } while (db.Users.Any(u => u.Number == number)); // 避免重複

            return number;
        }
    }
}