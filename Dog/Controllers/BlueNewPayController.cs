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

namespace Dog.Controllers
{
    public class BlueNewPayController : ApiController
    {
        private readonly BlueNewPayService _blueNewPayService = new BlueNewPayService();
        Models.Model1 db = new Models.Model1();
        // 在 BlueNewPayController.cs 中添加
        [HttpPost]
        [Route("Post/bluenew/return")]
        public IHttpActionResult PaymentReturnPost()
        {
            // 可以自由更換要跳轉的網址
            string redirectUrl = "https://lebuleduo.rocket-coding.com/GET/user/plans";
            // 或是用這個：
            // string redirectUrl = "https://lebuleduo.vercel.app/#/auth/line/callback";

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
                    order.PaymentStatus = PaymentStatus.已付款;
                    order.UpdatedAt = DateTime.Now;
                    db.SaveChanges();
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