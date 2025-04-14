using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dog.Models
{
    public class EnumList
    {
    }
    public enum Role
    {
        使用者 = 1,
        接單員 = 2,
    }
    public enum CommonIssues
    {
        垃圾量超過方案限制 = 1,
        未找到垃圾袋用戶無回應 = 2,
        無QR碼用戶無回應 = 3,
        垃圾袋破損嚴重 = 4,
        面交未見用戶已聯絡無回應 = 5
    }
    public enum OrderStatus
    {
        未排定 = 0,//UnScheduled
        已排定 = 1,//Scheduled
        前往中 = 2,//Ongoing
        已抵達 = 3,//Arrived
        已完成 = 4,//Completed
        異常 = 5,//Abnormal
        已取消 = 6
    }
    public enum PaymentStatus
    {
        未付款 = 0,
        已付款 = 1,
        已退款 = 2,
        付款失敗=3
    }
    public enum IdentityEnum
    {
        操作員 = 1,
        主管 = 2,
        老闆 = 3
    }
}