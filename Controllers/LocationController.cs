using System.Linq;
using System.Web.Mvc;
using SRM.Models;
using SRM.Data;

namespace SRM.Controllers
{
    public class LocationController : Controller
    {
        private readonly AppDbContext _db = new AppDbContext();

        // GET: Manage Locations page
        public ActionResult ManageLocation()
        {
            // 1. Retrieve the Global Filter from Session
            int? globalProgramId = Session["AgentProgramId"] as int?;

            // 2. Filter locations
            var locations = _db.Locations
                .Where(l => !globalProgramId.HasValue || l.Program_id == globalProgramId)
                .ToList();

            // 3. Populate Program Dropdown for the "Add New" section
            // Use the global filter to pre-select or limit the list if desired
            ViewBag.ProgramList = new SelectList(_db.Programs.OrderBy(p => p.Program_Name), "Program_Id", "Program_Name", globalProgramId);

            return View("~/Views/SystemSetup/ManageLocation.cshtml", locations);
        }

        [HttpPost]
        public JsonResult SaveLocation(int sno = 0, string Location_Description = "")
        {
            if (string.IsNullOrWhiteSpace(Location_Description))
                return Json(new { success = false, message = "Work area name required" });

            // 1. Get the ID directly from the Switcher's Session
            int? globalProgramId = Session["AgentProgramId"] as int?;

            if (!globalProgramId.HasValue)
            {
                return Json(new { success = false, message = "Please select a specific program in the top switcher first." });
            }

            var trimmedValue = Location_Description.Trim();
            var normalizedName = trimmedValue.ToLower();

            // 2. DUPLICATION CHECK (Within the active switcher context)
            bool exists = _db.Locations.Any(x =>
                x.Location_Description.Trim().ToLower() == normalizedName &&
                x.sno != sno &&
                x.Program_id == globalProgramId);

            if (exists)
                return Json(new { success = false, message = "This Work Area already exists in the currently selected program." });

            // 3. SAVE LOGIC
            Location loc;
            if (sno == 0)
            {
                // NEW RECORD
                loc = new Location
                {
                    Location_Description = trimmedValue,
                    Location_ID = trimmedValue, // Set same as description
                    Program_id = globalProgramId
                };
                _db.Locations.Add(loc);
            }
            else
            {
                // EDIT RECORD
                loc = _db.Locations.Find(sno);
                if (loc == null) return Json(new { success = false, message = "Location not found" });

                loc.Location_Description = trimmedValue;
                loc.Location_ID = trimmedValue; // Update ID column to match new description
            }

            _db.SaveChanges();

            // Return the object so the UI can update the row text immediately
            return Json(new
            {
                success = true,
                location = new
                {
                    sno = loc.sno,
                    Location_Description = loc.Location_Description
                }
            });
        }
        // POST: Delete location
        [HttpPost]
        public JsonResult DeleteLocation(int id)
        {
            var loc = _db.Locations.Find(id);
            if (loc == null)
                return Json(new { success = false, message = "Location not found" });

            _db.Locations.Remove(loc);
            _db.SaveChanges();
            return Json(new { success = true });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}