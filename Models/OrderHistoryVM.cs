using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thue_san_the_thao.Models
{
    public class OrderHistoryVM
    {
        public int OrderID { get; set; }
        public string OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string PayMethod { get; set; }
        public string PayStatus { get; set; }
        public List<OrderItemVM> Items { get; set; }
    }
}