using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SRM.Data;
using SRM.Models;

namespace SRM.Controllers
{
    public class ViewAllRequestsController : Controller
    {
        private AppDbContext _db = new AppDbContext();

        // GET: ViewAllRequests
        public async Task<ActionResult> ViewAllRequests(string searchBy, string searchText, DateTime? fromDate, DateTime? toDate, string statusFilter)
        {
            // 1. Initialize ViewModel with Defaults
            var vm = new AllRequestsViewModel
            {
                // Default to last 365 days if no date is picked
                FromDate = fromDate ?? DateTime.Now.AddDays(-365),
                // Default to today (+1 day buffer to include all of today's timestamps)
                ToDate = toDate ?? DateTime.Now.Date,
                StatusFilter = statusFilter ?? "All",
                SearchText = searchText
            };

            var query = _db.Request_Master.AsQueryable();

            // 2. Apply Date Filtering Logic
            // We use the start of the FromDate and the very end of the ToDate
            DateTime startDate = vm.FromDate.Date;
            DateTime endDate = vm.ToDate.Date.AddDays(1).AddTicks(-1);

            query = query.Where(r => r.RequestDate >= startDate && r.RequestDate <= endDate);

            // 3. Status Filtering
            if (vm.StatusFilter != "All")
            {
                query = query.Where(r => r.status == vm.StatusFilter);
            }

            // 4. Text Search Logic
            if (!string.IsNullOrEmpty(searchText))
            {
                if (searchBy == "RequestID")
                {
                    if (int.TryParse(searchText, out int id))
                    {
                        query = query.Where(r => r.RequestID == id);
                    }
                }
                else
                {
                    query = query.Where(r => r.ReqSummary.Contains(searchText));
                }
            }

            // 5. Execute search and assign to the list
            vm.RequestList = query.OrderByDescending(r => r.RequestID).ToList();

            // 6. Summary Statistics (Calculated for the sidebar)
            vm.QueueCount = _db.Request_Master.Count(r => r.status == "Q");
            vm.ForwardedCount = _db.Request_Master.Count(r => r.status == "F");
            vm.ResolvedCount = _db.Request_Master.Count(r => r.status == "R");
            vm.ClosedCount = _db.Request_Master.Count(r => r.status == "C");

            // TotalCount is NOT assigned here to avoid the CS0200 error. 
            // Your Model already handles this or calculates it automatically.

            return View("~/Views/ServiceRequest/ViewAllRequests.cshtml", vm);
        }

        public ActionResult Details(int id)
        {
            var request = _db.Request_Master.FirstOrDefault(r => r.RequestID == id);
            if (request == null) return HttpNotFound();

            // Populate dropdowns for the "Forwarding" section
            ViewBag.EmployeeList = new SelectList(_db.agent.ToList(), "Pno", "Name");
            ViewBag.GroupList = new SelectList(_db.groups.ToList(), "gid", "gname");

            return View("~/Views/ServiceRequest/Details.cshtml", request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForwardRequest(int id, string Forward_To, string Forward_To_Type, string ForwardedRemarks)
        {
            var req = _db.Request_Master.Find(id);
            if (req == null) return HttpNotFound();

            string senderPno = "ADMIN";
            string targetName = Forward_To;

            if (Forward_To_Type == "I")
            {
                var targetEmp = _db.agent.FirstOrDefault(e => e.Pno == Forward_To);
                targetName = targetEmp != null ? targetEmp.Name : Forward_To;
            }
            else if (Forward_To_Type == "G")
            {
                if (int.TryParse(Forward_To, out int gid))
                {
                    var targetGroup = _db.groups.FirstOrDefault(g => g.gid == gid);
                    targetName = targetGroup != null ? targetGroup.gname : "Group " + Forward_To;
                }
            }

            string timestamp = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");
            string newEntry = $"<div class='log-entry'>{timestamp} forward to {targetName} by {senderPno}</div>";

            req.Forward_By = senderPno;
            req.Forward_To = Forward_To;
            req.Forward_To_Type = Forward_To_Type;
            req.ForwardedDate = DateTime.Now;
            req.ForwardedRemarks = ForwardedRemarks;
            req.status = "F";

            req.ReqDetails = (req.ReqDetails ?? "") + newEntry;

            await _db.SaveChangesAsync();
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Added for security since it's a POST
        public async Task<ActionResult> TakeOwnership(int id)
        {
            var req = _db.Request_Master.Find(id);
            if (req == null) return HttpNotFound();

            // Replace this with your session-based user or actual login name
            string currentUser = "SAJJAD ALI SHAH";
            string timestamp = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

            // Create the log entry to show in the Communication Log box
            string newLogEntry = $@"<div class='log-entry' style='border-left: 4px solid #2e7d57; background: #f0fff4; padding: 10px; margin-top: 10px;'>
                                <strong>{timestamp} - OWNERSHIP TAKEN</strong> by {currentUser}
                             </div>";

            // Update the database fields
            req.status = "F"; // Typically 'F' (Forwarded/In Progress) or 'O' (Owned)
            req.Forward_To = currentUser;
            req.Forward_To_Type = "I";
            req.ForwardedDate = DateTime.Now;

            // Append to the HTML communication log
            req.ReqDetails = (req.ReqDetails ?? "") + newLogEntry;

            await _db.SaveChangesAsync();

            // Redirect back to see the updated log and status
            return RedirectToAction("Details", new { id = id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CloseRequest(int id, string ClosingRemarks)
        {
            var req = _db.Request_Master.Find(id);
            if (req == null) return HttpNotFound();

            // Replace with actual user context if available (e.g., User.Identity.Name)
            string currentUser = "ADMIN";
            string timestamp = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

            // Format the closing entry for the Communication Log
            string closeEntry = $@"<div class='log-entry' style='border-left: 4px solid #d9534f; background: #fff5f5; padding: 10px; margin-top: 10px;'>
                            <strong>{timestamp} - REQUEST CLOSED</strong> by {currentUser}<br/>
                            <strong>Resolution:</strong> {ClosingRemarks}
                          </div>";

            // Update DB fields
            req.status = "C";
            req.ReqDetails = (req.ReqDetails ?? "") + closeEntry;

            // If your table has specific closing columns, update them here:
            // req.ClosedDate = DateTime.Now;

            await _db.SaveChangesAsync();

            // Redirect back to details to see the closed status
            return RedirectToAction("Details", new { id = id });
        }
    }

}