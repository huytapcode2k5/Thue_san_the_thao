using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Areas.Admin.Controllers
{
    [Authorize]
    public class BookingSanController : Controller
    {
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        // ───────────────────────────────────────────
        // INDEX – danh sách đơn đặt sân
        // ───────────────────────────────────────────
        public ActionResult Index(string search, string status)
        {
            var bookings = db.Bookings
                           .Include("User")
                           .Include("TimeSlot")     // load khung giờ để hiển thị + tra giá
                           .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                int bookingID;
                if (int.TryParse(search, out bookingID))
                    bookings = bookings.Where(o => o.BookingID == bookingID);
                else
                    bookings = bookings.Where(o => o.User.FullName.Contains(search)
                                            || o.User.Email.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
                bookings = bookings.Where(o => o.Status == status);

            ViewBag.Status = status;
            ViewBag.Search = search;

            // Dictionary: TimeSlotID -> Price1 để tra giá nhanh trong view
            // GroupBy de tranh loi duplicate key neu co nhieu ban ghi cung TimeSlotID
            var priceDict = db.Prices
                              .Where(p => p.TimeSlotID != null)
                              .ToList()
                              .GroupBy(p => p.TimeSlotID.Value)
                              .ToDictionary(g => g.Key, g => g.First().Price1);

            ViewBag.PriceDict = priceDict;

            return View(bookings.OrderByDescending(o => o.BookingDate).ToList());
        }

        // ───────────────────────────────────────────
        // DETAILS – chi tiết đơn hàng
        // ───────────────────────────────────────────
        public ActionResult Details(int id)
        {
            var booking = db.Bookings
                          .Include("User")
                          .Include("Orders")            // load danh sách order
                          .Include("TimeSlot")          // load khung giờ
                          .Include("Field")             // Field nằm ở Booking (có FieldID)
                          .Include("Field.Facility")    // load thêm cơ sở của sân
                          .FirstOrDefault(o => o.BookingID == id);

            if (booking == null)
                return HttpNotFound();

            // Lấy mức giá tương ứng với TimeSlotID của booking này
            Price price = null;
            if (booking.TimeSlotID.HasValue)
                price = db.Prices.FirstOrDefault(p => p.TimeSlotID == booking.TimeSlotID);

            ViewBag.Price = price;

            return View(booking);
        }

        // ───────────────────────────────────────────
        // UPDATE STATUS (POST)
        // ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string status)
        {
            var booking = db.Bookings.Find(id);
            if (booking == null)
                return HttpNotFound();

            booking.Status = status;
            db.SaveChanges();

            TempData["Success"] = "Cập nhật trạng thái đơn hàng #" + id + " thành công!";
            return RedirectToAction("Details", new { id = id });
        }

        // ───────────────────────────────────────────
        // DELETE
        // ───────────────────────────────────────────
        public ActionResult Delete(int id)
        {
            var booking = db.Bookings.Include("Orders").FirstOrDefault(o => o.BookingID == id);
            if (booking == null)
                return HttpNotFound();

            db.Orders.RemoveRange(booking.Orders);
            db.Bookings.Remove(booking);
            db.SaveChanges();

            TempData["Success"] = "Đã xóa đơn hàng #" + id;
            return RedirectToAction("Index");
        }
    }
}