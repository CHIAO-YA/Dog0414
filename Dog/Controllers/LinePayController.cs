using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Dog.Models;
using Dog.Security;
//using System.Web.Mvc;
using Newtonsoft.Json;

namespace Dog.Controllers
{
    public class LinePayController : ApiController
    {
        private readonly LinePayService _linePayService = new LinePayService();

        // 交易請求給LinePay的API
        [HttpPost]
        [Route("Post/linePay/Reserve")]
        public async Task<IHttpActionResult> OrderReserve(LinePayRequestDto request)//預約交易時要給 LinePay 的資料（商品名稱、價格等等）
        {
            try
            {
                // 1. 驗證訂單資訊
                if (string.IsNullOrEmpty(request.orderId)) return BadRequest("訂單ID不能為空");
                int ordersId;
                if (!int.TryParse(request.orderId, out ordersId)) return BadRequest("訂單ID格式不正確");
                // 2. 查詢資料庫確認訂單存在並驗證狀態
                using (var db = new Models.Model1())
                {
                    var order = db.Orders.FirstOrDefault(o => o.OrdersID.ToString() == request.orderId);
                    if (order == null) { return NotFound(); }

                    //3.檢查訂單狀態 - 避免重複支付
                    if (order.PaymentStatus == PaymentStatus.已付款) return BadRequest("此訂單已完成支付");
                    //4.使用專門方法計算訂單總金額以確保正確性
                    decimal totalAmount = order.TotalAmount;
                    if (totalAmount != order.TotalAmount) { return BadRequest("訂單金額有誤，請重新下單"); }//資料庫
                    if (request.amount != (int)order.TotalAmount){return BadRequest("訂單金額不符，請重新確認");}//前端
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
                                    name = order.Plan?.PlanName ?? "方案",
                                    quantity = 1,
                                    price = (int)totalAmount
                                }
                            }
                        }
                    };
                    // 7. 設定付款結果回調URL
                    request.redirectUrls = new RedirectUrlsDto
                    {
                        confirmUrl = "https://www.youtube.com", // 成功
                        cancelUrl = "https://www.google.com"    // 失敗/取消
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
                        // 返回成功結果，前端可據此導向到 Line Pay 支付頁面
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
                        // 預約交易失敗
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

        // 確認交易的API
        //改用物件參數來接收 POST body，或是 明確標註從 Query 抓參數且改用 GET 方法。
        [HttpPost]
        [Route("Post/linePay/Confirm")]
        public async Task<IHttpActionResult> OrderConfirm([FromBody] ConfirmTransactionDto request)//(long transactionId)
        {
            try
            {
                // 用來查詢對應的訂單，例如查 DB 是否有這筆 ordersID 和 transactionId
                string ordersID = request.ordersID;// 來自前端的訂單編號
                long transactionId = request.transactionId;// LinePay 回傳的交易 ID
                                                          
                using (var db = new Models.Model1())
                {
                    // 先從資料庫獲取訂單
                    var order = db.Orders.FirstOrDefault(o => o.OrdersID.ToString() == ordersID);
                    if (order == null) return NotFound();

                    // 建立要送給 LinePay 的確認請求物件
                    var confirmRequest = new ConfirmRequestDto
                    {
                    transactionId = request.transactionId,
                    amount = (int)order.TotalAmount // 使用資料庫訂單金額
                    };

                var result = await _linePayService.GetPaymentStatusAsync(confirmRequest);

                // TODO: 在這裡可以根據 ordersID + transactionId 對應你自己的訂單資料庫狀態更新
                if (result.returnCode == "0000")// 假設 LinePay 會回傳是否成功的標記
                {
                    order.PaymentStatus = PaymentStatus.已付款; // 假設有此枚舉值
                      // 儲存更新到資料庫
                }
                else
                {
                    order.PaymentStatus = PaymentStatus.付款失敗; // 使用枚舉而非字串
                   
                }
                    db.SaveChanges();
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
    // 前端確認交易時會 POST 回來的資料（通常是交易成功後帶回）
    public class ConfirmTransactionDto
    {
        public string ordersID { get; set; }          // 你自家的訂單編號（用來找誰的訂單）
        public long transactionId { get; set; }       // LinePay 回傳的交易 ID
        public int amount { get; set; }
    }

}
