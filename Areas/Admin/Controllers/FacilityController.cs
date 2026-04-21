using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Areas.Admin.Controllers
{
    [Authorize]
    public class FacilityController : Controller
    {
        // GET: Admin/Facillity
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();
        public ActionResult Index()
        {
            return View(db.Facilities.ToList());
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Facility c)
        {
            if (ModelState.IsValid)
            {
                db.Facilities.Add(c);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(c);
        }

        public ActionResult Edit(int id)
        {
            var c = db.Facilities.Find(id);
            return View(c);
        }

        [HttpPost]
        public ActionResult Edit(Category c)
        {
            if (ModelState.IsValid)
            {
                db.Entry(c).State = System.Data.Entity.EntityState.Modified;
                //db.Entry(c).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(c);
        }

        public ActionResult Delete(int id)
        {
            var c = db.Facilities.Find(id);
            db.Facilities.Remove(c);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}