﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Dog.Models
{
    public class LinePayRequestDto
    {
        public int amount { get; set; }
        public string currency { get; } = "TWD";
        public string orderId { get; set; }
        public List<PackageDto> packages { get; set; }
        public RedirectUrlsDto redirectUrls { get; set; }
        public string OrderNumber { get; set; } // 自家訂單編號，用來知道是哪一筆訂單
    }

    public class PackageDto
    {
        public string id { get; set; }
        public int amount { get; set; }
        public List<ProductDto> products { get; set; }
    }

    public class RedirectUrlsDto
    {
        public string confirmUrl { get; set; }
        public string cancelUrl { get; set; }
    }

    public class ProductDto
    {
        public string id { get; set; }         // ✅ Line Pay 規定必填
        public string name { get; set; }
        public string imageUrl { get; set; }   // ✅ Line Pay 規定必填
        public int quantity { get; set; }
        public int price { get; set; }
    }

    // 接收LinePay的回應，建議獲取transactionId才方便完成Confirm API的操作
    public class LinePayResponseDto
    {
        public string returnCode { get; set; }
        public string returnMessage { get; set; }
        public LinePayResponseInfoDto info { get; set; }
    }

    public class LinePayResponseInfoDto
    {
        public long transactionId { get; set; }
        public PaymentUrlDto paymentUrl { get; set; }
    }

    public class PaymentUrlDto
    {
        public string web { get; set; }
        public string app { get; set; }
    }

    // 送出請求格式以及必要屬性
    public class ConfirmRequestDto
    {
        public long transactionId { get; set; }
        public int amount { get; set; }
        public string currency { get; } = "TWD";
    }

    // 接收LinePay的回應
    public class PaymentConfirmResponseDto
    {
        public string returnCode { get; set; }
        public string returnMessage { get; set; }
    }

}

