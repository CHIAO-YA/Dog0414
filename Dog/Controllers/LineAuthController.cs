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
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Data.Entity;

namespace Dog.Controllers
{
    public class LineAuthController : ApiController
    {
        Models.Model1 db = new Models.Model1();
        private readonly string channelId = "2007121127";
        private readonly string channelSecret = "d7c30599e53dc2aa970728521d61d2c3"; // 請從 LINE 開發者控制台取得
        private readonly string redirectUri = "http://4.240.61.223/auth/line-login"; // 設定正確的 redirect_uri


        [HttpPost]
        [Route("auth/line/callback")] // 處理來自前端的 code 和 role
        public async Task<IHttpActionResult> Callback([FromBody] LineCallbackRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Code) || request.Role == null)
            {
                return BadRequest("缺少必要的參數");
            }

            // Step 1: 使用授權碼換取 access token
            string tokenUrl = "https://api.line.me/oauth2/v2.1/token";
            var client = new HttpClient();
            var postData = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", request.Code },
                { "redirect_uri", redirectUri },
                { "client_id", channelId },
                { "client_secret", channelSecret }
            };

            var content = new FormUrlEncodedContent(postData);
            var response = await client.PostAsync(tokenUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest($"交換 access token 失敗：{responseBody}");
            }

            var tokenData = JsonConvert.DeserializeObject<dynamic>(responseBody);
            string accessToken = tokenData.access_token;

            // Step 2: 使用 access token 請求 LINE 用戶資料
            string userProfileUrl = "https://api.line.me/v2/profile";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var profileResponse = await client.GetAsync(userProfileUrl);
            var profileData = await profileResponse.Content.ReadAsStringAsync();

            if (!profileResponse.IsSuccessStatusCode)
            {
                return InternalServerError(new Exception("取得用戶資料失敗：" + profileData));
            }

            var userData = JsonConvert.DeserializeObject<dynamic>(profileData);
            string lineId = userData.userId;
            string lineName = userData.displayName;
            string linePicUrl = userData.pictureUrl;

            // Step 3: 傳送資料到 /api/auth/user
            var userRequest = new
            {
                LineId = lineId,
                LineName = lineName,
                LinePicUrl = linePicUrl,
                Role = request.Role
            };

            var apiClient = new HttpClient();
            // 使用當前應用程序的 URL
            var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
            var userApiUrl = $"{baseUrl}/api/auth/user";

            var userContent = new StringContent(JsonConvert.SerializeObject(userRequest), System.Text.Encoding.UTF8, "application/json");
            var apiResponse = await apiClient.PostAsync(userApiUrl, userContent);
            var apiResult = await apiResponse.Content.ReadAsStringAsync();

            if (!apiResponse.IsSuccessStatusCode)
            {
                return InternalServerError(new Exception("傳送用戶資料到 /api/auth/user 失敗：" + apiResult));
            }

            // 將 API 回應返回給前端
            return Ok(JsonConvert.DeserializeObject(apiResult));
        }

        [HttpPost]
        [Route("api/auth/user")]
        public async Task<IHttpActionResult> UserRegister([FromBody] UserRegisterRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.LineId))
            {
                return BadRequest("無效的請求參數");
            }

            try
            {
                // 解析角色 (1 = 使用者/customer, 2 = 接單員/deliver)
                int userRoleNumber;
                if (request.Role.ToLower() == "customer" || request.Role == "1")
                {
                    userRoleNumber = 1;
                }
                else if (request.Role.ToLower() == "deliver" || request.Role == "2")
                {
                    userRoleNumber = 2;
                }
                else
                {
                    userRoleNumber = 1; // 默認為顧客
                }

                // 檢查該 LINE ID 是否已存在於資料庫
                var existingRoles = db.Users.Where(u => u.LineId == request.LineId)
                                       .Select(u => (int)u.Roles)
                                       .ToList();

                // 如果用戶資料不存在，則新增資料
                if (!existingRoles.Any())
                {
                    var newUser = new Users
                    {
                        LineId = request.LineId,
                        LineName = request.LineName,
                        LinePicUrl = request.LinePicUrl,
                        CreatedAt = DateTime.Now,
                        Roles = (Role)userRoleNumber,
                        Number = GetUserNumber(userRoleNumber)
                    };
                    db.Users.Add(newUser);
                    await db.SaveChangesAsync();
                }
                else if (!existingRoles.Contains(userRoleNumber))
                {
                    // 如果該用戶已有其他角色，但沒有這個角色，則新增一筆資料
                    var newRoleEntry = new Users
                    {
                        LineId = request.LineId,
                        LineName = request.LineName,
                        LinePicUrl = request.LinePicUrl,
                        CreatedAt = DateTime.Now,
                        Roles = (Role)userRoleNumber,
                        Number = GetUserNumber(userRoleNumber)
                    };
                    db.Users.Add(newRoleEntry);
                    await db.SaveChangesAsync();
                }

                // 產生 JWT Token
                LineJwtAuthUtil jwtAuthUtil = new LineJwtAuthUtil();
                string jwtToken = jwtAuthUtil.GetToken(request.LineId, userRoleNumber.ToString());

                // 根據 userRole 決定角色名稱和重定向 URL
                string roleName = userRoleNumber == 1 ? "customer" : "deliver";
                string redirectUrl = userRoleNumber == 1 ? "https://localhost:5173/customer" : "https://localhost:5173/deliver";

                // 返回成功的訊息
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = "成功取得並儲存用戶資料",
                    token = jwtToken,
                    role = userRoleNumber,
                    roleName = roleName,
                    redirectUrl = redirectUrl
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private string GetUserNumber(int role)  // 根據角色自動產生唯一編號不重複
        {
            string prefix = role == 2 ? "D" : "U";
            string number;
            Random rand = new Random();
            do
            {
                int num = rand.Next(0, 10000); // 0000 ~ 9999
                number = prefix + num.ToString("D4");
            } while (db.Users.Any(u => u.Number == number)); // 避免重複

            return number;
        }
    }

    // 定義來自前端的請求資料結構
    public class LineCallbackRequest
    {
        public string Code { get; set; }  // LINE 授權碼
        public string Role { get; set; }  // 角色 (如 "customer", "deliver" 或數字 "1", "2")
    }

    // 用於接收 LINE 用戶資料的模型
    public class UserRegisterRequest
    {
        public string LineId { get; set; }
        public string LineName { get; set; }
        public string LinePicUrl { get; set; }
        public string Role { get; set; }  // 修改為字串類型
    }




    [HttpGet]
    [Route("auth/line-login")]//獲取 LINE 用戶資料
    public async Task<IHttpActionResult> Callback(string code, string state)
    {
        string aa = "";
        string channelId = "2007121127";
        string channelSecret = "d7c30599e53dc2aa970728521d61d2c3";
        string redirectUri = ConfigurationManager.AppSettings["redirect_uri44388"];
        // Step 1: 使用授權碼換取 access token
        string tokenUrl = "https://api.line.me/oauth2/v2.1/token";
        var client = new HttpClient();
        var postData = new Dictionary<string, string>
    {
    { "grant_type", "authorization_code" },
    { "code", code },//左邊LINE API 規定的參數名稱，右邊是從URL取得的參數
    { "redirect_uri", redirectUri },
    { "client_id", channelId },
    { "client_secret", channelSecret }
    };
        //向 LINE API 發送 HTTP POST 請求取得回應內容
        var content = new FormUrlEncodedContent(postData);
        var response = await client.PostAsync(tokenUrl, content);//非同步請求，等待 response（回應物件）回來再繼續執行
        aa += "01, ";
        var responseBody = await response.Content.ReadAsStringAsync();//LINE 回傳的 JSON 資料
        aa += "02, ";
        if (response.IsSuccessStatusCode)
        {
            // Step 2: 解析 access token
            var tokenData = JsonConvert.DeserializeObject<dynamic>(responseBody);
            string accessToken = tokenData.access_token;
            aa += "03, ";
            // Step 3: 使用 access token 請求用戶資料
            string userProfileUrl = "https://api.line.me/v2/profile";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var profileResponse = await client.GetAsync(userProfileUrl);
            var profileData = await profileResponse.Content.ReadAsStringAsync();
            aa += "04, ";
            // Step 4: 解析用戶資料
            var userData = JObject.Parse(profileData);
            string lineId = (string)userData["userId"];
            string lineName = (string)userData["displayName"];
            string linePicUrl = (string)userData["pictureUrl"];

            // 解析 state，決定角色 (1 = 使用者, 2 = 接單員)
            int userRole = int.TryParse(state, out int role) && role == 2 ? 2 : 1;
            aa += "05, ";
            using (var dbContext = new Model1())
            {
                try
                {
                    // 檢查該 LINE ID 是否已存在於資料庫
                    var existingRoles = dbContext.Users.Where(u => u.LineId == lineId).Select(u => (int)u.Roles).ToList();
                    // 如果用戶資料不存在，則新增資料
                    if (!existingRoles.Any())
                    {
                        var newUser = new Users
                        {
                            LineId = lineId,
                            LineName = lineName,
                            LinePicUrl = linePicUrl,
                            CreatedAt = DateTime.Now,
                            Roles = (Role)userRole, // 直接存入當前角色
                            Number = GetUserNumber(userRole)
                        };
                        dbContext.Users.Add(newUser);
                        await dbContext.SaveChangesAsync();
                        Console.WriteLine("新增用戶：" + userRole);
                    }
                    else if (!existingRoles.Contains(userRole))
                    {
                        // 如果該用戶已有其他角色，但沒有這個角色，則新增一筆資料
                        var newRoleEntry = new Users
                        {
                            LineId = lineId,
                            LineName = lineName,
                            LinePicUrl = linePicUrl,
                            CreatedAt = DateTime.Now,
                            Roles = (Role)userRole,
                            Number = GetUserNumber(userRole)
                        };
                        dbContext.Users.Add(newRoleEntry);
                        await dbContext.SaveChangesAsync();
                        Console.WriteLine("新增：" + userRole);
                    }
                    else
                    {
                        Console.WriteLine("用戶已擁有：" + userRole);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ 資料庫儲存失敗：" + ex.Message);
                    return InternalServerError(ex);
                }
            }

            // ✅ 產生 JWT Token
            LineJwtAuthUtil jwtAuthUtil = new LineJwtAuthUtil();
            string jwtToken = jwtAuthUtil.GetToken(lineId, userRole.ToString());
            // 根據 userRole 決定角色名稱
            string roleName = userRole == 1 ? "customer" : "deliver";
            string redirectUrl = userRole == 1 ? "https://localhost:5173/customer" : "https://localhost:5173/deliver";
            // Step 6: 返回成功的訊息
            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "成功取得並儲存用戶資料",
                profileData,
                token = jwtToken,
                role = userRole,       // 保留數字值
                roleName = roleName,    // 添加文字表示給前端使用
                redirectUrl = redirectUrl
            });
        }
        else
        {
            return Ok(new
            {
                aa,
                responseBody,
                x = "交換 access token 失敗",
            });

            //return BadRequest("交換 access token 失敗");
        }
    }
    private string GetUserNumber(int role)//根據角色自動產生唯一編號不重複
    {
        string prefix = role == 2 ? "D" : "U";
        string number;
        Random rand = new Random();
        do
        {
            int num = rand.Next(0, 10000); // 0000 ~ 9999
            number = prefix + num.ToString("D4");
        } while (db.Users.Any(u => u.Number == number)); // 避免重複

        return number;
    }
}

