using Dog.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Dog.Security
{
    public class BlueNewPayService
    {
        private readonly string _merchantID;
        private readonly string _hashKey;
        private readonly string _hashIV;
        private readonly string _paymentApiUrl;
        private readonly string _returnUrl;
        private readonly string _notifyUrl;

        public BlueNewPayService()
        {
            // 測試環境的資料，實際運行時需要替換成正式環境
            _merchantID = "MS155729652"; // 替換成你的商店代號
            _hashKey = "n1xwTtaiYiyazec7DnPCo7crP2AAF7Sc"; // 替換成你的 HashKey
            _hashIV = "CS6Jn7qiSPRKtj1P"; // 替換成你的 HashIV
            _paymentApiUrl = "https://ccore.newebpay.com/MPG/mpg_gateway"; // 測試環境URL
            _returnUrl = "https://lebuleduo.rocket-coding.com/Post/bluenew/return"; // 支付完成後的回調URL
            _notifyUrl = "https://lebuleduo.rocket-coding.com/Post/bluenew/notify"; // 支付通知的URL
        }

        // 產生交易資料
        public BlueNewPaymentData CreatePaymentData(Orders order)
        {
            // 產生訂單編號 (若已有訂單編號可直接使用)
            string merchantOrderNo = "ORD-" + DateTime.Now.ToString("yyyyMMdd-");


            // 產生交易時間 (藍新金流規定格式)
            string tradeDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            // 設定項目名稱
            string itemDesc = order.Plan?.PlanName ?? "收垃圾服務";

            // 建立交易資料物件
            var paymentData = new BlueNewPaymentData
            {
                MerchantID = _merchantID,
                MerchantOrderNo = merchantOrderNo,
                ItemDesc = itemDesc,
                Amt = (int)order.TotalAmount,
                TradeLimit = 600, // 交易有效時間，單位為分鐘
                ExpireDate = DateTime.Now.AddDays(7).ToString("yyyyMMdd"), // 商品有效期限
                ReturnURL = _returnUrl,
                NotifyURL = _notifyUrl,
                CustomerURL = _returnUrl,
                ClientBackURL = _returnUrl,
                Email = "customer@example.com", // 使用預設電子郵件
                OrderComment = $"訂單編號: {order.OrdersID}",
                CREDIT = 1, // 啟用信用卡付款
                WEBATM = 0, // 關閉網路ATM
                VACC = 0, // 關閉ATM轉帳
                CVS = 0, // 關閉超商代碼
                BARCODE = 0 // 關閉超商條碼
            };

            return paymentData;
        }

        // 產生 AES 加密文字
        public string GetAESEncrypt(string plainText)
        {
            string key = _hashKey;
            string iv = _hashIV;

            var encryptValue = EncryptAES256(plainText, key, iv);
            return encryptValue;
        }

        // AES 加密方法
        private string EncryptAES256(string plainText, string key, string iv)
        {
            byte[] keyBytes = Encoding.ASCII.GetBytes(key);
            byte[] ivBytes = Encoding.ASCII.GetBytes(iv);
            byte[] dataBytes = Encoding.UTF8.GetBytes(plainText);

            using (var aes = new RijndaelManaged())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] encryptedData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                    return Convert.ToBase64String(encryptedData);
                }
            }
        }

        // 產生檢查碼
        public string GetSHA256CheckSum(string tradeInfo)
        {
            string key = _hashKey;
            string iv = _hashIV;
            string plainText = $"HashKey={key}&{tradeInfo}&HashIV={iv}";

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainText));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString().ToUpper();
            }
        }
    }

    // 藍新金流交易資料模型
    public class BlueNewPaymentData
    {
        public string MerchantID { get; set; }
        public string MerchantOrderNo { get; set; }
        public string ItemDesc { get; set; }
        public int Amt { get; set; }
        public int TradeLimit { get; set; }
        public string ExpireDate { get; set; }
        public string ReturnURL { get; set; }
        public string NotifyURL { get; set; }
        public string CustomerURL { get; set; }
        public string ClientBackURL { get; set; }
        public string Email { get; set; }
        public string OrderComment { get; set; }
        public int CREDIT { get; set; }
        public int WEBATM { get; set; }
        public int VACC { get; set; }
        public int CVS { get; set; }
        public int BARCODE { get; set; }
    }
}