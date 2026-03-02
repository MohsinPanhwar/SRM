using SRM.Data;
using SRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SRM.Controllers
{
    public class CategoryController : BaseController
    {
        private AppDbContext db = new AppDbContext();

        // GET
        public ActionResult ViewCategory()
        {
            var list = db.invCategory.ToList();
            return View("~/Views/AssetIssuance/ViewCategory.cshtml",list);
        }

        // SAVE
        [HttpPost]
        public JsonResult Save(Category model)
        {
            if (string.IsNullOrWhiteSpace(model.CategoryName))
                return Json(new { success = false });
            bool exists = db.invCategory.Any(x => x.CategoryName.Trim().ToLower() == model.CategoryName.Trim().ToLower()
                                       && x.CategoryID != model.CategoryID);

            if (exists)
            {
                return Json(new { success = false, message = "This category already exists." });
            }
            if (model.CategoryID == 0)
            {
                db.invCategory.Add(model);
            }
            else
            {
                var data = db.invCategory.Find(model.CategoryID);
                data.CategoryName = model.CategoryName;
            }

            db.SaveChanges();
            return Json(new { success = true });
        }

        // DELETE
        [HttpPost]
        public JsonResult Delete(int id)
        {
            var data = db.invCategory.Find(id);
            db.invCategory.Remove(data);
            db.SaveChanges();

            return Json(new { success = true });
        }
    }
}