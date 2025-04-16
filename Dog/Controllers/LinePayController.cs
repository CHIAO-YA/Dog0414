using System;
using System.Collections.Generic;
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

        // 交易請求的API
        [HttpPost]
        [Route("api/linePay/reserve")]
        public async Task<IHttpActionResult> OrderReserve(LinePayRequestDto request)
        {
            try
            {
                var result = await _linePayService.ReservePaymentAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(ex);
            }
        }

        // 確認交易的API
        [HttpPost]
        [Route("api/linePay/confirm")]
        public async Task<IHttpActionResult> OrderConfirm(long transactionId)
        {
            try
            {
                var confirmRequest = new ConfirmRequestDto
                {
                    transactionId = transactionId,
                    amount = 500
                };
                var result = await _linePayService.GetPaymentStatusAsync(confirmRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
