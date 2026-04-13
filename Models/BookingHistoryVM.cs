using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thue_san_the_thao.Models
{
    public class BookingHistoryVM
    {
        public int BookingID { get; set; }
        public string FieldName { get; set; }
        public string FacilityName { get; set; }
        public string FieldType { get; set; }
        public string BookingDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Status { get; set; }
        public string CreatedAt { get; set; }
    }
}