using SRM.Data;
using SRM.Models;
using SRM.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace SRM.Controllers
{
    public class IncidentController : BaseController
    {
        private readonly AppDbContext _db = new AppDbContext();

        // GET: Incident/LogNewIncident
        public ActionResult LogNewIncident()
        {
            var pno = Session["AgentPno"] as string;
            if (string.IsNullOrEmpty(pno))
                return RedirectToAction("Login", "Account");

            int? globalProgramId = Session["AgentProgramId"] as int?;

            // 1. Programs: 
            // If the agent belongs to a program, they only see that one. 
            // If they are a Super Admin (null), they see all programs.
            var filteredPrograms = _db.Programs
                .Where(p => !globalProgramId.HasValue || p.Program_Id == globalProgramId)
                .OrderBy(p => p.Program_Name)
                .ToList();

            ViewBag.Programs = new SelectList(filteredPrograms, "Program_Id", "Program_Name");

            // 2. Groups (Incident Categories):
            // Initially filter by the session Program. 
            // These will repopulate via AJAX if the program dropdown changes.
            ViewBag.IncidentCategories = _db.IncidentCategories
                    .Where(x => !globalProgramId.HasValue || x.program_id == globalProgramId)
                    .OrderBy(x => x.cat_name)
                    .ToList();

            ViewBag.IncidentGroups = _db.groups
                    .Where(x => !globalProgramId.HasValue || x.program_id == globalProgramId)
                    .OrderBy(x => x.gname)
                    .Select(x => new SelectListItem
                    {
                        Value = x.gid.ToString(),
                        Text = x.gname
                    })
                    .ToList();

            // 3. Locations (Global)
            ViewBag.Locations = _db.IncidentLocations
                .OrderBy(x => x.LocationName)
                .ToList();

            // 4. Set a label for the UI
            ViewBag.CurrentProgramName = globalProgramId.HasValue
                ? _db.Programs.FirstOrDefault(p => p.Program_Id == globalProgramId)?.Program_Name
                : "All Programs";

            return View("~/Views/IncidentManagement/LogNewIncident.cshtml");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogNewIncident(FormCollection form)
        {
            int? globalProgramId = Session["AgentProgramId"] as int?;
            try
            {
                // 1. Parse Dates
                DateTime incDate = ParseDateTime(form["IncidentDate"], int.Parse(form["IncHour"]), int.Parse(form["IncMinute"]), form["IncAmPm"]);

                // 2. Build and Save IncidentMaster
                var incident = new IncidentMaster
                {
                    Incident = string.IsNullOrEmpty(form["IncidentId"]) ? (int?)null : int.Parse(form["IncidentId"]),
                    Location = form["Location"],
                    IncidentDate = incDate,
                    ReportBy = Session["AgentPno"]?.ToString(),
                    ReportDate = DateTime.Now,
                    inctype = form["IncidentType"],
                    Detail = form["IncidentDetail"],
                    program_id = globalProgramId,
                    status = "O",
                    gid = string.IsNullOrEmpty(form["gid"]) ? (int?)null : int.Parse(form["gid"])
                };

                _db.IncidentMasters.Add(incident);
                _db.SaveChanges();

                // 3. Handle Service Request
                if (form["CreateServiceRequest"]?.Contains("true") == true)
                {
                    var newRequest = new Request_Master
                    {
                        RequestDate = DateTime.Now,
                        RequestLogBy = Session["AgentPno"]?.ToString() ?? "ADMIN",
                        program_id = globalProgramId,
                        Priority = MapPriority(form["Priority"]),
                        ReqSummary = form["Summary"],
                        ReqDetails = form["Details"],
                        Location = form["Location"],
                        status = "Q",
                        RequestedIPAddress = Request.UserHostAddress
                    };
                    _db.Request_Master.Add(newRequest);
                    _db.SaveChanges();

                    incident.ServiceRequestID = newRequest.RequestID;
                    _db.SaveChanges();
                }

                TempData["Success"] = "Incident logged successfully!";
                return RedirectToAction("ShowAllIncident");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                ReloadDropdowns(globalProgramId);
                return View("~/Views/IncidentManagement/LogNewIncident.cshtml");
            }
        }

        public ActionResult ShowAllIncident()
        {
            int? globalProgramId = Session["AgentProgramId"] as int?;

            // Fixed the Join: Added .ToString() to ensure types match (Int vs String)
            var incidentList = (from im in _db.IncidentMasters
                                join cat in _db.IncidentCategories on im.Incident.ToString() equals cat.cat_id.ToString() into catJoin
                                from cat in catJoin.DefaultIfEmpty()
                                join loc in _db.IncidentLocations on im.Location equals loc.LocationCode into locJoin
                                from loc in locJoin.DefaultIfEmpty()
                                where !globalProgramId.HasValue || im.program_id == globalProgramId
                                orderby im.ReportDate descending
                                select new IncidentVM
                                {
                                    IncidentID = im.sno,
                                    sno = im.sno,
                                    CategoryName = cat != null ? cat.cat_name : "N/A",
                                    LocationName = loc != null ? loc.LocationName : im.Location,
                                    IncidentDate = im.IncidentDate,
                                    ReportBy = im.ReportBy,
                                    ReportDate = im.ReportDate,
                                    status = im.status
                                }).ToList();

            return View("~/Views/IncidentManagement/ShowAllIncident.cshtml", incidentList);
        }

        // --- HELPERS (These solve the 'name does not exist' errors) ---

        private void ReloadDropdowns(int? programId)
        {
            ViewBag.IncidentCategories = _db.IncidentCategories
                .Where(x => !programId.HasValue || x.program_id == programId)
                .ToList();
            ViewBag.Locations = _db.IncidentLocations.ToList();
            // ADD THIS
            ViewBag.Groups = _db.groups.ToList();
        }

        private DateTime ParseDateTime(string dateStr, int hour, int minute, string amPm)
        {
            if (amPm == "PM" && hour != 12) hour += 12;
            else if (amPm == "AM" && hour == 12) hour = 0;
            DateTime date = DateTime.Parse(dateStr);
            return new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
        }

        private int MapPriority(string p)
        {
            switch (p)
            {
                case "Critical": return 1;
                case "Urgent": return 2;
                case "Important": return 3;
                default: return 4;
            }
        }
        public ActionResult ManageIncidentCategories()
        {
            int? globalProgramId = Session["AgentProgramId"] as int?;

            var categories = _db.IncidentCategories
                .Where(c => !globalProgramId.HasValue || c.program_id == globalProgramId)
                .OrderBy(c => c.cat_name)
                .ToList();

            return View("~/Views/IncidentManagement/ManageIncidentCategories.cshtml", categories);
        }

        [HttpPost]
        public JsonResult AddCategory(string catName)
        {
            try
            {
                int? programId = Session["AgentProgramId"] as int?;

                var category = new IncidentCategories
                {
                    cat_name = catName,
                    program_id = programId ?? 1 // Default to 1 if no session
                };

                _db.IncidentCategories.Add(category);
                _db.SaveChanges();
                return Json(new { success = true, category });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EditCategory(int id, string catName)
        {
            var cat = _db.IncidentCategories.Find(id);
            if (cat != null)
            {
                cat.cat_name = catName;
                _db.SaveChanges();
                return Json(new { success = true, category = cat });
            }
            return Json(new { success = false, message = "Category not found" });
        }

        [HttpPost]
        public JsonResult DeleteCategory(int id)
        {
            var cat = _db.IncidentCategories.Find(id);
            if (cat != null)
            {
                _db.IncidentCategories.Remove(cat);
                _db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Category not found" });
        }

        public ActionResult Details(int id)
        {
            var incident = (from im in _db.IncidentMasters
                            join cat in _db.IncidentCategories on im.Incident.ToString() equals cat.cat_id.ToString() into catJoin
                            from cat in catJoin.DefaultIfEmpty()
                            join loc in _db.IncidentLocations on im.Location equals loc.LocationCode into locJoin
                            from loc in locJoin.DefaultIfEmpty()
                            join grp in _db.groups on im.gid equals grp.gid into grpJoin
                            from grp in grpJoin.DefaultIfEmpty()
                            where im.sno == id
                            select new IncidentVM
                            {
                                sno = im.sno,
                                CategoryName = cat != null ? cat.cat_name : "N/A",
                                LocationName = loc != null ? loc.LocationName : im.Location,
                                IncidentDate = im.IncidentDate,
                                ReportDate = im.ReportDate,
                                ReportBy = im.ReportBy,
                                Detail = im.Detail,
                                workLog = im.workLog,
                                ResolutionDetail = im.ResolutionDetail,
                                // Use a simple ternary or let the VM handle the "Closed" logic
                                status = im.status,
                                ServiceRequestID = im.ServiceRequestID,
                                // Ensure gname and manager_pno are strings
                                GroupName = grp != null ? grp.gname : "N/A",
                                ManagerName = grp != null ? grp.manager_pno : "Not Assigned"
                            }).FirstOrDefault();

            if (incident == null)
            {
                TempData["Error"] = "Incident record not found.";
                return RedirectToAction("ShowAllIncident");
            }

            // Map status string to readable text if needed before sending to View
            incident.status = incident.status == "C" ? "Closed" : "Open";

            return View("~/Views/IncidentManagement/IncidentDetails.cshtml", incident);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateIncident(IncidentVM model)
        {
            var incident = _db.IncidentMasters.FirstOrDefault(x => x.sno == model.sno);

            if (incident != null)
            {
                string pno = Session["AgentPno"]?.ToString() ?? "00000";
                string agentName = Session["AgentName"]?.ToString() ?? "Unknown";
                string timestamp = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
                string newWorkLogEntries = "";

                if (!string.IsNullOrWhiteSpace(model.ResolutionDetail))
                {
                    newWorkLogEntries += $"[{timestamp} | {agentName} ({pno})]" + Environment.NewLine +
                                         model.ResolutionDetail.Trim() + Environment.NewLine +
                                         "--------------------------------------------------" + Environment.NewLine + Environment.NewLine;
                }

                if (model.status == "Closed" && incident.status != "C")
                {
                    newWorkLogEntries += $"[{timestamp} | {agentName} ({pno})]" + Environment.NewLine +
                                         "*** INCIDENT CLOSED ***" + Environment.NewLine +
                                         "--------------------------------------------------";
                    incident.status = "C";
                }
                else
                {
                    incident.status = (model.status == "Closed") ? "C" : "O";
                }

                if (!string.IsNullOrEmpty(newWorkLogEntries))
                {
                    string existingLog = (incident.workLog ?? "").Trim();
                    incident.workLog = string.IsNullOrEmpty(existingLog)
                        ? newWorkLogEntries.Trim()
                        : existingLog + Environment.NewLine + Environment.NewLine + newWorkLogEntries.Trim();
                }
                                _db.SaveChanges();
                TempData["Success"] = "Incident updated successfully!";
            }

            return Redirect(Url.Action("Details", new { id = model.sno }) + "#worklog-section");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReopenIncident(IncidentVM model)
        {
            var incident = _db.IncidentMasters.FirstOrDefault(x => x.sno == model.sno);
            if (incident != null)
            {
                string pno = Session["AgentPno"]?.ToString() ?? "00000";
                string agentName = Session["AgentName"]?.ToString() ?? "Unknown";
                string timestamp = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");

                incident.status = "O";
                string entry = $"[{timestamp} | {agentName} ({pno})]" + Environment.NewLine +
                               "*** INCIDENT RE-OPENED ***" + Environment.NewLine +
                               "--------------------------------------------------";

                incident.workLog = (incident.workLog ?? "").Trim() + Environment.NewLine + Environment.NewLine + entry;

                _db.SaveChanges();
                TempData["Success"] = "Incident re-opened successfully.";
            }
            return Redirect(Url.Action("Details", new { id = model.sno }) + "#worklog-section");
        }
        public ActionResult SearchIncident(string incidentId)
        {
            using (var db = new AppDbContext())
            {
                // 1. Only count 'Closed' (C) incidents for the dropdown
                var incidentGroups = db.IncidentMasters
                    .Where(i => i.status == "C")
                    .GroupBy(i => i.Incident)
                    .Select(g => new { ID = g.Key, Count = g.Count() })
                    .ToList();

                var categories = db.IncidentCategories.ToList();

                ViewBag.IncidentGroups = incidentGroups.Select(g => new SelectListItem
                {
                    Text = (categories.FirstOrDefault(c => c.cat_id == g.ID)?.cat_name ?? "Unknown") + " ---- (" + g.Count + ")",
                    Value = g.ID.ToString()
                }).ToList();

                // 2. Filter Table results: Only show Closed incidents
                var query = db.IncidentMasters.Where(i => i.status == "C").AsQueryable();

                if (!string.IsNullOrEmpty(incidentId) && int.TryParse(incidentId, out int idValue))
                {
                    query = query.Where(i => i.Incident == idValue);
                }

                var results = query.OrderByDescending(i => i.IncidentDate).ToList();
                return View("~/Views/IncidentManagement/SearchIncident.cshtml", results);
            }
        }

       

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}