using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Areas.Admin.Controllers
{
    [Authorize]
    public class PriceController : Controller
    {
        // GET: Admin/Price
        Quan_ly_thue_san_the_thao_webncEntities db = new Quan_ly_thue_san_the_thao_webncEntities();

        public ActionResult Index()
        {
            return View(db.Prices.ToList());
        }

        public ActionResult Create()
        {
            var timeslots = db.TimeSlots
                .AsEnumerable()
                .Select(t => new
                {
                    t.TimeSlotID,
                    TimeDisplay = t.StartTime.HasValue && t.EndTime.HasValue
                        ? t.StartTime.Value.Hours.ToString("D2") + ":" + t.StartTime.Value.Minutes.ToString("D2")
                          + " - " +
                          t.EndTime.Value.Hours.ToString("D2") + ":" + t.EndTime.Value.Minutes.ToString("D2")
                        : "N/A"
                })
                .ToList();

            ViewBag.TimeslotId = new SelectList(timeslots, "TimeSlotID", "TimeDisplay");
            ViewBag.FieldId = new SelectList(db.Fields.Where(f => f.Status == true).ToList(), "FieldID", "FieldName");

            return View();
        }

        [HttpPost]
        public ActionResult Create(Price c)
        {
            if (ModelState.IsValid)
            {
                db.Prices.Add(c);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(c);
        }

        public ActionResult Edit(int id)
        {
            var c = db.Prices.Find(id);
            var timeslots = db.TimeSlots
                .AsEnumerable()
                .Select(t => new
                {
                    t.TimeSlotID,
                    TimeDisplay = t.StartTime.HasValue && t.EndTime.HasValue
                        ? t.StartTime.Value.Hours.ToString("D2") + ":" + t.StartTime.Value.Minutes.ToString("D2")
                          + " - " +
                          t.EndTime.Value.Hours.ToString("D2") + ":" + t.EndTime.Value.Minutes.ToString("D2")
                        : "N/A"
                })
                .ToList();

            ViewBag.TimeslotId = new SelectList(timeslots, "TimeSlotID", "TimeDisplay");
            ViewBag.FieldId = new SelectList(db.Fields.Where(f => f.Status == true).ToList(), "FieldID", "FieldName", c.FieldID);
            return View(c);
        }

        [HttpPost]
        public ActionResult Edit(Price c)
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
            var c = db.Prices.Find(id);
            db.Prices.Remove(c);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}