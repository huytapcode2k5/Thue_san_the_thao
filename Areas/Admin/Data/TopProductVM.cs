using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thue_san_the_thao.Areas.Admin.Data
{
    public class TopProductVM
    {
        public string ProductName { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}