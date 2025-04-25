using Dog.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Dog.Security
{



    public class LinePayService
    {
        private readonly string _id;
        private readonly string _secretKey;
        private readonly string _baseUrl;

        // HttpClient : 用於發送請求與接收回應
        // 建立靜態的HttpClient實例，建立多個LinePayService成員就可以共享
        private static HttpClient _client;

        // 透過建構函數，每次建立LinePayService物件時，
        // 同時建立靜態HttpClient實例、獲取Web.config內的資訊、 SandBox環境的Url
        public LinePayService()
        {
            _id = "2007183462";
            _secretKey = "62b2e105a73c7685e4abe9f7d3127f42";
            _baseUrl = "https://sandbox-api-pay.line.me";
            _client = new HttpClient();
        }

        // 生成Header內所需的"X-LINE-Authorization"
        private static string GenerateHmacSignature(string channelSecret, string message)
        {
            // 轉換為bytes array
            byte[] keyBytes = Encoding.UTF8.GetBytes(channelSecret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            // 透過HMAC-256算法計算channelSecret與message並回傳
            using (var hmac256 = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        // 建立交易請求
        public async Task<LinePayResponseDto> ReservePaymentAsync(LinePayRequestDto request)
        {
            // Request規定的API Spec
            string requestUrl = "/v3/payments/request";

            // 資料序列化為JSON格式
            string jsonRequestBody = JsonConvert.SerializeObject(request);
            // 透過Guid Class產生隨機值
            string nonce = Guid.NewGuid().ToString();

            // 組合出完整請求路徑
            string fullPath = $"{_baseUrl}{requestUrl}";

            // 組合成簽章所需要的message
            string message = $"{_secretKey}{requestUrl}{jsonRequestBody}{nonce}";
            string signature = GenerateHmacSignature(_secretKey, message);

            // 欲發送請求的路徑以及Http方法
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, fullPath)
            {
                // 請求的主體為UTF-8編碼格式，資料格式為JSON(LinePay規定)
                Content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json")
            };

            // 新增LinePay規定的Header
            httpRequest.Headers.Add("X-LINE-ChannelId", _id);
            httpRequest.Headers.Add("X-LINE-Authorization-Nonce", nonce);
            httpRequest.Headers.Add("X-LINE-Authorization", signature);

            try
            {
                HttpResponseMessage response = await _client.SendAsync(httpRequest);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    LinePayResponseDto result = JsonConvert.DeserializeObject<LinePayResponseDto>(responseContent);
                    result.status = "SUCCESS";
                    result.message = "Payment request successful";
                    return result;
                }
                else
                {
                    // 回傳錯誤時處理
                    LinePayResponseDto errorResult = JsonConvert.DeserializeObject<LinePayResponseDto>(responseContent);
                    errorResult.status = "FAILURE";
                    errorResult.message = "Payment request failed with status code: " + response.StatusCode.ToString();
                    return errorResult;
                }
            }
            catch (Exception ex)
            {
                // 捕捉並處理錯誤
                return new LinePayResponseDto
                {
                    status = "ERROR",
                    message = "An error occurred while processing the payment: " + ex.Message
                };
            }
        }


        // 發送確認交易請求
        public async Task<PaymentConfirmResponseDto> GetPaymentStatusAsync(ConfirmRequestDto confirm)
        {
            string confirmUrl = $"/v3/payments/{confirm.transactionId}/confirm";
            string nonce = Guid.NewGuid().ToString();
            string jsonConfirm = JsonConvert.SerializeObject(confirm);
            string fullPath = $"{_baseUrl}{confirmUrl}";

            // 組合成簽章所需要的message
            string message = $"{_secretKey}{confirmUrl}{jsonConfirm}{nonce}";
            string signature = GenerateHmacSignature(_secretKey, message);

            HttpRequestMessage requestConfirm = new HttpRequestMessage(HttpMethod.Post, fullPath)
            {
                Content = new StringContent(jsonConfirm, Encoding.UTF8, "application/json")
            };

            requestConfirm.Headers.Add("X-LINE-ChannelId", _id);
            requestConfirm.Headers.Add("X-LINE-Authorization-Nonce", nonce);
            requestConfirm.Headers.Add("X-LINE-Authorization", signature);

            HttpResponseMessage responseConfirm = await _client.SendAsync(requestConfirm);
            string responseContent = await responseConfirm.Content.ReadAsStringAsync();

            try
            {

                if (responseConfirm.IsSuccessStatusCode)
                {
                    PaymentConfirmResponseDto result = JsonConvert.DeserializeObject<PaymentConfirmResponseDto>(responseContent);
                    return result;
                }
                else
                {
                    // 回傳錯誤時處理
                    PaymentConfirmResponseDto errorResult = JsonConvert.DeserializeObject<PaymentConfirmResponseDto>(responseContent);
                    errorResult.status = "FAILURE";
                    errorResult.message = "Payment confirmation failed with status code: " + responseConfirm.StatusCode.ToString();
                    return errorResult;
                }
            }
            catch (Exception ex)
            {
                // 捕捉並處理錯誤
                return new PaymentConfirmResponseDto
                {
                    status = "ERROR",
                    message = "An error occurred while confirming the payment: " + ex.Message
                };
            }
        }
    }
}
//public class LinePayService
//{
//    private readonly string _channelId;
//    private readonly string _channelSecret;
//    private readonly string _baseUrl;
//    private readonly HttpClient _client;

//    public LinePayService()
//    {
//        _channelId = "2007183462"; // 替換為你的 ChannelId
//        _channelSecret = "62b2e105a73c7685e4abe9f7d3127f42"; // 替換為你的 ChannelSecret
//        _baseUrl = "https://sandbox-api-pay.line.me"; // Sandbox 環境
//        _client = new HttpClient();
//    }

//    private string GenerateHmacSignature(string message)
//    {
//        byte[] keyBytes = Encoding.UTF8.GetBytes(_channelSecret);
//        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

//        using (var hmac = new HMACSHA256(keyBytes))
//        {
//            byte[] hashBytes = hmac.ComputeHash(messageBytes);
//            return Convert.ToBase64String(hashBytes);
//        }
//    }

//    public async Task<LinePayResponse> ReservePaymentAsync(LinePayRequest request)
//    {
//        try
//        {
//            string requestPath = "/v3/payments/request";
//            string jsonBody = JsonConvert.SerializeObject(request, new JsonSerializerSettings
//            {
//                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
//                Formatting = Formatting.None
//            });
//            string nonce = Guid.NewGuid().ToString();
//            string fullUrl = $"{_baseUrl}{requestPath}";
//            string message = $"{_channelSecret}{requestPath}{jsonBody}{nonce}";
//            string signature = GenerateHmacSignature(message);

//            var httpRequest = new HttpRequestMessage(HttpMethod.Post, fullUrl)
//            {
//                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
//            };

//            httpRequest.Headers.Add("X-LINE-ChannelId", _channelId);
//            httpRequest.Headers.Add("X-LINE-Authorization-Nonce", nonce);
//            httpRequest.Headers.Add("X-LINE-Authorization", signature);

//            HttpResponseMessage response = await _client.SendAsync(httpRequest);
//            string responseContent = await response.Content.ReadAsStringAsync();
//            System.Diagnostics.Debug.WriteLine($"LINE Pay 預約回應: {responseContent}");

//            return JsonConvert.DeserializeObject<LinePayResponse>(responseContent);
//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine($"預約交易失敗: {ex.Message}");
//            throw;
//        }
//    }

//    public async Task<ConfirmResponse> ConfirmPaymentAsync(ConfirmRequest request)
//    {
//        try
//        {
//            string confirmPath = $"/v3/payments/{request.transactionId}/confirm";
//            string jsonBody = JsonConvert.SerializeObject(request, new JsonSerializerSettings
//            {
//                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
//                Formatting = Formatting.None
//            });
//            string nonce = Guid.NewGuid().ToString();
//            string fullUrl = $"{_baseUrl}{confirmPath}";
//            string message = $"{_channelSecret}{confirmPath}{jsonBody}{nonce}";
//            string signature = GenerateHmacSignature(message);

//            var httpRequest = new HttpRequestMessage(HttpMethod.Post, fullUrl)
//            {
//                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
//            };

//            httpRequest.Headers.Add("X-LINE-ChannelId", _channelId);
//            httpRequest.Headers.Add("X-LINE-Authorization-Nonce", nonce);
//            httpRequest.Headers.Add("X-LINE-Authorization", signature);

//            HttpResponseMessage response = await _client.SendAsync(httpRequest);
//            string responseContent = await response.Content.ReadAsStringAsync();
//            System.Diagnostics.Debug.WriteLine($"LINE Pay 確認回應: {responseContent}");

//            return JsonConvert.DeserializeObject<ConfirmResponse>(responseContent);
//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine($"確認交易失敗: {ex.Message}");
//            throw;
//        }
//    }
//}
//}