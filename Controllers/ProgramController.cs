using System.Linq;
using System.Web.Mvc;
using SRM.Models;
using SRM.Data;

namespace SRM.Controllers
{
    public class ProgramController : Controller
    {
        private readonly AppDbContext _db = new AppDbContext();

        // GET: Manage Programs page
        public ActionResult ManagePrograms()
        {
            var programs = _db.Programs.ToList();
            return View("~/Views/SystemSetup/ManagePrograms.cshtml", programs);
        }

        // POST: Add new program
        [HttpPost]
        public JsonResult Add(string programName)
        {
            if (string.IsNullOrWhiteSpace(programName))
                return Json(new { success = false, message = "Program name required" });

            // --- 1. DUPLICATION CHECK ---
            // We check if any program already exists with the same name (ignoring spaces and case)
            var normalizedName = programName.Trim().ToLower();
            bool exists = _db.Programs.Any(x => x.Program_Name.Trim().ToLower() == normalizedName);

            if (exists)
            {
                return Json(new { success = false, message = "This program name already exists." });
            }

            // --- 2. PROCEED WITH SAVE ---
            var program = new Program_Setup
            {
                Program_Name = programName.Trim(), // Good practice to trim before saving
            };

            _db.Programs.Add(program);
            _db.SaveChanges();

            return Json(new { success = true, program });
        }
        // POST: Delete program
        [HttpPost]
        public JsonResult Delete(int id)
        {
            var program = _db.Programs.Find(id);
            if (program == null)
                return Json(new { success = false, message = "Program not found" });

            _db.Programs.Remove(program);
            _db.SaveChanges();

            return Json(new { success = true });
        }

        // POST: Edit program
        [HttpPost]
        public JsonResult Edit(int id, string programName)
        {
            var program = _db.Programs.Find(id);
            if (program == null)
                return Json(new { success = false, message = "Program not found" });

            program.Program_Name = programName;
            _db.SaveChanges();

            return Json(new { success = true, program });
        }
    }
}
