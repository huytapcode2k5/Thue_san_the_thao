using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Controllers
{
    [Authorize]
    public class ShopController : Controller
    {
        // GET: Shop
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        // Trang sản phẩm cho khách xem
        public ActionResult Index()
        {
            var list = db.Products.Where(p => p.Status == true).ToList();
            ViewBag.Categories = db.Categories.ToList();
            return View(list);
        }

        // ✅ Thêm vào giỏ hàng
        [HttpPost]
        public JsonResult AddToCart(int productId, int quantity = 1)
        {
            if (Session["UserId"] == null)
                return Json(new { success = false, message = "Bạn cần đăng nhập!" });

            int userId = Convert.ToInt32(Session["UserId"]);

            try
            {
                // Kiểm tra sản phẩm còn hàng không
                var product = db.Products.FirstOrDefault(p => p.ProductID == productId && p.Status == true);
                if (product == null)
                    return Json(new { success = false, message = "Sản phẩm không tồn tại!" });

                if (product.Stock < quantity)
                    return Json(new { success = false, message = "Sản phẩm không đủ hàng!" });

                // Tìm hoặc tạo Cart cho user
                var cart = db.Carts.FirstOrDefault(c => c.UserID == userId);
                if (cart == null)
                {
                    cart = new Cart { UserID = userId, CreatedAt = DateTime.Now };
                    db.Carts.Add(cart);
                    db.SaveChanges();
                }

                // Kiểm tra sản phẩm đã có trong giỏ chưa
                var cartItem = db.CartItems.FirstOrDefault(ci => ci.CartID == cart.CartID && ci.ProductID == productId);
                if (cartItem != null)
                {
                    // Đã có thì tăng số lượng
                    cartItem.Quantity += quantity;
                }
                else
                {
                    // Chưa có thì thêm mới
                    cartItem = new CartItem
                    {
                        CartID = cart.CartID,
                        ProductID = productId,
                        Quantity = quantity
                    };
                    db.CartItems.Add(cartItem);
                }

                db.SaveChanges();

                // Đếm tổng số item trong giỏ
                int cartCount = db.CartItems.Where(ci => ci.CartID == cart.CartID).Sum(ci => ci.Quantity) ?? 0;

                return Json(new { success = true, message = "Đã thêm vào giỏ hàng!", cartCount = cartCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ===== ADMIN =====
        public ActionResult Create()
        {
            ViewBag.Categories = db.Categories.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(Product p)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            p.ProductDate = DateTime.Now;
            p.Status = true;
            db.Products.Add(p);
            db.SaveChanges();
            return RedirectToAction("Index", "Shop");
        }

        public ActionResult Edit(int id)
        {
            var p = db.Products.FirstOrDefault(x => x.ProductID == id);
            ViewBag.Categories = db.Categories.ToList();
            return View(p);
        }

        [HttpPost]
        public ActionResult Edit(Product p)
        {
            var data = db.Products.FirstOrDefault(x => x.ProductID == p.ProductID);
            if (data != null)
            {
                data.Name = p.Name;
                data.Price = p.Price;
                data.Stock = p.Stock;
                data.Image = p.Image;
                data.Description = p.Description;
                data.CategoryID = p.CategoryID;
                data.Status = p.Status;
                db.SaveChanges();
            }
            return RedirectToAction("Index", "Shop");
        }

        public ActionResult Delete(int id)
        {
            var p = db.Products.FirstOrDefault(x => x.ProductID == id);
            if (p != null)
            {
                db.Products.Remove(p);
                db.SaveChanges();
            }
            return RedirectToAction("Index", "Shop");
        }
    }
}