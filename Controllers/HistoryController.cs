using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using Thue_san_the_thao.Models.Data;
using Thue_san_the_thao.Models;


namespace Thue_san_the_thao.Controllers
{
    [Authorize]
    public class HistoryController : Controller
    {
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        private int? GetUserId()
        {
            var id = Session["UserID"];
            if (id == null) return null;
            return (int)id;
        }

        // GET: /History
        public ActionResult Index(string tab = "booking")
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            // Lấy bảng giá một lần để tra
            var prices = db.Prices.ToList();

            // ===== Lịch sử đặt sân =====
            var bookings = db.Bookings
                .Include("Field.Facility")
                .Include("TimeSlot")
                .Where(b => b.UserID == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToList()
                .Select(b =>
                {
                    decimal price = 0;
                    if (b.FieldID.HasValue && b.TimeSlotID.HasValue)
                    {
                        var p = prices.FirstOrDefault(x =>
                            x.FieldID == b.FieldID && x.TimeSlotID == b.TimeSlotID);
                        price = p?.Price1 ?? 0;
                    }
                    return new BookingHistoryVM
                    {
                        BookingID = b.BookingID,
                        FieldName = b.Field != null ? b.Field.FieldName : "—",
                        FacilityName = b.Field?.Facility != null ? b.Field.Facility.FacilityName : "—",
                        FieldType = b.Field != null ? b.Field.Type : "—",
                        BookingDate = b.BookingDate.HasValue ? b.BookingDate.Value.ToString("dd/MM/yyyy") : "—",
                        StartTime = b.TimeSlot?.StartTime.HasValue == true ? b.TimeSlot.StartTime.Value.ToString(@"hh\:mm") : "—",
                        EndTime = b.TimeSlot?.EndTime.HasValue == true ? b.TimeSlot.EndTime.Value.ToString(@"hh\:mm") : "—",
                        Status = b.Status ?? "Pending",
                        CreatedAt = b.CreatedAt.HasValue ? b.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") : "—",
                        TotalAmount = price
                    };
                }).ToList();

            // ===== Lịch sử đơn hàng sản phẩm (chỉ lấy đơn KHÔNG có BookingID) =====
            var orders = db.Orders
                .Include("OrderDetails.Product")
                .Include("Payments")
                .Where(o => o.UserID == userId && o.BookingID == null)
                .OrderByDescending(o => o.OrderDate)
                .ToList()
                .Select(o => new OrderHistoryVM
                {
                    OrderID = o.OrderID,
                    OrderDate = o.OrderDate.HasValue ? o.OrderDate.Value.ToString("dd/MM/yyyy HH:mm") : "—",
                    TotalAmount = o.TotalAmount ?? 0,
                    Status = o.Status ?? "Chờ xác nhận",
                    PayMethod = o.Payments.FirstOrDefault()?.Method ?? "—",
                    PayStatus = o.Payments.FirstOrDefault()?.Status ?? "—",
                    Items = o.OrderDetails.Select(d => new OrderItemVM
                    {
                        ProductName = d.Product != null ? d.Product.Name : "Sản phẩm",
                        Image = d.Product != null ? d.Product.Image : null,
                        Quantity = d.Quantity ?? 0,
                        Price = d.Price ?? 0
                    }).ToList()
                }).ToList();

            ViewBag.Tab = tab;
            ViewBag.Bookings = bookings;
            ViewBag.Orders = orders;

            return View();
        }

        // POST: Huỷ đặt sân
        [HttpPost]
        public ActionResult CancelBooking(int id)
        {
            var userId = GetUserId();
            var booking = db.Bookings.FirstOrDefault(b => b.BookingID == id && b.UserID == userId);

            if (booking != null && booking.Status == "pending")
            {
                booking.Status = "cancelled";
                db.SaveChanges();
                TempData["Success"] = "Đã huỷ đặt sân thành công.";
            }

            return RedirectToAction("Index", new { tab = "booking" });
        }
    }
}