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
                //驗證資料
                if (string.IsNullOrEmpty(request.OrderNumber) || string.IsNullOrEmpty(request.orderId))
                {
                    return BadRequest("訂單資料不完整");
                }
                //$$
                //decimal totalAmount = 0;
                //foreach (var item in request.Items)
                //{
                //    totalAmount += item.Price * item.Quantity; // 根據商品價格和數量計算總金額
                //}
                request.successUrl = "https://www.youtube.com"; // 成功導向
                request.faillUrl = "https://www.google.com";  // 失敗導向
                var result = await _linePayService.ReservePaymentAsync(request);
                return Ok(result);
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
                string ordersID = request.ordersID;//	自家訂單編號，用來知道是哪一筆訂單
                long transactionId = request.transactionId;//LinePay 付款後回傳的編號，是該筆交易在 LinePay 裡的唯一識別

                // 建立要送給 LinePay 的確認請求物件
                var confirmRequest = new ConfirmRequestDto
                {
                    transactionId = transactionId,
                    amount = 500 // 假設固定金額，你也可以從 DB 撈實際金額
                };

                var result = await _linePayService.GetPaymentStatusAsync(confirmRequest);

                // TODO: 在這裡可以根據 ordersID + transactionId 對應你自己的訂單資料庫狀態更新
                // 例如：更新該筆訂單為「已付款」

                return Ok(result);
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

    }
    //private decimal OrderTotalMoney(Orders orders)
    //{

    //}
}
