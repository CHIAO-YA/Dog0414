using Dog.Models;
using Dog.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Data.Entity;
using static Dog.Controllers.LineBotController;
using isRock.LineBot;
using System.Configuration;

namespace Dog.Controllers
{
    public class BlueNewPayController : ApiController
    {
        private string channelAccessToken = ConfigurationManager.AppSettings["LineChannelAccessToken"];
        private string channelSecret = ConfigurationManager.AppSettings["LineChannelSecret"];
        private readonly BlueNewPayService _blueNewPayService = new BlueNewPayService();
        Models.Model1 db = new Models.Model1();

        [HttpPost, HttpGet]
        [Route("Post/bluenew/return")]
        public IHttpActionResult PaymentReturnPost()
        {
            //bool isProductionAvailable = CheckUrlAvailable("https://lebuleduo.vercel.app/#/customer/subscribe-success");
            //string redirectUrl = isProductionAvailable
            //    ? "https://lebuleduo.vercel.app/#/customer/subscribe-success"
            //    : "http://localhost:5173/#/customer/subscribe-success";

            // 可以自由更換要跳轉的網址
            string redirectUrl = "https://lebuleduo.vercel.app/#/customer/subscribe-success";
            ////https://lebuleduo.vercel.app/#/customer/subscribe-success
            ////http://localhost:5173/#/customer/subscribe-success
            var html = $@"<html>
                    <head>
                        <meta charset='utf-8'/>
                        <script>
                            window.location.href = '{redirectUrl}';
                        </script>
                    </head>
                    <body>
                        正在導向中，請稍後...
                    </body>
                </html>";

            var response = new HttpResponseMessage
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            };

            return ResponseMessage(response);
        }


