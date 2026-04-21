using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Thue_san_the_thao.Areas.Admin.Data;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Areas.Admin.Controllers
{
    public class RevenueController : Controller
    {
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        public ActionResult Index(int? year, int? month)
        {
            int  selectedYear  = year  ?? DateTime.Now.Year;
            int? selectedMonth = month;

            // ── Danh sách năm có dữ liệu ──
            var orderYears = db.Orders
                .Where(o => o.OrderDate.HasValue && o.BookingID == null)
                .Select(o => o.OrderDate.Value.Year)
                .Distinct().ToList();

            var bookingYears = db.Bookings
                .Where(b => b.BookingDate.HasValue
                         && (b.Status == "Hoàn thành" || b.Status == "Đã xác nhận"))
                .Select(b => b.BookingDate.Value.Year)
                .Distinct().ToList();

            var allYears = orderYears
                .Union(bookingYears)
                .Union(new[] { DateTime.Now.Year })
                .OrderByDescending(y => y)
                .ToList();

            // ────────────────────────────────────────────
            // DOANH THU SẢN PHẨM
            // Lấy từ bảng Orders (không có BookingID)
            // Status "Hoàn thành" — admin set khi hoàn thành đơn hàng
            // ────────────────────────────────────────────
            var orderQuery = db.Orders
                .Where(o => o.OrderDate.HasValue
                         && o.BookingID == null
                         && o.Status == "Hoàn thành"
                         && o.OrderDate.Value.Year == selectedYear);

            if (selectedMonth.HasValue)
                orderQuery = orderQuery
                    .Where(o => o.OrderDate.Value.Month == selectedMonth.Value);

            var orderByMonth = orderQuery
                .GroupBy(o => o.OrderDate.Value.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(o => o.TotalAmount ?? 0) })
                .ToList();

            // ────────────────────────────────────────────
            // DOANH THU ĐẶT SÂN
            // Lấy từ bảng Bookings (status "Hoàn thành" hoặc "Đã xác nhận")
            // Giá lấy từ bảng Prices qua TimeSlotID + FieldID
            // ────────────────────────────────────────────
            var bookingQuery = db.Bookings
                .Include("TimeSlot.Prices")
                .Include("Field")
                .Where(b => b.BookingDate.HasValue
                         && (b.Status == "Hoàn thành" || b.Status == "Đã xác nhận")
                         && b.BookingDate.Value.Year == selectedYear);

            if (selectedMonth.HasValue)
                bookingQuery = bookingQuery
                    .Where(b => b.BookingDate.Value.Month == selectedMonth.Value);

            var bookings = bookingQuery.ToList();

            // ── Khởi tạo mảng 12 tháng ──
            decimal[] productRevenue = new decimal[12];
            decimal[] bookingRevenue = new decimal[12];

            foreach (var item in orderByMonth)
                productRevenue[item.Month - 1] = item.Total;

            foreach (var b in bookings)
            {
                int m = b.BookingDate.Value.Month;

                // Lấy giá từ bảng Prices theo TimeSlotID và FieldID
                decimal gia = 0;
                if (b.TimeSlotID.HasValue && b.TimeSlot?.Prices != null && b.FieldID.HasValue)
                {
                    var price = b.TimeSlot.Prices
                                 .FirstOrDefault(p => p.FieldID == b.FieldID.Value);
                    gia = price?.Price1 ?? 0;
                }

                bookingRevenue[m - 1] += gia;
            }

            // ── Tổng quan ──
            decimal totalProductRevenue = productRevenue.Sum();
            decimal totalBookingRevenue = bookingRevenue.Sum();
            decimal grandTotal          = totalProductRevenue + totalBookingRevenue;

            // ────────────────────────────────────────────
            // TOP 5 SẢN PHẨM BÁN CHẠY
            // ────────────────────────────────────────────
            var topProducts = db.OrderDetails
                .Include("Product")
                .Where(d => d.Order.OrderDate.HasValue
                         && d.Order.Status == "Hoàn thành"
                         && d.Order.BookingID == null
                         && d.Order.OrderDate.Value.Year == selectedYear
                         && (!selectedMonth.HasValue
                             || d.Order.OrderDate.Value.Month == selectedMonth.Value))
                .GroupBy(d => new { d.ProductID, d.Product.Name })
                .Select(g => new TopProductVM
                {
                    ProductName   = g.Key.Name,
                    TotalQuantity = g.Sum(d => d.Quantity ?? 0),
                    TotalRevenue  = g.Sum(d => (d.Quantity ?? 0) * (d.Price ?? 0))
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(5)
                .ToList();

            // ────────────────────────────────────────────
            // TOP 5 SÂN ĐƯỢC ĐẶT NHIỀU
            // ────────────────────────────────────────────
            var topFields = bookings
                .Where(b => b.Field != null)
                .GroupBy(b => new { b.FieldID, FieldName = b.Field.FieldName })
                .Select(g =>
                {
                    decimal rev = 0;
                    foreach (var b in g)
                    {
                        if (b.TimeSlot?.Prices != null && b.FieldID.HasValue)
                        {
                            var p = b.TimeSlot.Prices
                                     .FirstOrDefault(pr => pr.FieldID == b.FieldID.Value);
                            rev += p?.Price1 ?? 0;
                        }
                    }
                    return new TopFieldVM
                    {
                        FieldName     = g.Key.FieldName,
                        TotalBookings = g.Count(),
                        TotalRevenue  = rev
                    };
                })
                .OrderByDescending(f => f.TotalRevenue)
                .Take(5)
                .ToList();

            // ── Truyền sang View ──
            ViewBag.SelectedYear        = selectedYear;
            ViewBag.SelectedMonth       = selectedMonth;
            ViewBag.AllYears            = allYears;
            ViewBag.ProductRevenue      = productRevenue;
            ViewBag.BookingRevenue      = bookingRevenue;
            ViewBag.TotalProductRevenue = totalProductRevenue;
            ViewBag.TotalBookingRevenue = totalBookingRevenue;
            ViewBag.GrandTotal          = grandTotal;
            ViewBag.TopProducts         = topProducts;
            ViewBag.TopFields           = topFields;

            return View();
        }
    }
}