using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        private int? GetUserId()
        {
            var id = Session["UserId"];
            if (id == null) return null;
            return (int)id;
        }

        private ActionResult RequireLogin()
        {
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        private Cart GetOrCreateCart(int userId)
        {
            var cart = db.Carts.FirstOrDefault(c => c.UserID == userId);
            if (cart == null)
            {
                cart = new Cart { UserID = userId, CreatedAt = DateTime.Now };
                db.Carts.Add(cart);
                db.SaveChanges();
            }
            return cart;
        }

        // ===== XEM GIỎ HÀNG =====
        public ActionResult Index()
        {
            var uid = GetUserId();
            if (uid == null) return RequireLogin();

            var cart = GetOrCreateCart(uid.Value);
            var items = db.CartItems
                .Include("Product")
                .Where(i => i.CartID == cart.CartID)
                .ToList();
            return View(items);
        }

        // ===== THÊM VÀO GIỎ =====
        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            var uid = GetUserId();
            if (uid == null) return RequireLogin();

            var cart = GetOrCreateCart(uid.Value);
            var item = db.CartItems.FirstOrDefault(i => i.CartID == cart.CartID && i.ProductID == productId);
            if (item == null)
            {
                db.CartItems.Add(new CartItem
                {
                    CartID = cart.CartID,
                    ProductID = productId,
                    Quantity = quantity
                });
            }
            else
            {
                item.Quantity += quantity;
            }
            db.SaveChanges();
            TempData["Success"] = "Đã thêm vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        // ===== CẬP NHẬT SỐ LƯỢNG =====
        [HttpPost]
        public ActionResult UpdateQuantity(int cartItemId, int quantity)
        {
            var uid = GetUserId();
            if (uid == null) return RequireLogin();

            var item = db.CartItems.Find(cartItemId);
            if (item != null)
            {
                if (quantity <= 0)
                    db.CartItems.Remove(item);
                else
                    item.Quantity = quantity;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // ===== XÓA KHỎI GIỎ =====
        [HttpPost]
        public ActionResult Remove(int cartItemId)
        {
            var uid = GetUserId();
            if (uid == null) return RequireLogin();

            var item = db.CartItems.Find(cartItemId);
            if (item != null)
            {
                db.CartItems.Remove(item);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // ===== TRANG THANH TOÁN =====
        public ActionResult Checkout()
        {
            var uid = GetUserId();
            if (uid == null) return RequireLogin();

            var cart = GetOrCreateCart(uid.Value);
            var items = db.CartItems
                .Include("Product")
                .Where(i => i.CartID == cart.CartID)
                .ToList();

            if (!items.Any())
                return RedirectToAction("Index");

            return View(items);
        }

        // ===== HIỆN MÃ QR (chưa tạo đơn, chỉ tính tiền + hiện QR) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(string ReceiverName, string ReceiverPhone,
                                       string ShippingAddress, string ShippingNote)
        {
            var uid = GetUserId();
            if (uid == null) return RequireLogin();

            var cart = GetOrCreateCart(uid.Value);
            var items = db.CartItems
                .Include("Product")
                .Where(i => i.CartID == cart.CartID)
                .ToList();

            if (!items.Any())
                return RedirectToAction("Index");

            // Tính tổng tiền và lưu vào Session để dùng lúc xác nhận
            decimal total = items.Sum(i => i.Product.Price * (i.Quantity ?? 1));
            Session["PendingTotal"] = total;
            Session["ReceiverName"] = ReceiverName;
            Session["ReceiverPhone"] = ReceiverPhone;
            Session["ShippingAddress"] = ShippingAddress;
            Session["ShippingNote"] = ShippingNote;

            // Chuyển sang trang QR — chưa tạo đơn, chưa xóa giỏ
            return RedirectToAction("PaymentQR");
        }

        // ===== TRANG HIỆN MÃ QR =====
        public ActionResult PaymentQR()
        {
            var uid = GetUserId();
            if (uid == null) return RequireLogin();

            var cart = GetOrCreateCart(uid.Value);
            var items = db.CartItems
                .Include("Product")
                .Where(i => i.CartID == cart.CartID)
                .ToList();

            // Nếu giỏ trống (user back vào trang này sau khi đã xác nhận) thì về giỏ
            if (!items.Any())
                return RedirectToAction("Index");

            decimal total = items.Sum(i => i.Product.Price * (i.Quantity ?? 1));
            ViewBag.Total = total;

            return View(items);
        }

        // ===== XÁC NHẬN ĐÃ CHUYỂN KHOẢN → MỚI tạo đơn + xóa giỏ =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPayment()
        {
            var uid = GetUserId();
            if (uid == null) return RequireLogin();

            var cart = GetOrCreateCart(uid.Value);
            var items = db.CartItems
                .Include("Product")
                .Where(i => i.CartID == cart.CartID)
                .ToList();

            if (!items.Any())
            {
                TempData["Error"] = "Giỏ hàng trống, không thể tạo đơn.";
                return RedirectToAction("Index");
            }

            decimal total = items.Sum(i => i.Product.Price * (i.Quantity ?? 1));

            // Tạo đơn hàng
            var order = new Order
            {
                UserID = uid.Value,
                OrderDate = DateTime.Now,
                TotalAmount = total,
                Status = "pending",
                ReceiverName = Session["ReceiverName"]?.ToString(),
                ReceiverPhone = Session["ReceiverPhone"]?.ToString(),
                ShippingAddress = Session["ShippingAddress"]?.ToString(),
                ShippingNote = Session["ShippingNote"]?.ToString()
            };
            db.Orders.Add(order);
            db.SaveChanges();

            // Lưu chi tiết đơn
            foreach (var item in items)
            {
                db.OrderDetails.Add(new OrderDetail
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    Price = item.Product.Price
                });
            }

            // Tạo bản ghi thanh toán
            db.Payments.Add(new Payment
            {
                OrderID = order.OrderID,
                Amount = total,
                Method = "QR",
                Status = "pending",
                CreatedAt = DateTime.Now
            });

            db.SaveChanges();

            // Xóa giỏ hàng — chỉ xóa SAU KHI user xác nhận đã chuyển khoản
            db.CartItems.RemoveRange(items);
            db.SaveChanges();

            Session.Remove("PendingTotal");
            Session.Remove("ReceiverName");
            Session.Remove("ReceiverPhone");
            Session.Remove("ShippingAddress");
            Session.Remove("ShippingNote");

            TempData["PaySuccess"] = $"Đơn hàng #{order.OrderID} đã được ghi nhận! Chúng tôi sẽ xác nhận sau khi kiểm tra thanh toán.";
            return RedirectToAction("Index", "History", new { tab = "order" });
        }
    }
}