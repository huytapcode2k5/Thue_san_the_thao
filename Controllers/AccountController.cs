using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Areas.Admin.Controllers
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
                return RedirectToAction("Trangchu", "HomeAdmin",new { area = "Admin" });

            return RedirectToAction("Trangchu", "Home");
        }
        
    }
}