        // 產生付款表單資料
        [HttpPost]
        [Route("Post/bluenew/createPayment")]
        public IHttpActionResult CreatePayment(BlueNewPaymentRequest request)
        {
            try
            {
                // 1. 驗證訂單資訊
                if (string.IsNullOrEmpty(request.orderId)) return BadRequest("訂單ID不能為空");
                int ordersId;
                if (!int.TryParse(request.orderId, out ordersId)) return BadRequest("訂單ID格式不正確");

                // 2. 查詢資料庫確認訂單存在並驗證狀態
                var order = db.Orders.FirstOrDefault(o => o.OrdersID.ToString() == request.orderId);
                if (order == null) return NotFound();

                // 3. 檢查訂單狀態 - 避免重複支付
                if (order.PaymentStatus == PaymentStatus.已付款) return BadRequest("此訂單已完成支付");

                // 4. 建立藍新金流交易資料
                var paymentData = _blueNewPayService.CreatePaymentData(order);

                // 5. 將交易資料轉為 List<KeyValuePair<string, string>>
                List<KeyValuePair<string, string>> tradeData = new List<KeyValuePair<string, string>>() {
                    new KeyValuePair<string, string>("MerchantID", paymentData.MerchantID),
                    new KeyValuePair<string, string>("RespondType", "JSON"),
                    new KeyValuePair<string, string>("TimeStamp", ((int)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds).ToString()),
                    new KeyValuePair<string, string>("Version", "2.0"),
                    new KeyValuePair<string, string>("MerchantOrderNo", paymentData.MerchantOrderNo),
                    new KeyValuePair<string, string>("Amt", paymentData.Amt.ToString()),
                    new KeyValuePair<string, string>("ItemDesc", paymentData.ItemDesc),
                    new KeyValuePair<string, string>("TradeLimit", paymentData.TradeLimit.ToString()),
                    new KeyValuePair<string, string>("NotifyURL", paymentData.NotifyURL),
                    new KeyValuePair<string, string>("ReturnURL", paymentData.ReturnURL),
                    new KeyValuePair<string, string>("ClientBackURL", paymentData.ClientBackURL),
                    new KeyValuePair<string, string>("Email", "customer@example.com"),
                    new KeyValuePair<string, string>("LoginType", "0")
                };

                // 6. 轉換為 key1=Value1&key2=Value2&key3=Value3...
                var tradeQueryPara = string.Join("&", tradeData.Select(x => $"{x.Key}={x.Value}"));

                // 7. AES加密交易資料
                string hashKey = "n1xwTtaiYiyazec7DnPCo7crP2AAF7Sc";
                string hashIV = "CS6Jn7qiSPRKtj1P";
                string encryptedTradeInfo = CryptoUtil.EncryptAESHex(tradeQueryPara, hashKey, hashIV);

                // 8. 產生檢查碼
                string tradeSha = CryptoUtil.EncryptSHA256($"HashKey={hashKey}&{encryptedTradeInfo}&HashIV={hashIV}");

                // 9. 更新訂單資訊
                order.OrderNumber = paymentData.MerchantOrderNo;
                order.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                // 10. 回傳給前端需要的資料
                return Ok(new
                {
                    success = true,
                    message = "付款資料準備完成",
                    paymentData = new
                    {
                        MerchantID = paymentData.MerchantID,
                        TradeInfo = encryptedTradeInfo,
                        TradeSha = tradeSha,
                        Version = "2.0",
                        PaymentUrl = "https://ccore.newebpay.com/MPG/mpg_gateway"
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // 處理藍新金流的付款通知
        [HttpPost]
        [Route("Post/bluenew/notify")]
        public IHttpActionResult PaymentNotify([FromBody] BlueNewPayNotifyRequest request)
        {
            try
            {
                // 解密 TradeInfo
                string hashKey = "n1xwTtaiYiyazec7DnPCo7crP2AAF7Sc";
                string hashIV = "CS6Jn7qiSPRKtj1P";
                string decryptedTradeInfo = CryptoUtil.DecryptAESHex(request.TradeInfo, hashKey, hashIV);

                // 解析 JSON
                var tradeData = JsonConvert.DeserializeObject<BlueNewPayNotifyResult>(decryptedTradeInfo);

                // 根據商戶訂單號查找訂單
                var order = db.Orders.FirstOrDefault(o => o.OrderNumber == tradeData.Result.MerchantOrderNo);
                if (order == null) return NotFound();
                
                // 更新訂單支付狀態
                if (tradeData.Status == "SUCCESS")
                {
                    //// 只針對ATM轉帳特別處理
                    //if (tradeData.Result.PaymentType == "VACC") // ATM 虛擬帳號
                    //{
                    //    order.LinePayMethod = "ATM轉帳";
                    //    System.Diagnostics.Debug.WriteLine("設置支付方式為ATM轉帳");
                    //}

                    order.PaymentStatus = PaymentStatus.已付款;
                order.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                    var user = db.Users.FirstOrDefault(u => u.UsersID == order.UsersID);
                    if (user != null && !string.IsNullOrEmpty(user.MessageuserId))
                    {
                        string msg = $"📦 Lebu-leduo 訂單已結帳成功！\n" +
                                     $"🛍️感謝您的訂購！您的垃圾收運服務已成功結帳並排程。\n\n" +
                                     $"【訂單資訊】\n" +
                                     $"訂單編號：{order.OrderNumber}\n" +
                                     $"支付方式：{order.LinePayMethod}\n" +
                                     $"金額：{order.TotalAmount} 元\n\n" +
                                     $"如有任何問題，歡迎隨時聯繫客服 😊";

                        string cleanMessageuserId = user.MessageuserId.Trim().Replace("\n", "").Replace("\r", "").Replace(" ", "");

                        // 發送 LINE 訊息
                        var lineBot = new isRock.LineBot.Bot(channelAccessToken);
                        lineBot.PushMessage(cleanMessageuserId, msg);
                        System.Diagnostics.Debug.WriteLine($"發送付款成功通知");
                        System.Diagnostics.Debug.WriteLine($"用戶 LineId: {cleanMessageuserId}");
                    }
                }
            else
                {
                    order.PaymentStatus = PaymentStatus.付款失敗;
                    order.UpdatedAt = DateTime.Now;
                    db.SaveChanges();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("Get/Orders/{OrdersID}")]
        public IHttpActionResult GetOrderByID(int OrdersID)
        {
            var order = db.Orders.Include(o => o.Plan).Include(o => o.Discount).FirstOrDefault(o => o.OrdersID == OrdersID);
            if (order == null)
            {
                return Ok(new
                {
                    statusCode = 200,
                    status = true,
                    message = "找不到此訂單",
                });
            }
            var photo = db.Photo.Where(p => p.OrdersID == order.OrdersID).Select(p => p.OrderImageUrl).ToList();

            var result = new
            {
                order.OrderNumber,
                order.TotalAmount,
                order.PaymentStatus,
                order.LinePayMethod,
                order.Discount.Months,
                order.Plan.PlanName,
                order.Plan.Liter,
                order.Plan.PlanKG,
                order.CreatedAt,
                order.UpdatedAt,
            };

            return Ok(new
            {
                statusCode = 200,
                status = true,
                message = "訂單資料取得成功",
                result
            });
        }
    }
    // 請求付款的模型
    public class BlueNewPaymentRequest
    {
        public string orderId { get; set; }
    }

    // 接收付款通知的模型
    public class BlueNewPayNotifyRequest
    {
        public string MerchantID { get; set; }
        public string TradeInfo { get; set; }
        public string TradeSha { get; set; }
        public string Version { get; set; }
    }

    // 解密後的付款通知結果模型
    public class BlueNewPayNotifyResult
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public BlueNewPayResultData Result { get; set; }
    }

    public class BlueNewPayResultData
    {
        public string MerchantID { get; set; }
        public string Amt { get; set; }
        public string TradeNo { get; set; }
        public string MerchantOrderNo { get; set; }
        public string PaymentType { get; set; }
        public string RespondCode { get; set; }
        public string PayTime { get; set; }
        public string IP { get; set; }
        public string EscrowBank { get; set; }
    }
}