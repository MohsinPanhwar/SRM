using SRM.Data;
using SRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SRM.Controllers
{
    public class InventoryBrandController : Controller
    {
        private AppDbContext db = new AppDbContext();

        // GET
        public ActionResult ViewBrand()
        {
            return View("~/Views/AssetIssuance/ViewBrand.cshtml",db.invBrand.ToList());
        }

        // SAVE (Add + Update)
        [HttpPost]
        public JsonResult Save(Brand model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Brand name required" });

            bool exists = db.invBrand.Any(x => x.Name.Trim().ToLower() == model.Name.Trim().ToLower()
                                       && x.ID != model.ID);

            if (exists)
            {
                return Json(new { success = false, message = "This Brand already exists." });
            }

            if (model.ID == 0)
            {
                db.invBrand.Add(model);
            }
            else
            {
                var data = db.invBrand.Find(model.ID);
                data.Name = model.Name;
            }

            db.SaveChanges();
            return Json(new { success = true });
        }

        // DELETE
        [HttpPost]
        public JsonResult Delete(int id)
        {
            var data = db.invBrand.Find(id);
            db.invBrand.Remove(data);
            db.SaveChanges();

            return Json(new { success = true });
        }
    }
}