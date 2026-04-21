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
    public class OrderController : Controller
    {
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        // ───────────────────────────────────────────
        // INDEX – danh sách đơn hàng (có lọc + tìm)
        // ───────────────────────────────────────────
        public ActionResult Index(string search, string status)
        {
            var orders = db.Orders
                           .Include("User")
                           .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                int orderId;
                if (int.TryParse(search, out orderId))
                    orders = orders.Where(o => o.OrderID == orderId);
                else
                    orders = orders.Where(o => o.User.FullName.Contains(search)
                                            || o.User.Email.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
                orders = orders.Where(o => o.Status == status);

            ViewBag.Status = status;
            ViewBag.Search = search;

            return View(orders.OrderByDescending(o => o.OrderDate).ToList());
        }

        // ───────────────────────────────────────────
        // DETAILS – chi tiết đơn hàng + danh sách sản phẩm
        // ───────────────────────────────────────────
        public ActionResult Details(int id)
        {
            var order = db.Orders
                          .Include("User")
                          .Include("OrderDetails.Product")
                          .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        // ───────────────────────────────────────────
        // UPDATE STATUS (POST Ajax-friendly)
        // ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string status)
        {
            var order = db.Orders
                          .Include("OrderDetails.Product")
                          .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
                return HttpNotFound();

            // Không cho đổi lại nếu đã hoàn thành hoặc đã huỷ
            if (order.Status == "Hoàn thành" || order.Status == "Đã hủy")
            {
                TempData["Error"] = "Đơn hàng đã kết thúc, không thể thay đổi trạng thái!";
                return RedirectToAction("Details", new { id });
            }

            // ★ Trừ kho khi chuyển sang "Hoàn thành"
            if (status == "Hoàn thành")
            {
                foreach (var detail in order.OrderDetails)
                {
                    var product = detail.Product;
                    if (product == null) continue;

                    int qty = detail.Quantity ?? 0;
                    if (product.Stock < qty)
                    {
                        TempData["Error"] = $"Sản phẩm {product.Name} không đủ hàng! Còn {product.Stock}; cần {qty}.";
                        return RedirectToAction("Details", new { id });
                    }

                    product.Stock -= qty;
                }
            }

            order.Status = status;
            db.SaveChanges();

            TempData["Success"] = status == "Hoàn thành"
                ? $"Đơn #{id} hoàn thành — đã trừ tồn kho!"
                : $"Cập nhật trạng thái đơn #{id} thành công!";

            return RedirectToAction("Details", new { id });
        }

        // ───────────────────────────────────────────
        // DELETE
        // ───────────────────────────────────────────
        public ActionResult Delete(int id)
        {
            var order = db.Orders.Include("OrderDetails").FirstOrDefault(o => o.OrderID == id);
            if (order == null)
                return HttpNotFound();

            db.OrderDetails.RemoveRange(order.OrderDetails);
            db.Orders.Remove(order);
            db.SaveChanges();

            TempData["Success"] = "Đã xóa đơn hàng #" + id;
            return RedirectToAction("Index");
        }
    }
}