using System;
using System.Linq;
using System.Web.Mvc;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Areas.Admin.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        // Lấy user hiện tại từ Session
        private User GetCurrentUser()
        {
            var userId = Session["UserId"];
            if (userId == null) return null;
            return db.Users.Find((int)userId);
        }

        // ===== XEM THÔNG TIN =====
        public ActionResult Index()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");
            return View(user);
        }

        // ===== CẬP NHẬT THÔNG TIN =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string FullName, string Phone)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");

            user.FullName = FullName;
            user.Phone = Phone;

            db.SaveChanges();

            // Cập nhật lại tên trong Session
            Session["UserName"] = user.FullName;

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Index");
        }

        // ===== ĐỔI MẬT KHẨU =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");

            // Kiểm tra mật khẩu cũ
            if (user.PasswordHash != OldPassword)
            {
                TempData["PwError"] = "Mật khẩu cũ không đúng.";
                return RedirectToAction("Index");
            }

            // Kiểm tra mật khẩu mới khớp nhau
            if (NewPassword != ConfirmPassword)
            {
                TempData["PwError"] = "Mật khẩu mới không khớp.";
                return RedirectToAction("Index");
            }

            // Kiểm tra độ dài
            if (NewPassword.Length < 6)
            {
                TempData["PwError"] = "Mật khẩu mới phải ít nhất 6 ký tự.";
                return RedirectToAction("Index");
            }

            user.PasswordHash = NewPassword;
            db.SaveChanges();

            TempData["PwSuccess"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index");
        }
    }
}