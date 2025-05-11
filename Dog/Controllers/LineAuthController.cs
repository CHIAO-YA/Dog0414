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
using Microsoft.Ajax.Utilities;
using System.Web;

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
        [Route("auth/line/callback")]
        public async Task<IHttpActionResult> LineLoginFromFrontend(LineLoginRequest request)
        {
            if (request.code == null || string.IsNullOrEmpty(request.code))
            {
                return BadRequest("授權碼不能為空");
            }
            int? usersId = null;//使用者id在url中通常都是字串 所以要轉成int
            if (!string.IsNullOrEmpty(request.usersId))
            {
                int parsedId;
                if (int.TryParse(request.usersId, out parsedId))
                {
                    usersId = parsedId;
                }
            }
            string channelId = "2007121127";
            string channelSecret = "d7c30599e53dc2aa970728521d61d2c3";
            string redirectUri = "https://lebuleduo.vercel.app/#/auth/line/callback";//登入頁面
            //http://localhost:5173/#/auth/line/callback
            //https://lebuleduo.vercel.app/#/auth/line/callback
            try
            {
                //拿到授權碼 code，去跟 LINE 官方換 access token取得使用者資料
                string tokenUrl = "https://api.line.me/oauth2/v2.1/token";//LINE 交換 Token 的 API
                var client = new HttpClient();//建立一個 HTTP 用戶端，準備發送請求給 LINE
                var postData = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },//拿授權碼換 Token
                    { "code", request.code }, // 前端傳來的授權碼
                    { "redirect_uri", redirectUri },//重定向 URL
                    { "client_id", channelId },
                    { "client_secret", channelSecret }
                };
                //向LINE發送HTTP請求
                var content = new FormUrlEncodedContent(postData);//postData json格式
                var response = await client.PostAsync(tokenUrl, content);//非同步等待發送POST(LINE API 的網址/打包好的表單資料)
                var responseBody = await response.Content.ReadAsStringAsync();//把LINE回傳的結果讀成JSON格式
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest($"交換access token失敗: {responseBody}, {redirectUri}, {request.code}");
                }
                //解析access token
                var tokenData = JsonConvert.DeserializeObject<dynamic>(responseBody);//反序列轉成物件
                string accessToken = tokenData.access_token;//解析出來的 tokenData 裡，把 access_token 抓出來存到變數 accessToken 裡

                // 步驟3: 使用access token向LINE請求用戶資料
                string userProfileUrl = "https://api.line.me/v2/profile";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var profileResponse = await client.GetAsync(userProfileUrl);
                if (!profileResponse.IsSuccessStatusCode)
                {
                    return BadRequest("獲取用戶資料失敗");
                }
                //從 HTTP 回應中讀取資料 用非同步等待從伺服器回傳使用者資料讀成字串，存進 profileData 變數裡
                var profileData = await profileResponse.Content.ReadAsStringAsync();

                var userData = JObject.Parse(profileData);
                string lineId = (string)userData["userId"];
                string lineName = (string)userData["displayName"];
                string linePicUrl = (string)userData["pictureUrl"];

                int userRole = request.role == "customer" ? 1 : 2;

                var result = await ProcessUserData(
                    lineId,
                    lineName,
                    linePicUrl,
                    userRole,
                    request.msgId,
                    request.usersId
                );
                //JWT Token身份憑證
                LineJwtAuthUtil jwtAuthUtil = new LineJwtAuthUtil();
                string jwtToken = jwtAuthUtil.GetToken(lineId, "customer");//不做資料比對 較單純
                //string jwtToken = jwtAuthUtil.GetToken(lineId, request.role);
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = "成功取得並儲存用戶資料",
                    profileData = userData,
                    token = jwtToken,
                    role = userRole,
                    roleName = request.role,
                    LineId = lineId,
                    result.usersId,
                    result.Number,
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        private async Task<UserProcessResult> ProcessUserData(string lineId, string lineName, string linePicUrl, int userRole, string msgId = null, string userId = null)
        {
            int usersId = 0;
            string number = "";
            if (!Enum.IsDefined(typeof(Role), userRole))
            {
                userRole = 1; // 默認為使用者
            }
            using (var db = new Model1())
            {
                try
                {
                    // 接單員
                    if (userRole == 2)
                    {
                        var existingDeliver = await db.Users
                            .FirstOrDefaultAsync(u => u.LineId == lineId && u.Roles == (Role)userRole);
                        if (existingDeliver != null)
                        {
                            existingDeliver.LineName = lineName;
                            existingDeliver.LinePicUrl = linePicUrl;
                            existingDeliver.IsOnline = true; 
                            await db.SaveChangesAsync();

                            return new UserProcessResult
                            {
                                usersId = existingDeliver.UsersID,
                                MsgId = existingDeliver.MessageuserId,
                                Number = existingDeliver.Number
                            };
                        }
                    }
                    // LINE 登入後再綁定帳號 → 合併資料、刪除臨時帳號
                    if (!string.IsNullOrEmpty(userId))
                    {
                        int bindedUserId;
                        if (int.TryParse(userId, out bindedUserId))
                        {
                            // 找到主帳號（綁定後取得的 userId）
                            var targetUser = await db.Users.FirstOrDefaultAsync(u => u.UsersID == bindedUserId && u.Roles == (Role)userRole);
                            if (targetUser != null)
                            {
                                // 找到 LINE 登入時產生的臨時帳號（同 LineId + 同角色 + 不是同一個 UsersID）
                                var tempUser = await db.Users.FirstOrDefaultAsync(u =>
                                    u.LineId == lineId && u.Roles == (Role)userRole && u.UsersID != targetUser.UsersID);
                                if (tempUser != null)
                                {
                                    targetUser.LineId = lineId;
                                    targetUser.LineName = tempUser.LineName;
                                    targetUser.LinePicUrl = tempUser.LinePicUrl;
                                    // 只有角色為 1 才處理 msgId
                                    if (userRole == 1)
                                    {
                                        if (string.IsNullOrEmpty(targetUser.MessageuserId) && !string.IsNullOrEmpty(tempUser.MessageuserId))
                                        {
                                            targetUser.MessageuserId = tempUser.MessageuserId;
                                        }
                                        if (!string.IsNullOrEmpty(msgId))
                                        {
                                            targetUser.MessageuserId = msgId;
                                        }
                                    }
                                    if (string.IsNullOrEmpty(targetUser.Number))
                                    {
                                        targetUser.Number = GetUserNumber(userRole);
                                    }
                                    db.Users.Remove(tempUser);
                                }
                                else
                                {
                                    targetUser.LineId = lineId;
                                    targetUser.LineName = lineName;
                                    targetUser.LinePicUrl = linePicUrl;
                                    if (userRole == 1 && !string.IsNullOrEmpty(msgId))
                                    {
                                        targetUser.MessageuserId = msgId;
                                    }
                                    if (string.IsNullOrEmpty(targetUser.Number))
                                    {
                                        targetUser.Number = GetUserNumber(userRole);
                                    }
                                }
                                await db.SaveChangesAsync();
                                return new UserProcessResult
                                {
                                    usersId = targetUser.UsersID,
                                    MsgId = targetUser.MessageuserId,
                                    Number = targetUser.Number
                                };
                            }
                        }
                    }

                    // 如果有提供 userId 參數 (通常是客人 role=1)，先根據 userId 檢查 
                    if (!string.IsNullOrEmpty(userId))
                    {
                        int UsersID;
                        if (int.TryParse(userId, out UsersID))
                        {
                            var existUsersID = await db.Users.FirstOrDefaultAsync(u => u.UsersID == UsersID);
                            if (existUsersID != null)
                            {
                                if (existUsersID.Roles != (Role)userRole)
                                {
                                    // 角色不同，不做任何事，讓程式繼續執行到下一個判斷
                                }
                                else
                                {
                                    // 用戶已存在，更新 LINE 相關資訊
                                    existUsersID.LineId = lineId;
                                    existUsersID.LineName = lineName;
                                    existUsersID.LinePicUrl = linePicUrl;

                                    // 如果是客人且有 msgId，則更新 msgId
                                    if (userRole == 1 && !string.IsNullOrEmpty(msgId))
                                    {
                                        existUsersID.MessageuserId = msgId;
                                    }

                                    // 如果沒有編號，則生成一個
                                    if (string.IsNullOrEmpty(existUsersID.Number))
                                    {
                                        existUsersID.Number = GetUserNumber(userRole);
                                    }

                                    await db.SaveChangesAsync();
                                    usersId = existUsersID.UsersID;
                                    return new UserProcessResult
                                    {
                                        usersId = usersId,
                                        MsgId = existUsersID.MessageuserId,
                                        Number = existUsersID.Number
                                    };
                                }
                            }
                        }
                    }

                    // 檢查 LINE ID 是否已有相同角色的記錄
                    var existUserRole = await db.Users
                        .Where(u => u.LineId == lineId && u.Roles == (Role)userRole)
                        .FirstOrDefaultAsync();

                    if (existUserRole != null)
                    {
                        // 已存在相同 LineId 和角色的用戶，更新資料
                        existUserRole.LineName = lineName;
                        existUserRole.LinePicUrl = linePicUrl;

                        // 如果是客人角色且有 msgId，則更新 msgId
                        if (userRole == 1 && !string.IsNullOrEmpty(msgId))
                        {
                            existUserRole.MessageuserId = msgId;
                        }

                        await db.SaveChangesAsync();
                        usersId = existUserRole.UsersID;
                        number = existUserRole.Number;
                        return new UserProcessResult
                        {
                            usersId = usersId,
                            MsgId = existUserRole.MessageuserId,
                            Number = number
                        };
                    }

                    //檢查是否存在不同角色但相同 LineId 的用戶
                    var existingUser = await db.Users
                        .Where(u => u.LineId == lineId && u.Roles != (Role)userRole)
                        .FirstOrDefaultAsync();

                    if (existingUser != null)
                    {
                        // 發現相同 LINE ID 但不同角色的用戶
                        // 創建新記錄，讓一個 LINE 帳號可以有多種角色
                        var newRoleUser = new Users
                        {
                            LineId = lineId,
                            LineName = lineName,
                            LinePicUrl = linePicUrl,
                            CreatedAt = DateTime.Now,
                            Roles = (Role)userRole,
                            Number = GetUserNumber(userRole),
                            IsOnline = userRole == 2, // 接單員預設為在線
                            MessageuserId = userRole == 1 ? msgId : null // 只有客人才綁定 msgId
                        };

                        db.Users.Add(newRoleUser);
                        await db.SaveChangesAsync();
                        usersId = newRoleUser.UsersID;
                        number = newRoleUser.Number;

                        return new UserProcessResult
                        {
                            usersId = usersId,
                            MsgId = newRoleUser.MessageuserId,
                            Number = number
                        };
                    }
                    else
                    {
                        //創建全新用戶
                        var newUser = new Users
                        {
                            LineId = lineId,
                            LineName = lineName,
                            LinePicUrl = linePicUrl,
                            CreatedAt = DateTime.Now,
                            Roles = (Role)userRole,
                            Number = GetUserNumber(userRole),
                            IsOnline = userRole == 2, // 接單員預設為在線
                            MessageuserId = userRole == 1 ? msgId : null // 只有客人才綁定 msgId
                        };

                        db.Users.Add(newUser);
                        await db.SaveChangesAsync();
                        usersId = newUser.UsersID;
                        number = newUser.Number;
                        return new UserProcessResult
                        {
                            usersId = usersId,
                            MsgId = newUser.MessageuserId,
                            Number = number
                        };
                    }
                }
                catch (Exception ex)
                {
                    // 處理例外
                    throw ex;
                }
            }
        }
        public class UserProcessResult//封裝
        {
            public int usersId { get; set; }
            public string Number { get; set; }
            public string MsgId { get; set; }
        }
        public class LineLoginRequest
        {
            public string code { get; set; }
            public string role { get; set; }
            public string usersId { get; set; }
            public string msgId { get; set; } // 訊息 ID (選填)
            public string state { get; set; } // 添加 state 屬性
        }

        private string GetUserNumber(int role)
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
