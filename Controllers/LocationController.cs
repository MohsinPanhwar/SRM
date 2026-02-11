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
            var locations = _db.Locations.ToList();
            return View("~/Views/SystemSetup/ManageLocation.cshtml", locations);
        }

        // POST: Add / Edit location
        [HttpPost]
        public JsonResult SaveLocation(int sno = 0, string Location_Description = "")
        {
            if (string.IsNullOrWhiteSpace(Location_Description))
                return Json(new { success = false, message = "Work area name required" });

            // 1. DUPLICATION CHECK
            var normalizedName = Location_Description.Trim().ToLower();

            // Check if any other location has the same name, excluding the current 'sno'
            bool exists = _db.Locations.Any(x => x.Location_Description.Trim().ToLower() == normalizedName && x.sno != sno);

            if (exists)
            {
                return Json(new { success = false, message = "This Work Area already exists." });
            }

            // 2. SAVE LOGIC
            Location loc;
            if (sno == 0)
            {
                loc = new Location { Location_Description = Location_Description.Trim() };
                _db.Locations.Add(loc);
            }
            else
            {
                loc = _db.Locations.Find(sno);
                if (loc == null)
                    return Json(new { success = false, message = "Location not found" });

                loc.Location_Description = Location_Description.Trim();
            }

            _db.SaveChanges();
            return Json(new { success = true, location = loc });
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
    }
}
