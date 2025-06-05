using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Dog.Models;
using Dog.Security;
using Newtonsoft.Json;
using isRock.LineBot;
using System.Configuration;
using System.Diagnostics;

namespace Dog.Controllers
{
    public class LinePayController : ApiController
    {
        private string channelAccessToken = ConfigurationManager.AppSettings["LineChannelAccessToken"];
        private string channelSecret = ConfigurationManager.AppSettings["LineChannelSecret"];
        private readonly LinePayService _linePayService = new LinePayService();
        Models.Model1 db = new Models.Model1();

        // 交易請求給LinePay的API
        [HttpPost]
        [Route("Post/linePay/Reserve")]
        public async Task<IHttpActionResult> OrderReserve(LinePayRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.orderId)) return BadRequest("訂單ID不能為空");
                int ordersId;
                if (!int.TryParse(request.orderId, out ordersId)) return BadRequest("訂單ID格式不正確");
                using (var db = new Models.Model1())
                {
                    var order = db.Orders.FirstOrDefault(o => o.OrdersID.ToString() == request.orderId);
                    if (order == null) { return NotFound(); }

                    if (order.PaymentStatus == PaymentStatus.已付款) return BadRequest("此訂單已完成支付");
                    decimal totalAmount = order.TotalAmount;
                    if (totalAmount != order.TotalAmount) { return BadRequest("訂單金額有誤，請重新下單"); }//資料庫
                    if (request.amount != (int)order.TotalAmount) { return BadRequest("訂單金額不符，請重新確認"); }//前端
                    //5.設定 Line Pay 請求所需資訊
                    request.amount = (int)totalAmount;
                    //6.設定商品資訊（將顯示在 Line Pay 付款頁面上）
                    request.packages = new List<PackageDto>
                {
                    new PackageDto
                    {
                        id = "pkg-" + ordersId,
                        amount = (int)totalAmount,
                        products = new List<ProductDto>
                        {
                            new ProductDto
                            {
                               id = "prod-" + ordersId,               // 補上產品 ID，例如 "prod-29441093"
                               name = order.Plan?.PlanName ?? "方案",
                               imageUrl = "https://github.com/CHIAO-YA/DogPhotourl/blob/main/godphoto/line.PNG?raw=true", // 補上一個圖片 URL
                               quantity = 1,
                               price = (int)totalAmount
                            }
                        }
                    }
                };
                    request.redirectUrls = new RedirectUrlsDto
                    {
                        confirmUrl = "https://lebuleduo.vercel.app/#/customer/subscribe-confirm",
                        cancelUrl = "https://lebuleduo.vercel.app/#/customer/subscribe-fail"
                    };
                    // 8. 呼叫 Line Pay 服務預約交易
                    var result = await _linePayService.ReservePaymentAsync(request);
                    // 9. 處理預約交易結果
                    if (result != null && result.returnCode == "0000") // 假設 0000 代表成功
                    {
                        // 更新訂單狀態為「等待付款」
                        order.LinePayTransactionId = result.info?.transactionId;
                        order.LinePayStatus = "reserved";
                        order.PaymentStatus = PaymentStatus.未付款; // 假設有此枚舉值
                        order.UpdatedAt = DateTime.Now;
                        db.SaveChanges();
                        return Ok(new
                        {
                            success = true,
                            message = "交易預約成功",
                            paymentUrl = result.info?.paymentUrl?.web,
                            transactionId = result.info?.transactionId,
                          
                        });
                    }
                    else
                    {
                        return Ok(new
                        {
                            success = false,
                            message = "交易預約失敗",
                            errorCode = result?.returnCode,
                            errorMessage = result?.returnMessage
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(ex);
            }
        }



       //// 確認交易的API
       ////改用物件參數來接收 POST body，或是 明確標註從 Query 抓參數且改用 GET 方法。
       // [HttpPost]
       // [Route("Post/linePay/Confirm")]
       // public async Task<IHttpActionResult> OrderConfirm([FromBody] ConfirmTransactionDto request)
       // {
       //     try
       //     {
       //         //用來查詢對應的訂單，例如查 DB 是否有這筆 ordersID 和 transactionId
       //         string orderId = request.orderId;// 來自前端的訂單編號
       //         string transactionId = request.transactionId;// LinePay 回傳的交易 ID

       //         using (var db = new Models.Model1())
       //         {
       //             var order = db.Orders.FirstOrDefault(o => o.OrdersID.ToString() == orderId);
       //             if (order == null) return NotFound();

       //             //移到這裡：記錄原始狀態
       //             var originalStatus = order.PaymentStatus.ToString();

       //             //建立要送給 LinePay 的確認請求物件
       //             var confirmRequest = new ConfirmRequestDto
       //             {
       //                 transactionId = request.transactionId.ToString(),
       //                 amount = (int)order.TotalAmount, // 使用資料庫訂單金額
       //                 currency = "TWD"
       //             };

       //             var result = await _linePayService.GetPaymentStatusAsync(confirmRequest);

       //             //TODO: 在這裡可以根據 ordersID +transactionId 對應你自己的訂單資料庫狀態更新
       //             if (result.returnCode == "0000")
       //             {
       //                 order.PaymentStatus = PaymentStatus.已付款;
       //                 var user = db.Users.FirstOrDefault(u => u.UsersID == order.UsersID);
       //                 if (user != null && !string.IsNullOrEmpty(user.MessageuserId))
       //                 {
       //                     string msg = $"【Lebu-leduo 通知】訂單已結帳成功！\n" +
       //                                  $"🛍️感謝您的訂購！您的垃圾收運服務已成功結帳並排程。\n\n" +
       //                                  $"【訂單資訊】\n" +
       //                                  $"訂單編號：{order.OrderNumber}\n" +
       //                                  $"支付方式：LINE Pay\n" +
       //                                  $"金額：{order.TotalAmount} 元\n\n" +
       //                                  $"如有任何問題，歡迎隨時聯繫客服😊";

       //                     string cleanMessageuserId = user.MessageuserId.Trim().Replace("\n", "").Replace("\r", "").Replace(" ", "");
       //                     var lineBot = new isRock.LineBot.Bot(channelAccessToken);
       //                     lineBot.PushMessage(cleanMessageuserId, msg);
       //                 }
       //             }
       //             else
       //             {
       //                 order.PaymentStatus = PaymentStatus.付款失敗;
       //             }
       //             db.SaveChanges();
       //             //return Ok(result);
       //             // 🔍 修改成統一格式的回應
       //             return Ok(new
       //             {
       //                 success = result.returnCode == "0000",
       //                 returnCode = result.returnCode,
       //                 returnMessage = result.returnMessage,
       //                 status = result.status,
       //                 message = result.message,

       //                 // 額外資訊
       //                 orderInfo = new
       //                 {
       //                     orderId = orderId,
       //                     transactionId = transactionId,
       //                     amount = order.TotalAmount,
       //                     paymentStatus = order.PaymentStatus.ToString(),
       //                     orderNumber = order.OrderNumber
       //                 }
       //             });
       //         }
       //     }
       //     catch (Exception ex)
       //     {
       //         return InternalServerError(ex);
       //     }
       // }

        [HttpPost]
        [Route("Post/linePay/Confirm")]
        public async Task<IHttpActionResult> OrderConfirm([FromBody] ConfirmTransactionDto request)
        {
            try
            {
                string orderId = request.orderId;
                string transactionId = request.transactionId;

                using (var db = new Models.Model1())
                {
                    var order = db.Orders.FirstOrDefault(o => o.OrdersID.ToString() == orderId);
                    if (order == null)
                    {
                        return Ok(new
                        {
                            statusCode = 404,
                            status = false,
                            message = "訂單不存在"
                        });
                    }

                    var originalStatus = order.PaymentStatus.ToString();

                    var confirmRequest = new ConfirmRequestDto
                    {
                        transactionId = request.transactionId.ToString(),
                        amount = (int)order.TotalAmount,
                        currency = "TWD"
                    };

                    var result = await _linePayService.GetPaymentStatusAsync(confirmRequest);

                    string lineErrorMessage = null;

                    if (result.returnCode == "0000")
                    {
                        order.PaymentStatus = PaymentStatus.已付款;

                        // 嘗試發送 LINE Bot 通知
                        var user = db.Users.FirstOrDefault(u => u.UsersID == order.UsersID);
                        if (user != null && !string.IsNullOrEmpty(user.MessageuserId))
                        {
                            try
                            {
                                string msg = $"【Lebu-leduo 通知】訂單已結帳成功！\n" +
                                             $"🛍️感謝您的訂購！您的垃圾收運服務已成功結帳並排程。\n\n" +
                                             $"【訂單資訊】\n" +
                                             $"訂單編號：{order.OrderNumber}\n" +
                                             $"支付方式：LINE Pay\n" +
                                             $"金額：{order.TotalAmount} 元\n\n" +
                                             $"如有任何問題，歡迎隨時聯繫客服😊";

                                string cleanMessageuserId = user.MessageuserId.Trim().Replace("\n", "").Replace("\r", "").Replace(" ", "");
                                var lineBot = new isRock.LineBot.Bot(channelAccessToken);
                                lineBot.PushMessage(cleanMessageuserId, msg);
                            }
                            catch (Exception lineEx)
                            {
                                lineErrorMessage = "LINE 訊息發送失敗: " + lineEx.Message;
                            }
                        }

                        db.SaveChanges();

                        // 判斷是否有 LINE 訊息錯誤
                        if (lineErrorMessage != null)
                        {
                            return Ok(new
                            {
                                statusCode = 200,
                                status = true,
                                message = "付款確認成功，但LINE訊息發送失敗",
                                lineError = lineErrorMessage,
                                result = new
                                {
                                    orderId = orderId,
                                    transactionId = transactionId,
                                    amount = order.TotalAmount,
                                    paymentStatus = order.PaymentStatus.ToString(),
                                    orderNumber = order.OrderNumber,
                                    returnCode = result.returnCode,
                                    returnMessage = result.returnMessage
                                }
                            });
                        }
                        else
                        {
                            // 完全成功的情況
                            return Ok(new
                            {
                                statusCode = 200,
                                status = true,
                                message = "付款確認成功",
                                result = new
                                {
                                    orderId = orderId,
                                    transactionId = transactionId,
                                    amount = order.TotalAmount,
                                    paymentStatus = order.PaymentStatus.ToString(),
                                    orderNumber = order.OrderNumber,
                                    returnCode = result.returnCode,
                                    returnMessage = result.returnMessage
                                }
                            });
                        }
                    }
                    else
                    {
                        order.PaymentStatus = PaymentStatus.付款失敗;
                        db.SaveChanges();

                        // 付款失敗的情況
                        return Ok(new
                        {
                            statusCode = 400,
                            status = false,
                            message = "付款確認失敗",
                            error = new
                            {
                                returnCode = result.returnCode,
                                returnMessage = result.returnMessage,
                                linePayStatus = result.status
                            },
                            result = new
                            {
                                orderId = orderId,
                                transactionId = transactionId,
                                paymentStatus = order.PaymentStatus.ToString()
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    statusCode = 500,
                    status = false,
                    message = "系統錯誤",
                    error = ex.Message
                });
            }
        }

        //前端確認交易時會 POST 回來的資料（通常是交易成功後帶回）
        public class ConfirmTransactionDto
        {
            public string orderId { get; set; }          // 你自家的訂單編號（用來找誰的訂單）
            public string transactionId { get; set; }       // LinePay 回傳的交易 ID
            public int amount { get; set; }
        }
    }
}
