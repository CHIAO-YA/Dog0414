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

namespace Dog.Controllers
{
    public class LineAuthController : ApiController
    {
        Models.Model1 db = new Models.Model1();

        [HttpGet]
        [Route("auth/line-login")]//獲取 LINE 用戶資料
        public async Task<IHttpActionResult> Callback(string code, string state)
        {
            string channelId = "2007121127";
            string channelSecret = "d7c30599e53dc2aa970728521d61d2c3";
            string redirectUri = "https://localhost:5173/auth/line-login";
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
            var responseBody = await response.Content.ReadAsStringAsync();//LINE 回傳的 JSON 資料

            if (response.IsSuccessStatusCode)
            {
                // Step 2: 解析 access token
                var tokenData = JsonConvert.DeserializeObject<dynamic>(responseBody);
                string accessToken = tokenData.access_token;

                // Step 3: 使用 access token 請求用戶資料
                string userProfileUrl = "https://api.line.me/v2/profile";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var profileResponse = await client.GetAsync(userProfileUrl);
                var profileData = await profileResponse.Content.ReadAsStringAsync();

                // Step 4: 解析用戶資料
                var userData = JObject.Parse(profileData);
                string lineId = (string)userData["userId"];
                string lineName = (string)userData["displayName"];
                string linePicUrl = (string)userData["pictureUrl"];

                // 解析 state，決定角色 (1 = 使用者, 2 = 接單員)
                int userRole = int.TryParse(state, out int role) && role == 2 ? 2 : 1;

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
                //var ExpRefresh = new Dictionary<string, object>
                //{
                //    { "lineId", lineId },
                //    { "userRole", userRole.ToString() }
                //};
                //string jwtToken2 = jwtAuthUtil.LineExpRefreshToken(ExpRefresh);


                // Step 6: 返回成功的訊息
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = "成功取得並儲存用戶資料",
                    profileData,
                    token = jwtToken
                });
            }
            else
            {
                return BadRequest("交換 access token 失敗");
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
}