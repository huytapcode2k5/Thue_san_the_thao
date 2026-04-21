using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Areas.Admin.Controllers
{
    [Authorize]
    public class FieldController : Controller
    {
        // GET: Admin/Field
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();
        public ActionResult Index(string search, int? FacilityId)
        {
            var fields = db.Fields.Include("Facility").AsQueryable();

            // lọc theo tên
            if (!string.IsNullOrEmpty(search))
            {
                fields = fields.Where(p => p.FieldName.Contains(search));
            }

            // lọc theo danh mục
            if (FacilityId.HasValue)
            {
                fields = fields.Where(p => p.FacilityID == FacilityId);
            }

            ViewBag.FacilityID = new SelectList(db.Facilities, "FacilityID", "FacilityName");
            ViewBag.PriceId = new SelectList(db.Prices, "PriceID", "Price1");

            return View(fields.ToList());
        }
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.FacilityID = new SelectList(db.Facilities, "FacilityID", "FacilityName");
            ViewBag.PriceId = new SelectList(db.Prices, "PriceID", "Price1");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Models.Data.Field entity)
        {
            try
            {
                //if (!ModelState.IsValid)
                //{
                //    ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "Name");
                //    return View(entity);
                //}

                //var f = Request.Files["ImageFile"];
                //if (f != null && f.ContentLength > 0)
                //{
                //    string folderPath = Server.MapPath("~/Images/");
                //    if (!Directory.Exists(folderPath))
                //        Directory.CreateDirectory(folderPath);

                //    string fileName = Path.GetFileName(f.FileName);
                //    string path = Path.Combine(folderPath, fileName);
                //    f.SaveAs(path);
                //    entity.Image = f.FileName; // Chỉ lưu tên file
                //}

                //entity.ProductDate = DateTime.Now;
                //entity.Status = true;
                //db.Products.Add(entity);
                //db.SaveChanges();
                //return View(entity);
                ModelState.Remove("Status");
                ModelState.Remove("FieldDate");
                ModelState.Remove("Image");

                if (ModelState.IsValid)
                {
                    var file = Request.Files["Image"];
                    if (file != null && file.ContentLength > 0)
                    {
                        // Tạo folder nếu chưa có
                        string folder = Server.MapPath("~/Images/field/");
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        string path = Path.Combine(folder, file.FileName);
                        file.SaveAs(path);
                        entity.Image = file.FileName;
                    }
                    entity.FieldDate = DateTime.Now;
                    entity.Status = true;
                    db.Fields.Add(entity);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {

            }
            var cat = db.Facilities.ToList();
            ViewBag.FacilityID = new SelectList(cat, "FacilityID", "FacilityName");
            return View();

        }
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var p = db.Fields.Find(id);
            ViewBag.FacilityID = new SelectList(db.Facilities, "FacilityID", "FacilityName", p.FacilityID);
            return View(p);
        }

        [HttpPost]
        public ActionResult Edit(Field p, HttpPostedFileBase upload)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.FacilityID = new SelectList(db.Facilities, "FacilityID", "FacilityName", p.FacilityID);
                return View(p);
            }

            if (upload != null && upload.ContentLength > 0)
            {
                string folderPath = Server.MapPath("~/Images/field/");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = Path.GetFileName(upload.FileName);
                string path = Path.Combine(folderPath, fileName);
                upload.SaveAs(path);
                p.Image = fileName;
            }
            db.Entry(p).State = System.Data.Entity.EntityState.Modified;
            //db.Entry(p).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var p = db.Fields.Find(id);
            db.Fields.Remove(p);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}