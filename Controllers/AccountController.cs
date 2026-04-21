using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Data.Entity;
using System.Web.Security;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Controllers
{
    public class AccountController : Controller
    {
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        [AllowAnonymous]
        public ActionResult Login()
        {
            return View(new User());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User model, string Password)
        {
            // Tìm user theo email trước
            var user = db.Users.FirstOrDefault(u => u.Email == model.Email);

            if (user == null)
            {
                ViewBag.Error = "Email không tồn tại.";
                return View(model);
            }

            // So sánh mật khẩu (plain text)
            // Nếu DB lưu plain text thì dùng dòng này:
            if (user.PasswordHash != Password)
            {
                ViewBag.Error = "Mật khẩu không đúng.";
                return View(model);
            }
            FormsAuthentication.SetAuthCookie(user.Email, false);
            // Lưu session
            Session["UserId"] = user.UserID;
            Session["UserName"] = user.FullName;
            Session["RoleID"] = user.RoleID;   // lưu RoleID thay vì Role object

            // Phân quyền theo RoleID
            if (user.RoleID == 1)
                return RedirectToAction("Trangchu", "HomeAdmin", new { area = "Admin" });

            return RedirectToAction("Trangchu", "Home");
        }
        public ActionResult Logout()
        {
            // Xóa session
            Session.Clear();
            Session.Abandon();

            // 🔥 XÓA COOKIE ĐĂNG NHẬP
            System.Web.Security.FormsAuthentication.SignOut();

            return RedirectToAction("Login", "Account");
        }
        // Lấy user hiện tại từ Session
        private User GetCurrentUser()
        {
            var userId = Session["UserId"];
            if (userId == null) return null;
            return db.Users.Find((int)userId);
        }
        // ===== PROFILE - XEM =====
        public ActionResult Profile()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login");
            return View(user);
        }

        // ===== PROFILE - CẬP NHẬT THÔNG TIN =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateInfo(string FullName, string Phone)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login");

            user.FullName = FullName;
            user.Phone = Phone;
            db.SaveChanges();

            Session["UserName"] = user.FullName;
            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        // ===== PROFILE - ĐỔI MẬT KHẨU =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login");

            if (user.PasswordHash != OldPassword)
            {
                TempData["PwError"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction("Profile");
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["PwError"] = "Mật khẩu mới không khớp.";
                return RedirectToAction("Profile");
            }

            if (NewPassword.Length < 6)
            {
                TempData["PwError"] = "Mật khẩu mới phải ít nhất 6 ký tự.";
                return RedirectToAction("Profile");
            }

            user.PasswordHash = NewPassword;
            db.SaveChanges();

            TempData["PwSuccess"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }
        //ktra quen mk
        // ===== BƯỚC 1: Nhập email =====
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // ===== BƯỚC 2: Kiểm tra email + số điện thoại =====
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string Email, string Phone)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == Email);

            if (user == null)
            {
                ViewBag.Error = "Email này chưa được đăng ký.";
                return View();
            }

            if (user.Phone != Phone)
            {
                ViewBag.Error = "Số điện thoại không khớp với tài khoản.";
                return View();
            }

            // Lưu UserID vào Session tạm để dùng ở bước đặt mật khẩu mới
            Session["ResetUserId"] = user.UserID;

            return RedirectToAction("ResetPassword");
        }

        // ===== BƯỚC 3: Nhập mật khẩu mới =====
        [AllowAnonymous]
        public ActionResult ResetPassword()
        {
            // Nếu không qua bước ForgotPassword thì không cho vào
            if (Session["ResetUserId"] == null)
                return RedirectToAction("ForgotPassword");

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string NewPassword, string ConfirmPassword)
        {
            if (Session["ResetUserId"] == null)
                return RedirectToAction("ForgotPassword");

            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            if (NewPassword.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải ít nhất 6 ký tự.";
                return View();
            }

            int userId = (int)Session["ResetUserId"];
            var user = db.Users.Find(userId);

            if (user == null)
            {
                ViewBag.Error = "Tài khoản không tồn tại.";
                return View();
            }

            user.PasswordHash = NewPassword;
            db.SaveChanges();

            // Xoá session tạm
            Session.Remove("ResetUserId");

            TempData["ResetSuccess"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập lại.";
            return RedirectToAction("Login");
        }
        public ActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(User model, string ConfirmPassword)
        {
            if (string.IsNullOrWhiteSpace(model.FullName) || model.FullName.Trim().Length < 2)
                ModelState.AddModelError("FullName", "Họ tên không hợp lệ (tối thiểu 2 ký tự)");

            if (string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError("Email", "Vui lòng nhập email");
            else if (!Regex.IsMatch(model.Email.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                ModelState.AddModelError("Email", "Email không hợp lệ");

            if (string.IsNullOrWhiteSpace(model.Phone))
                ModelState.AddModelError("Phone", "Vui lòng nhập số điện thoại");
            else if (!Regex.IsMatch(model.Phone.Trim(), @"^\d{10}$"))
                ModelState.AddModelError("Phone", "SĐT phải đúng 10 chữ số");

            if (string.IsNullOrEmpty(model.PasswordHash) || model.PasswordHash.Length < 6)
                ModelState.AddModelError("PasswordHash", "Mật khẩu tối thiểu 6 ký tự");
            else if (model.PasswordHash != ConfirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu không khớp");

            if (!ModelState.IsValid)
            {
                var formData = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone
                };
                return View(formData);
            }

            if (db.Users.Any(x => x.Email == model.Email.Trim()))
            {
                ViewBag.Error = "Email này đã được đăng ký";
                var formData = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone
                };
                return View(formData);
            }

            // ✅ Tạo object mới hoàn toàn để lưu — tránh EF dùng object cũ đã bị track
            var newUser = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                PasswordHash = model.PasswordHash,
                Status = true,
                CreatedAt = DateTime.Now
            };

            var role = db.Roles.FirstOrDefault(r => r.RoleName == "User");
            if (role != null)
                newUser.RoleID = role.RoleID;

            db.Users.Add(newUser);
            db.SaveChanges();

            TempData["Success"] = "Đăng ký thành công!";
            return RedirectToAction("Register");
        }
    }
}