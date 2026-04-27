using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Thue_san_the_thao.Models.Data;
using Thue_san_the_thao.Areas.Admin.Data;

namespace Thue_san_the_thao.Areas.Admin.Controllers
{
    [Authorize]
    public class HomeAdminController : Controller
    {
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        public ActionResult Trangchu()
        {
            var today = DateTime.Today;
            var firstThisMonth = new DateTime(today.Year, today.Month, 1);
            var firstLastMonth = firstThisMonth.AddMonths(-1);

            // ══ THỐNG KÊ TỔNG QUAN ══

            ViewBag.TotalUsers = db.Users.Count(u => u.RoleID != 1);

            // FIX: đếm tất cả Facility (không lọc Status vì có thể null/false)
            ViewBag.TotalFacilities = db.Facilities.Count();

            // Chỉ đếm sân đang hoạt động
            ViewBag.TotalFields = db.Fields.Count(f => f.Status == true);

            // Sản phẩm đang bán
            ViewBag.TotalProducts = db.Products.Count(p => p.Status == true);

            // ══ ĐẶT SÂN ══

            // Tổng lượt đặt tháng này (mọi trạng thái - để admin thấy traffic thực)
            ViewBag.BookingThisMonth = db.Bookings
                .Count(b => b.BookingDate >= firstThisMonth);

            // Hôm nay
            ViewBag.BookingToday = db.Bookings
                .Count(b => DbFunctions.TruncateTime(b.BookingDate) == today);

            // Theo trạng thái
            ViewBag.BookingWaitPay = db.Bookings.Count(b => b.Status == "Chờ thanh toán");
            ViewBag.BookingWaitConf = db.Bookings.Count(b => b.Status == "Pending" || b.Status == "Chờ xác nhận");
            ViewBag.BookingConfirmed = db.Bookings.Count(b => b.Status == "Đã xác nhận");

            // ══ ĐƠN HÀNG ══

            // Tổng đơn tháng này (mọi trạng thái)
            ViewBag.OrderThisMonth = db.Orders
                .Count(o => o.OrderDate >= firstThisMonth);

            // Hôm nay
            ViewBag.OrderToday = db.Orders
                .Count(o => DbFunctions.TruncateTime(o.OrderDate) == today);

            // Theo trạng thái
            ViewBag.OrderWaitConf = db.Orders.Count(o => o.Status == "Chờ xác nhận");
            ViewBag.OrderConfirmed = db.Orders.Count(o => o.Status == "Đã xác nhận");

            // ══ DOANH THU THÁNG NÀY ══

            var allPrices = db.Prices.ToList();

            // Booking đã xác nhận / hoàn thành tháng này
            var confirmedBookings = db.Bookings
                .Where(b => (b.Status == "Đã xác nhận" || b.Status == "Hoàn thành")
                         && b.BookingDate >= firstThisMonth
                         && b.FieldID != null && b.TimeSlotID != null)
                .ToList();

            decimal revenueBooking = confirmedBookings.Sum(b => {
                var pr = allPrices.FirstOrDefault(p => p.FieldID == b.FieldID && p.TimeSlotID == b.TimeSlotID);
                return pr?.Price1 ?? 0m;
            });

            // Order đã xác nhận / hoàn thành tháng này
            decimal revenueOrder = db.Orders
                .Where(o => (o.Status == "Đã xác nhận" || o.Status == "Hoàn thành")
                         && o.OrderDate >= firstThisMonth
                         && o.BookingID == null)   // chi tinh don san pham, khong tinh don booking
                .Sum(o => (decimal?)o.TotalAmount) ?? 0m;

            ViewBag.RevenueBookingMonth = revenueBooking;
            ViewBag.RevenueOrderMonth = revenueOrder;
            ViewBag.RevenueTotalMonth = revenueBooking + revenueOrder;

            // ══ DOANH THU THÁNG TRƯỚC (để so sánh tăng trưởng) ══

            var confirmedBookingsLast = db.Bookings
                .Where(b => (b.Status == "Đã xác nhận" || b.Status == "Hoàn thành")
                         && b.BookingDate >= firstLastMonth
                         && b.BookingDate < firstThisMonth
                         && b.FieldID != null && b.TimeSlotID != null)
                .ToList();

            decimal revenueBookingLast = confirmedBookingsLast.Sum(b => {
                var pr = allPrices.FirstOrDefault(p => p.FieldID == b.FieldID && p.TimeSlotID == b.TimeSlotID);
                return pr?.Price1 ?? 0m;
            });

            decimal revenueOrderLast = db.Orders
                .Where(o => (o.Status == "Đã xác nhận" || o.Status == "Hoàn thành")
                         && o.OrderDate >= firstLastMonth
                         && o.OrderDate < firstThisMonth
                         && o.BookingID == null)
                .Sum(o => (decimal?)o.TotalAmount) ?? 0m;

            ViewBag.RevenueTotalLastMonth = revenueBookingLast + revenueOrderLast;

            // ══ TOP 5 SÂN ĐƯỢC ĐẶT NHIỀU NHẤT (tháng này) ══

            var topFields = db.Bookings
                .Include("Field")
                .Where(b => b.BookingDate >= firstThisMonth && b.FieldID != null)
                .ToList()
                .GroupBy(b => b.FieldID)
                .Select(g => new TopFieldVM
                {
                    FieldName = g.FirstOrDefault()?.Field?.FieldName ?? "—",
                    TotalBookings = g.Count(),
                    TotalRevenue = g.Sum(b => {
                        var pr = allPrices.FirstOrDefault(p => p.FieldID == b.FieldID && p.TimeSlotID == b.TimeSlotID);
                        return pr?.Price1 ?? 0m;
                    })
                })
                .OrderByDescending(x => x.TotalBookings)
                .Take(5)
                .ToList();

            ViewBag.TopFields = topFields;

            // ══ TOP 5 SẢN PHẨM BÁN CHẠY (tháng này) ══

            var topProducts = db.OrderDetails
                .Include("Product")
                .Include("Order")
                .Where(d => d.Order.OrderDate >= firstThisMonth && d.ProductID != null)
                .ToList()
                .GroupBy(d => d.ProductID)
                .Select(g => new TopProductVM
                {
                    ProductName = g.FirstOrDefault()?.Product?.Name ?? "—",
                    TotalQuantity = g.Sum(d => d.Quantity ?? 0),
                    TotalRevenue = g.Sum(d => (d.Price ?? 0) * (d.Quantity ?? 0))
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToList();

            ViewBag.TopProducts = topProducts;

            // ══ 8 ĐẶT SÂN GẦN NHẤT ══

            ViewBag.RecentBookings = db.Bookings
                .Include("User")
                .Include("Field")
                .OrderByDescending(b => b.CreatedAt)
                .Take(8)
                .ToList();

            // ══ 8 ĐƠN HÀNG GẦN NHẤT ══

            ViewBag.RecentOrders = db.Orders
                .Include("User")
                .OrderByDescending(o => o.OrderDate)
                .Take(8)
                .ToList();

            // ══ BIỂU ĐỒ DOANH THU 6 THÁNG ══

            var chartData = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var mStart = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var mEnd = mStart.AddMonths(1);

                var booksInMonth = db.Bookings
                    .Where(b => (b.Status == "Đã xác nhận" || b.Status == "Hoàn thành")
                             && b.BookingDate >= mStart && b.BookingDate < mEnd
                             && b.FieldID != null && b.TimeSlotID != null)
                    .ToList();

                decimal revB = booksInMonth.Sum(b => {
                    var pr = allPrices.FirstOrDefault(p => p.FieldID == b.FieldID && p.TimeSlotID == b.TimeSlotID);
                    return pr?.Price1 ?? 0m;
                });

                decimal revO = db.Orders
                    .Where(o => (o.Status == "Đã xác nhận" || o.Status == "Hoàn thành")
                             && o.OrderDate >= mStart && o.OrderDate < mEnd
                             && o.BookingID == null)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0m;

                chartData.Add(new
                {
                    month = mStart.ToString("MM/yyyy"),
                    booking = (long)revB,
                    order = (long)revO,
                    total = (long)(revB + revO)
                });
            }

            ViewBag.ChartDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(chartData);

            return View();
        }
    }
}