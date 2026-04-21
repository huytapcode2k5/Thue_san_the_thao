using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Controllers
{
    public class HomeController : Controller
    {
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        public ActionResult Trangchu()
        {
            ViewBag.Products = db.Products
                                 .Where(p => p.Status == true)
                                 .OrderByDescending(p => p.ProductID)
                                 .Take(8)
                                 .ToList();

            ViewBag.Reviews = db.Reviews
                                .Include("User")
                                .OrderByDescending(r => r.CreatedAt)
                                .Take(6)
                                .ToList();

            // Load san tu DB va build JSON string ngay tai Controller
            // Khong dung anonymous type de tranh loi Razor/serialize o View
            var allFields = db.Fields
                              .Include("Prices")
                              .Where(f => f.Status == true)
                              .ToList();

            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            bool firstGroup = true;
            foreach (var g in allFields.GroupBy(f => (f.Type ?? "other").ToLower().Trim()))
            {
                if (!firstGroup) sb.Append(",");
                firstGroup = false;

                sb.Append("\"" + g.Key + "\":[");
                bool firstField = true;
                foreach (var f in g)
                {
                    if (!firstField) sb.Append(",");
                    firstField = false;

                    decimal price = f.Prices.Any() ? f.Prices.Min(p => p.Price1 ?? 0) : 0m;
                    string priceStr = price.ToString("N0",
                        System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
                    string safeName = (f.FieldName ?? "")
                        .Replace("\\", "\\\\")
                        .Replace("\"", "\\\"");
                    string img = GetDefaultImage(f.Type);
                    // Neu Field co cot Image va co gia tri thi dung anh rieng
                    if (!string.IsNullOrEmpty(f.Image))
                        img = f.Image;

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

        private string GetDefaultImage(string type)
        {
            switch ((type ?? "").ToLower().Trim())
            {
                case "football": return "bernabeu.jpg";
                case "badminton": return "caulong.jpg";
                case "tennis": return "tennis.jpg";
                case "pickleball": return "pickleball.jpg";
                default: return "default.jpg";
            }
        }

        [HttpPost]
        public JsonResult SubmitReview(int rating, string comment)
        {
            if (Session["UserId"] == null)
                return Json(new { success = false, message = "Ban can dang nhap de danh gia!" });
            if (rating < 1 || rating > 5)
                return Json(new { success = false, message = "Vui long chon so sao!" });
            if (string.IsNullOrWhiteSpace(comment))
                return Json(new { success = false, message = "Vui long nhap noi dung!" });
            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                var review = new Review
                {
                    UserID = userId,
                    Rating = rating,
                    Comment = comment.Trim(),
                    CreatedAt = DateTime.Now
                };
                db.Reviews.Add(review);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Loi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AddToCart(int productId, int quantity = 1)
        {
            if (Session["UserId"] == null)
                return Json(new { success = false, message = "Ban can dang nhap!" });
            int userId = Convert.ToInt32(Session["UserId"]);
            try
            {
                var product = db.Products.FirstOrDefault(p => p.ProductID == productId && p.Status == true);
                if (product == null)
                    return Json(new { success = false, message = "San pham khong ton tai!" });
                if (product.Stock < quantity)
                    return Json(new { success = false, message = "Khong du hang!" });

                var cart = db.Carts.FirstOrDefault(c => c.UserID == userId);
                if (cart == null)
                {
                    cart = new Cart { UserID = userId, CreatedAt = DateTime.Now };
                    db.Carts.Add(cart);
                    db.SaveChanges();
                }

                var item = db.CartItems.FirstOrDefault(ci => ci.CartID == cart.CartID && ci.ProductID == productId);
                if (item != null)
                    item.Quantity += quantity;
                else
                    db.CartItems.Add(new CartItem { CartID = cart.CartID, ProductID = productId, Quantity = quantity });

                db.SaveChanges();
                int cartCount = db.CartItems.Where(ci => ci.CartID == cart.CartID).Sum(ci => (int?)ci.Quantity) ?? 0;
                return Json(new { success = true, message = "Da them vao gio hang!", cartCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Loi: " + ex.Message });
            }
        }
    }
}