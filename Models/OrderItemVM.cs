using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thue_san_the_thao.Models
{
    public class OrderItemVM
    {
        public string ProductName { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}