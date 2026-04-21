using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thue_san_the_thao.Areas.Admin.Data
{
    public class TopFieldVM
    {
        public string FieldName { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
