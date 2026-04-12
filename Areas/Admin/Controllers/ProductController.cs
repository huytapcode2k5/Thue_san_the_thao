using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Areas.Admin.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();


        public ActionResult Index(string search, int? categoryId)
        {
            var products = db.Products.Include("Category").AsQueryable();

            // lọc theo tên
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.Name.Contains(search));
            }

            // lọc theo danh mục
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId);
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "Name");

            return View(products.ToList());
        }
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Models.Data.Product entity)
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
                if (ModelState.IsValid)
                {
                    var file = Request.Files["Image"];
                    if(file != null && file.ContentLength > 0)
                    {
                        string path = Server.MapPath("~/Images/"+ file.FileName);
                        file.SaveAs(path);
                        entity.Image = file.FileName;

                    }
                    entity.ProductDate = DateTime.Now;
                    db.Products.Add(entity);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch(Exception ex)
            {
               
            }
            var cat = db.Categories.ToList();
            ViewBag.Category = new SelectList(cat, "CategoryID", "Name");
            return View();

        }
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var p = db.Products.Find(id);
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "Name", p.CategoryID);
            return View(p);
        }

        [HttpPost]
        public ActionResult Edit(Product p, HttpPostedFileBase upload)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "Name", p.CategoryID);
                return View(p);
            }

            if (upload != null && upload.ContentLength > 0)
            {
                string folderPath = Server.MapPath("~/Images/");
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
            var p = db.Products.Find(id);
            db.Products.Remove(p);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}