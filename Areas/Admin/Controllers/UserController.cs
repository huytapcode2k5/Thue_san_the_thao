using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Thue_san_the_thao.Models.Data;
using System.Data.Entity;

namespace Thue_san_the_thao.Areas.Admin.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        // GET: Admin/User
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();
        public ActionResult Index()
        {
            var users = db.Users.Include(u => u.Role).ToList();
            return View(users);
        }

        // CREATE GET
        public ActionResult Create()
        {
            ViewBag.RoleID = new SelectList(db.Roles, "RoleID", "RoleName");
            return View();
        }

        // CREATE POST
        [HttpPost]
        public ActionResult Create(User user)
        {
            if (ModelState.IsValid)
            {
                user.CreatedAt = System.DateTime.Now;
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RoleID = new SelectList(db.Roles, "RoleID", "RoleName", user.RoleID);
            return View(user);
        }

        // EDIT GET
        public ActionResult Edit(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            ViewBag.RoleID = new SelectList(db.Roles, "RoleID", "RoleName", user.RoleID);
            return View(user);
        }

        // EDIT POST
        [HttpPost]
        public ActionResult Edit(User user)
        {
            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RoleID = new SelectList(db.Roles, "RoleID", "RoleName", user.RoleID);
            return View(user);
        }

        // DELETE
        public ActionResult Delete(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            db.Users.Remove(user);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}