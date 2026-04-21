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
        public int SlotCount { get; set; }   // so khung gio trong nhom
        public decimal TotalAmount { get; set; }   // tong tien ca nhom
        public string Status { get; set; }
        public string CreatedAt { get; set; }
    }
}