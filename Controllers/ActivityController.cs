using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SRM.Data;
using SRM.Models;
using SRM.Models.ViewModels;

namespace SRM.Controllers
{
    public class ActivityController : Controller
    {
        private readonly AppDbContext _db = new AppDbContext();

        // GET: Activity/LogNewActivity
        public ActionResult LogNewActivity()
        {
            int? globalProgramId = Session["AgentProgramId"] as int?;

            // 1. Filter Categories based on the switcher/session
            ViewBag.Activities = _db.ActivityCategories
                .Where(x => !globalProgramId.HasValue || x.program_id == globalProgramId)
                .ToList();

            // 2. Locations remain unfiltered (Global)
            ViewBag.Locations = _db.IncidentLocations.ToList();

            return View("~/Views/ActivityManagement/LogNewActivity.cshtml");
        }

        // POST: Activity/LogNewActivity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogNewActivity(FormCollection form)
        {
            // 1. Get current Program context
            int? globalProgramId = Session["AgentProgramId"] as int?;

            try
            {
                // 2. Parse Dates using the helper
                DateTime startDate = ParseDateTime(form["StartDate"], int.Parse(form["StartHour"]), int.Parse(form["StartMinute"]), form["StartAmPm"]);
                DateTime endDate = ParseDateTime(form["EndDate"], int.Parse(form["EndHour"]), int.Parse(form["EndMinute"]), form["EndAmPm"]);

                // 3. Validation: Date logic
                if (endDate <= startDate)
                {
                    TempData["Error"] = "End date/time must be after start date/time.";
                    ReloadDropdowns(globalProgramId);
                    return View("~/Views/ActivityManagement/LogNewActivity.cshtml");
                }

                // 4. Handle Activity ID and Name Lookup (Fix for CS0019)
                string activityIdStr = form["ActivityId"];
                string categoryName = "";

                if (!string.IsNullOrEmpty(activityIdStr))
                {
                    // We find the category by comparing the string from the form 
                    // to the string 'cat_id' in the database
                    var category = _db.ActivityCategories
                                       .FirstOrDefault(c => c.cat_id == activityIdStr);

                    if (category != null)
                    {
                        categoryName = category.cat_name;
                    }
                }

                // 5. Build and Save to ActivityMaster
                var activity = new ActivityMaster
                {
                    // Parse string back to int? for the Activity foreign key column
                    Activity = string.IsNullOrEmpty(activityIdStr) ? (int?)null : int.Parse(activityIdStr),

                    Location = form["Location"],
                    ActivityDate = startDate,
                    ActivityEndDate = endDate,
                    ReportBy = Session["AgentPno"]?.ToString(),
                    ReportDate = DateTime.Now,
                    Detail = form["ActivityDetail"],
                    program_id = globalProgramId,
                    status = "O" // Setting default status to 'Open'

                    // Note: If you add 'ActivityName' to your model, uncomment below:
                    // ActivityName = categoryName 
                };

                _db.ActivityMasters.Add(activity);
                _db.SaveChanges();

                TempData["Success"] = "Activity logged successfully!";
                return RedirectToAction("ShowAllActivities");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
                ReloadDropdowns(globalProgramId);
                return View("~/Views/ActivityManagement/LogNewActivity.cshtml");
            }
        }

