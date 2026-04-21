using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        private int? GetUserId()
        {
            var id = Session["UserId"];
            if (id == null) return null;
            return (int)id;
        }

        // ================== TRANG CHON SAN ==================
        public ActionResult DatSan()
        {
            var uid = GetUserId();
            if (uid == null) return RedirectToAction("Login", "Account");

            var allFields = db.Fields
                              .Include("Facility")
                              .Where(f => f.Status == true)
                              .ToList();

            var allPrices = db.Prices.ToList();

            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            bool firstGroup = true;
            foreach (var g in allFields.GroupBy(f => MapType(f.Type)))
            {
                if (!firstGroup) sb.Append(",");
                firstGroup = false;
                sb.Append("\"" + g.Key + "\":[");
                bool firstField = true;
                foreach (var f in g)
                {
                    if (!firstField) sb.Append(",");
                    firstField = false;

                    string safeName = (f.FieldName ?? "")
                        .Replace("\\", "\\\\").Replace("\"", "\\\"");
                    string img = !string.IsNullOrEmpty(f.Image)
                        ? f.Image : GetDefaultImage(f.Type);

                    var fieldPrices = allPrices.Where(p => p.FieldID == f.FieldID).ToList();
                    decimal minPrice = fieldPrices.Any() ? fieldPrices.Min(p => p.Price1 ?? 0) : 0m;
                    string priceStr = minPrice.ToString("N0",
                        System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));

                    sb.Append("{");
                    sb.Append("\"id\":" + f.FieldID + ",");
                    sb.Append("\"name\":\"" + safeName + "\",");
                    sb.Append("\"img\":\"" + img + "\",");
                    sb.Append("\"price\":\"" + priceStr + "\"");
                    sb.Append("}");
                }
                sb.Append("]");
            }
            sb.Append("}");

            ViewBag.FieldsJson = sb.ToString();
            return View();
        }

        // ================== TRANG CHON GIO ==================
        public ActionResult Index(int? fieldId)
        {
            var uid = GetUserId();
            if (uid == null) return RedirectToAction("Login", "Account");

            if (fieldId == null || fieldId <= 0)
                return RedirectToAction("Trangchu", "Home");

            var field = db.Fields
                          .Include("Facility")
                          .FirstOrDefault(f => f.FieldID == fieldId);

            if (field == null)
                return RedirectToAction("Trangchu", "Home");

            string fieldAddress = field.Facility != null && !string.IsNullOrEmpty(field.Facility.Address)
                ? field.Facility.Address : "Đang cập nhật";

            string fieldImage = !string.IsNullOrEmpty(field.Image)
                ? field.Image : GetDefaultImage(field.Type);

            // Lay gia theo dung san nay
            var prices = db.Prices
                           .Where(p => p.FieldID == fieldId && p.TimeSlotID != null)
                           .ToList();

            decimal defaultPrice = prices.Any() ? prices.Min(p => p.Price1 ?? 0) : 0m;

            var sbPrice = new System.Text.StringBuilder("{");
            bool firstP = true;
            foreach (var p in prices)
            {
                if (!firstP) sbPrice.Append(",");
                firstP = false;
                sbPrice.Append("\"" + p.TimeSlotID + "\":" + (int)(p.Price1 ?? 0));
            }
            sbPrice.Append("}");

            ViewBag.FieldId = field.FieldID;
            ViewBag.FieldName = field.FieldName;
            ViewBag.FieldImage = fieldImage;
            ViewBag.FieldAddress = fieldAddress;
            ViewBag.FieldPrice = (int)defaultPrice;
            ViewBag.PriceBySlotJson = sbPrice.ToString();
            ViewBag.TimeSlots = db.TimeSlots.OrderBy(t => t.StartTime).ToList();

            return View();
        }

        // ================== LAY SLOT DA DAT (chi lay slot da thanh toan) ==================
        public JsonResult GetBooked(int fieldId, DateTime bookingDate)
        {
            var date = bookingDate.Date;
            // Chi block slot co status KHAC "Chờ thanh toán" (tuc la da thanh toan hoac admin xac nhan)
            var booked = db.Bookings
                .Where(x =>
                    x.FieldID == fieldId &&
                    x.Status != "Chờ thanh toán" &&
                    x.BookingDate.HasValue &&
                    x.BookingDate.Value.Year == date.Year &&
                    x.BookingDate.Value.Month == date.Month &&
                    x.BookingDate.Value.Day == date.Day)
                .Select(x => x.TimeSlotID)
                .ToList();

            return Json(booked, JsonRequestBehavior.AllowGet);
        }

        // ================== KIEM TRA SLOT TRUOC KHI HIEN QR ==================
        // Khong luu DB, chi kiem tra con trong va tinh tien → redirect sang QR
        [HttpPost]
        public JsonResult CheckAndPrepare(int fieldId, string timeSlotIds, string bookingDate)
        {
            var uid = GetUserId();
            if (uid == null)
                return Json(new { success = false, message = "Bạn cần đăng nhập!" });

            try
            {
                // Parse danh sach slotId tu chuoi "1,2,3"
                var ids = timeSlotIds.Split(',')
                                     .Select(s => int.Parse(s.Trim()))
                                     .ToList();

                DateTime date = DateTime.Parse(bookingDate).Date;

                // Kiem tra xem co slot nao da bi dat chua (chi tinh slot da thanh toan)
                foreach (var slotId in ids)
                {
                    bool exist = db.Bookings.Any(x =>
                        x.FieldID == fieldId &&
                        x.TimeSlotID == slotId &&
                        x.Status != "Chờ thanh toán" &&
                        x.BookingDate.HasValue &&
                        x.BookingDate.Value.Year == date.Year &&
                        x.BookingDate.Value.Month == date.Month &&
                        x.BookingDate.Value.Day == date.Day);

                    if (exist)
                    {
                        var slotInfo = db.TimeSlots.Find(slotId);
                        string slotTime = slotInfo != null && slotInfo.StartTime.HasValue
                            ? slotInfo.StartTime.Value.Hours.ToString("D2") + ":" +
                              slotInfo.StartTime.Value.Minutes.ToString("D2")
                            : "khung giờ này";
                        return Json(new { success = false, message = $"Khung giờ {slotTime} đã được đặt!" });
                    }
                }

                // Tinh tong tien tat ca slot
                decimal total = 0;
                foreach (var slotId in ids)
                {
                    var pr = db.Prices.FirstOrDefault(p => p.FieldID == fieldId && p.TimeSlotID == slotId);
                    total += pr != null ? (pr.Price1 ?? 0) : 0;
                }

                // Luu thong tin vao Session (chua luu DB)
                Session["PendingBooking"] = new PendingBookingData
                {
                    FieldId = fieldId,
                    TimeSlotIds = ids,
                    BookingDate = date,
                    TotalAmount = total
                };

                string redirectUrl = Url.Action("BookingPaymentQR", "Booking");
                return Json(new { success = true, redirectUrl = redirectUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ================== TRANG QR DAT SAN ==================
        public ActionResult BookingPaymentQR()
        {
            var uid = GetUserId();
            if (uid == null) return RedirectToAction("Login", "Account");

            var pending = Session["PendingBooking"] as PendingBookingData;
            if (pending == null)
                return RedirectToAction("DatSan");

            var field = db.Fields.Include("Facility").FirstOrDefault(f => f.FieldID == pending.FieldId);

            // Lay thong tin cac slot
            var slots = db.TimeSlots
                          .Where(t => pending.TimeSlotIds.Contains(t.TimeSlotID))
                          .OrderBy(t => t.StartTime)
                          .ToList();

            string startTime = slots.FirstOrDefault()?.StartTime.HasValue == true
                ? slots.First().StartTime.Value.Hours.ToString("D2") + ":" +
                  slots.First().StartTime.Value.Minutes.ToString("D2") : "";
            string endTime = slots.LastOrDefault()?.EndTime.HasValue == true
                ? slots.Last().EndTime.Value.Hours.ToString("D2") + ":" +
                  slots.Last().EndTime.Value.Minutes.ToString("D2") : "";

            ViewBag.FieldName = field?.FieldName ?? "Sân thể thao";
            ViewBag.BookingDate = pending.BookingDate.ToString("dd/MM/yyyy");
            ViewBag.StartTime = startTime;
            ViewBag.EndTime = endTime;
            ViewBag.SlotCount = pending.TimeSlotIds.Count;
            ViewBag.Amount = (long)pending.TotalAmount;

            return View();
        }

        // ================== XAC NHAN DA CHUYEN KHOAN → MOI LUU BOOKING ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmBookingPayment()
        {
            var uid = GetUserId();
            if (uid == null) return RedirectToAction("Login", "Account");

            var pending = Session["PendingBooking"] as PendingBookingData;
            if (pending == null)
            {
                TempData["Error"] = "Phiên đặt sân đã hết hạn, vui lòng đặt lại.";
                return RedirectToAction("DatSan");
            }

            // Kiem tra lan cuoi truoc khi luu
            foreach (var slotId in pending.TimeSlotIds)
            {
                bool exist = db.Bookings.Any(x =>
                    x.FieldID == pending.FieldId &&
                    x.TimeSlotID == slotId &&
                    x.Status != "Chờ thanh toán" &&
                    x.BookingDate.HasValue &&
                    x.BookingDate.Value.Year == pending.BookingDate.Year &&
                    x.BookingDate.Value.Month == pending.BookingDate.Month &&
                    x.BookingDate.Value.Day == pending.BookingDate.Day);

                if (exist)
                {
                    TempData["Error"] = "Có khung giờ vừa bị đặt bởi người khác, vui lòng chọn lại.";
                    Session.Remove("PendingBooking");
                    return RedirectToAction("Index", new { fieldId = pending.FieldId });
                }
            }

            // Luu tung Booking cho tung slot
            foreach (var slotId in pending.TimeSlotIds)
            {
                db.Bookings.Add(new Booking
                {
                    FieldID = pending.FieldId,
                    TimeSlotID = slotId,
                    BookingDate = pending.BookingDate,
                    Status = "Chờ xác nhận",
                    CreatedAt = DateTime.Now,
                    UserID = uid.Value
                });
            }
            db.SaveChanges();

            Session.Remove("PendingBooking");

            TempData["PaySuccess"] = "Đặt sân thành công! Chúng tôi sẽ xác nhận sau khi kiểm tra thanh toán.";
            return RedirectToAction("Index", "History", new { tab = "booking" });
        }

        // ================== HELPERS ==================
        private string MapType(string type)
        {
            switch ((type ?? "").ToLower().Trim())
            {
                case "football": case "bóng đá": case "bong da": case "bongda": return "football";
                case "badminton": case "cầu lông": case "cau long": case "caulong": return "badminton";
                case "tennis": return "tennis";
                case "pickleball": return "pickleball";
                default: return "other";
            }
        }

        private string GetDefaultImage(string type)
        {
            switch ((type ?? "").ToLower().Trim())
            {
                case "football": case "bóng đá": case "bong da": return "bernabeu.jpg";
                case "badminton": case "cầu lông": case "cau long": return "caulong.jpg";
                case "tennis": return "tennis.jpg";
                case "pickleball": return "pickleball.jpg";
                default: return "default.jpg";
            }
        }
    }

    // Class luu trang thai dat san chua thanh toan (trong Session)
    public class PendingBookingData
    {
        public int FieldId { get; set; }
        public List<int> TimeSlotIds { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; }
    }
}