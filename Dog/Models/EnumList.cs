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
    public enum OrderStatus
    {
        未完成 = 1,
        前往中 = 2,
        已完成 = 3,
        異常回報 = 4,
        已取消 = 5
    }
    public enum PaymentStatus
    {
        未付款 = 0,
        已付款 = 1,
        已退款 = 2
    }
    public enum IdentityEnum
    {
        操作員 = 1,
        主管 = 2,
        老闆 = 3
    }
}