        public ActionResult ShowAllActivities()
        {
            int? globalProgramId = Session["AgentProgramId"] as int?;

            var activityList = (from am in _db.ActivityMasters
                                join cat in _db.ActivityCategories on am.Activity.ToString() equals cat.cat_id into catJoin
                                from cat in catJoin.DefaultIfEmpty()
                                join loc in _db.IncidentLocations on am.Location equals loc.LocationCode into locJoin
                                from loc in locJoin.DefaultIfEmpty()
                                where !globalProgramId.HasValue || am.program_id == globalProgramId
                                orderby am.ReportDate descending
                                select new ActivityVM // <--- Use the ViewModel here
                                {
                                    sno = am.sno,
                                    ActivityName = cat != null ? cat.cat_name : "N/A",
                                    LocationName = loc != null ? loc.LocationName : am.Location,
                                    ActivityDate = am.ActivityDate,
                                    ReportBy = am.ReportBy,
                                    ReportDate = am.ReportDate,
                                    status = am.status
                                }).ToList();

            return View("~/Views/ActivityManagement/ShowAllActivities.cshtml", activityList);
        }
        public ActionResult ViewAllCategories()
        {
            // Get current Program context
            int? globalProgramId = Session["AgentProgramId"] as int?;

            // Fetch and filter by program_id
            var categories = _db.ActivityCategories
                .Where(c => !globalProgramId.HasValue || c.program_id == globalProgramId)
                .OrderBy(c => c.cat_id)
                .ToList();

            return View("~/Views/ActivityManagement/ViewAllCategories.cshtml", categories);
        }
        // POST: Activity/AddCategory
        [HttpPost]
        public JsonResult AddCategory(string catName)
        {
            try
            {
                int? programId = Session["AgentProgramId"] as int?;
                // Generate a simple unique 3-char ID logic or use a sequence
                var newId = (_db.ActivityCategories.Count() + 1).ToString().PadLeft(3, '0');

                var category = new ActivityCategories
                {
                    cat_id = newId,
                    cat_name = catName,
                    program_id = programId
                };
                _db.ActivityCategories.Add(category);
                _db.SaveChanges();
                return Json(new { success = true, category });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Activity/EditCategory
        [HttpPost]
        public JsonResult EditCategory(string id, string catName)
        {
            var cat = _db.ActivityCategories.Find(id);
            if (cat != null)
            {
                cat.cat_name = catName;
                _db.SaveChanges();
                return Json(new { success = true, category = cat });
            }
            return Json(new { success = false, message = "Category not found" });
        }

        // POST: Activity/DeleteCategory
        [HttpPost]
        public JsonResult DeleteCategory(string id)
        {
            var cat = _db.ActivityCategories.Find(id);
            if (cat != null)
            {
                _db.ActivityCategories.Remove(cat);
                _db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Category not found" });
        }
        // --- Helpers ---

        private void ReloadDropdowns(int? programId)
        {
            ViewBag.Activities = _db.ActivityCategories
                .Where(x => !programId.HasValue || x.program_id == programId)
                .ToList();
            ViewBag.Locations = _db.IncidentLocations.ToList();
        }

        public ActionResult ViewAllLocations()
        {
            // Fetch all locations from the IncidentLocation table
            var locations = _db.IncidentLocations
                               .OrderBy(l => l.LocationCode)
                               .ToList();

            return View("~/Views/ActivityManagement/ViewAllLocations.cshtml", locations);
        }
        // POST: Activity/AddLocation
        [HttpPost]
        public JsonResult AddLocation(string locCode, string locName)
        {
            try
            {
                // Validate code length based on your model [StringLength(3)]
                if (string.IsNullOrEmpty(locCode) || locCode.Length > 3)
                {
                    return Json(new { success = false, message = "Code must be 1-3 characters." });
                }

                // Check if code already exists
                if (_db.IncidentLocations.Any(x => x.LocationCode == locCode))
                {
                    return Json(new { success = false, message = "This Location Code already exists." });
                }

                var location = new IncidentLocation
                {
                    LocationCode = locCode.ToUpper(), // Standardize to uppercase
                    LocationName = locName
                };

                _db.IncidentLocations.Add(location);
                _db.SaveChanges();
                return Json(new { success = true, location });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Activity/EditLocation
        [HttpPost]
        public JsonResult EditLocation(string id, string locName)
        {
            var loc = _db.IncidentLocations.Find(id);
            if (loc != null)
            {
                loc.LocationName = locName;
                _db.SaveChanges();
                return Json(new { success = true, location = loc });
            }
            return Json(new { success = false, message = "Location not found" });
        }

        // POST: Activity/DeleteLocation
        [HttpPost]
        public JsonResult DeleteLocation(string id)
        {
            var loc = _db.IncidentLocations.Find(id);
            if (loc != null)
            {
                _db.IncidentLocations.Remove(loc);
                _db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Location not found" });
        }
        private DateTime ParseDateTime(string dateStr, int hour, int minute, string amPm)
        {
            if (amPm == "PM" && hour != 12) hour += 12;
            else if (amPm == "AM" && hour == 12) hour = 0;

            DateTime date = DateTime.Parse(dateStr);
            return new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
        }
        public ActionResult Details(int id)
        {
            var activity = (from am in _db.ActivityMasters
                            join cat in _db.ActivityCategories on am.Activity.ToString() equals cat.cat_id into catJoin
                            from cat in catJoin.DefaultIfEmpty()
                            join loc in _db.IncidentLocations on am.Location equals loc.LocationCode into locJoin
                            from loc in locJoin.DefaultIfEmpty()
                            where am.sno == id
                            select new ActivityVM // Ensure this class name matches your ViewModel
                            {
                                sno = am.sno,
                                ActivityName = cat != null ? cat.cat_name : "N/A",
                                LocationName = loc != null ? loc.LocationName : am.Location,
                                ActivityDate = am.ActivityDate,
                                ActivityEndDate = am.ActivityEndDate,
                                ReportDate = am.ReportDate,
                                ReportBy = am.ReportBy,
                                Detail = am.Detail,
                                workLog = am.workLog,
                                ResolutionDetail = am.ResolutionDetail,
                                status = am.status == "C" ? "Closed" : "Open"
                            }).FirstOrDefault();

            // FIX: If no record is found, don't go to the View; show an error or redirect
            if (activity == null)
            {
                TempData["Error"] = "Activity record not found.";
                return RedirectToAction("ShowAllActivities");
            }

            return View("~/Views/ActivityManagement/ActivityDetails.cshtml", activity);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateActivity(ActivityVM model)
        {
            var activity = _db.ActivityMasters.FirstOrDefault(x => x.sno == model.sno);

            if (activity != null)
            {
                string pno = Session["AgentPno"]?.ToString() ?? "00000";
                string agentName = Session["AgentName"]?.ToString() ?? "Unknown";
                string timestamp = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
                string newWorkLogEntries = "";

                // 1. Handle standard Resolution Detail entry
                if (!string.IsNullOrWhiteSpace(model.ResolutionDetail))
                {
                    newWorkLogEntries += $"[{timestamp} | {agentName} ({pno})]" + Environment.NewLine +
                                         model.ResolutionDetail.Trim() + Environment.NewLine +
                                         "--------------------------------------------------" + Environment.NewLine + Environment.NewLine;
                }

                // 2. Logic: If status is being changed to Closed, add the specific header
                if (model.status == "Closed" && activity.status != "C")
                {
                    newWorkLogEntries += $"[{timestamp} | {agentName} ({pno})]" + Environment.NewLine +
                                         "*** ACTIVITY CLOSED ***" + Environment.NewLine +
                                         "--------------------------------------------------";

                    activity.status = "C";
                    activity.ClosedDate = DateTime.Now;
                    activity.ClosedBy = pno;
                }
                else
                {
                    activity.status = (model.status == "Closed") ? "C" : "O";
                }

                // 3. Append all new entries to existing WorkLog
                if (!string.IsNullOrEmpty(newWorkLogEntries))
                {
                    string existingLog = (activity.workLog ?? "").Trim();
                    activity.workLog = string.IsNullOrEmpty(existingLog)
                        ? newWorkLogEntries.Trim()
                        : existingLog + Environment.NewLine + Environment.NewLine + newWorkLogEntries.Trim();
                }

                _db.SaveChanges();
                TempData["Success"] = "Activity updated successfully!";
            }

            return Redirect(Url.Action("Details", new { id = model.sno }) + "#worklog-section");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReopenActivity(ActivityVM model)
        {
            var activity = _db.ActivityMasters.FirstOrDefault(x => x.sno == model.sno);
            if (activity != null)
            {
                string pno = Session["AgentPno"]?.ToString() ?? "00000";
                string agentName = Session["AgentName"]?.ToString() ?? "Unknown";
                string timestamp = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");

                activity.status = "O"; // Set back to Open
                activity.ClosedDate = null;
                activity.ClosedBy = null;

                string entry = $"[{timestamp} | {agentName} ({pno})]" + Environment.NewLine +
                               "*** ACTIVITY RE-OPENED ***" + Environment.NewLine +
                               "--------------------------------------------------";

                activity.workLog = (activity.workLog ?? "").Trim() + Environment.NewLine + Environment.NewLine + entry;

                _db.SaveChanges();
                TempData["Success"] = "Activity re-opened successfully.";
            }
            return Redirect(Url.Action("Details", new { id = model.sno }) + "#worklog-section");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
