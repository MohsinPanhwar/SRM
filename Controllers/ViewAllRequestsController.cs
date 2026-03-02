using SRM.Data;
using SRM.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SRM.Controllers
{
    public class ViewAllRequestsController : BaseController
    {
        private readonly AppDbContext _db = new AppDbContext();

        // GET: ViewAllRequests
        public async Task<ActionResult> ViewAllRequests(string searchBy, string searchText, DateTime? fromDate, DateTime? toDate, string statusFilter)
        {
            // 1. Get the Global Filter from Session set by the Layout Switcher
            int? globalProgramId = Session["AgentProgramId"] as int?;

            // 2. Initialize ViewModel with Defaults
            var vm = new AllRequestsViewModel
            {
                FromDate = fromDate ?? DateTime.Now.AddDays(-365),
                ToDate = toDate ?? DateTime.Now.Date,
                StatusFilter = statusFilter ?? "All",
                SearchText = searchText
            };

            var query = _db.Request_Master.AsQueryable();

            // 3. APPLY GLOBAL PROGRAM FILTER
            if (globalProgramId.HasValue)
            {
                query = query.Where(r => r.program_id == globalProgramId);
            }

            // 4. Apply Date Filtering Logic
            DateTime startDate = vm.FromDate.Date;
            DateTime endDate = vm.ToDate.Date.AddDays(1).AddTicks(-1);
            query = query.Where(r => r.RequestDate >= startDate && r.RequestDate <= endDate);

            // 5. Status Filtering
            if (vm.StatusFilter != "All")
            {
                query = query.Where(r => r.status == vm.StatusFilter);
            }

            // 6. Text Search Logic
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

            // 7. Execute search and assign to the list (use async)
            vm.RequestList = await query.OrderByDescending(r => r.RequestID).ToListAsync();

            // 8. Summary Statistics (Filtered by Global Program)
            var statsQuery = _db.Request_Master.AsQueryable();
            if (globalProgramId.HasValue)
            {
                statsQuery = statsQuery.Where(r => r.program_id == globalProgramId);
            }

            vm.QueueCount = await statsQuery.CountAsync(r => r.status == "Q");
            vm.ForwardedCount = await statsQuery.CountAsync(r => r.status == "F");
            vm.ResolvedCount = await statsQuery.CountAsync(r => r.status == "R");
            vm.ClosedCount = await statsQuery.CountAsync(r => r.status == "C");

            return View("~/Views/ServiceRequest/ViewAllRequests.cshtml", vm);
        }

        public ActionResult Details(int id)
        {
            // 1. Fetch the request
            var request = _db.Request_Master.FirstOrDefault(r => r.RequestID == id);
            if (request == null) return HttpNotFound();

            // 2. Get the Global Filter from Session and Admin status
            int? globalProgramId = Session["AgentProgramId"] as int?;
            bool isSuperAdmin = Session["IsAdmin"]?.ToString() == "Y";

            // --- SECURITY & CONTEXT CHECK ---
            // If a specific program is selected (not "All") and the user isn't a SuperAdmin,
            // ensure the request actually belongs to that program.
            if (globalProgramId.HasValue && !isSuperAdmin)
            {
                if (request.program_id != globalProgramId)
                {
                    TempData["Error"] = "Context Mismatch: This request belongs to a different program.";
                    return RedirectToAction("ViewAllRequests");
                }
            }

            // 3. Filter the Employee List (Agents) by the selected program
            var filteredAgents = _db.agent
                .Where(a => !globalProgramId.HasValue || a.ProgramId == globalProgramId)
                .OrderBy(a => a.Name)
                .ToList();

            // 4. Filter the Group List by the selected program
            var filteredGroups = _db.groups
                .Where(g => !globalProgramId.HasValue || g.program_id == globalProgramId)
                .OrderBy(g => g.gname)
                .ToList();

            // 5. Assign to ViewBag for the dropdowns
            ViewBag.EmployeeList = new SelectList(filteredAgents, "Pno", "Name");
            ViewBag.GroupList = new SelectList(filteredGroups, "gid", "gname");

            return View("~/Views/ServiceRequest/Details.cshtml", request);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForwardRequest(int id, string Forward_To, string Forward_To_Type, string ForwardedRemarks)
        {
            // If Forward_To came in empty, try the raw form value
            if (string.IsNullOrEmpty(Forward_To))
            {
                Forward_To = Request.Form["Forward_To_Agent"];
            }

            var req = _db.Request_Master.Find(id);
            if (req == null) return HttpNotFound();

            string senderName = Session["AgentName"]?.ToString() ?? "Unknown User";
            string senderPno = Session["AgentPno"]?.ToString() ?? "SYSTEM";
            string targetName = Forward_To;

            if (Forward_To_Type == "I")
            {
                var targetEmp = _db.agent.FirstOrDefault(e => e.Pno.Trim() == Forward_To.Trim());
                targetName = targetEmp?.Name ?? "Agent (" + Forward_To + ")";
            }
            else if (Forward_To_Type == "G")
            {
                if (int.TryParse(Forward_To, out int gid))
                {
                    var targetGroup = _db.groups.FirstOrDefault(g => g.gid == gid);
                    targetName = targetGroup?.gname ?? "Group " + Forward_To;
                }
            }

            // Now targetName is guaranteed to have a value
            string timestamp = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");
            string newEntry = $"<div class='log-entry' style='border-left:3px solid #3498db; padding-left:10px; margin-bottom:5px;'>" +
                              $"{timestamp} forwarded to <strong>{targetName}</strong> by <strong>{senderName}</strong>" +
                              $"</div>";

            // ... (rest of your save logic)

        req.Forward_By = senderPno;
            req.Forward_To = Forward_To;
            req.Forward_To_Type = Forward_To_Type;
            req.ForwardedDate = DateTime.Now;
            req.ForwardedRemarks = ForwardedRemarks;
            req.status = "F";
            req.ReqDetails = (req.ReqDetails ?? "") + newEntry;

            await _db.SaveChangesAsync();
            var workshopEntry = _db.NewIncomings.FirstOrDefault(n => n.RequestID == id);
            if (workshopEntry != null)
            {
                workshopEntry.assignto = targetName;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> TakeOwnership(int id)
        {
            var req = _db.Request_Master.Find(id);
            if (req == null) return HttpNotFound();

            string agentName = Session["AgentName"]?.ToString() ?? "Unknown Agent";
            string agentPno = Session["AgentPno"]?.ToString() ?? "";

            string timestamp = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");
            string logEntry = $"<div class='log-entry' style='background:#f0fff4; border-left:3px solid #2e7d57; padding:5px; margin-top:5px;'>" +
                              $"{timestamp} ,ownership taken by {agentName}</div>";

            req.ownership = agentPno;
            req.status = "O";
            req.ReqDetails = (req.ReqDetails ?? "") + logEntry;

            _db.Configuration.ValidateOnSaveEnabled = false;
            await _db.SaveChangesAsync();
            _db.Configuration.ValidateOnSaveEnabled = true;

            return RedirectToAction("Details", new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CloseRequest(int id, string ClosingRemarks)
        {
            var req = _db.Request_Master.Find(id);
            if (req == null) return HttpNotFound();

            string timestamp = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");
            string agentName = Session["AgentName"]?.ToString() ?? "Admin";
            string agentPno = Session["AgentPno"]?.ToString() ?? "ADMIN";

            string log = $"<div class='log-entry' style='background:#fff5f5; color:red; border-left:3px solid red; padding:5px;'>{timestamp} CLOSED by {agentName}. Remarks: {ClosingRemarks}</div>";

            // 1. Update status
            req.status = "C";

            // 2. CHECK YOUR MODEL PROPERTY NAMES HERE:
            // If your model uses 'ReqCloseBy', keep it. 
            // If it uses 'RequestCloseBy' or 'CloseBy', change it below:
            req.ReqCloseBy = agentPno;
            req.ReqCloseDate = DateTime.Now;

            // 3. Update History
            req.ReqDetails = (req.ReqDetails ?? "") + log;

            // 4. Force Entity Framework to see the changes
            _db.Entry(req).State = System.Data.Entity.EntityState.Modified;

            _db.Configuration.ValidateOnSaveEnabled = false;
            await _db.SaveChangesAsync();
            _db.Configuration.ValidateOnSaveEnabled = true;

            // Update DateOut in NewIncoming if this is a workshop request
            var workshopEntry = _db.NewIncomings.FirstOrDefault(n => n.RequestID == id);
            if (workshopEntry != null)
            {
                workshopEntry.WarrantyDateOut = DateTime.Now.ToString("dd/MM/yyyy");
                // Ensure this is also marked as modified
                _db.Entry(workshopEntry).State = System.Data.Entity.EntityState.Modified;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("ViewAllRequests");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResolveRequest(int id, string ActualPArea)
        {
            var req = _db.Request_Master.Find(id);
            if (req == null) return HttpNotFound();

            if (string.IsNullOrEmpty(ActualPArea)) return RedirectToAction("Details", new { id });

            string resolverName = Session["AgentName"]?.ToString() ?? "Unknown Agent";
            string resolverPno = Session["AgentPno"]?.ToString() ?? "";

            string timestamp = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");
            string logEntry = $"<div class='log-entry' style='background:#e6f7ff; border-left:3px solid #1890ff; padding:5px; margin-top:5px;'>" +
                              $"{timestamp} ,resolved by {resolverName}. <strong>Actual Problem Area: {ActualPArea}</strong></div>";

            req.status = "R";
            req.ActualPArea = ActualPArea;
            req.ReqResolveDate = DateTime.Now;
            req.ReqResolveBy = resolverPno;
            req.ReqDetails = (req.ReqDetails ?? "") + logEntry;

            _db.Configuration.ValidateOnSaveEnabled = false;
            await _db.SaveChangesAsync();
            _db.Configuration.ValidateOnSaveEnabled = true;

            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReopenRequest(int id)
        {
            var req = _db.Request_Master.Find(id);
            if (req == null) return HttpNotFound();

            string agentName = Session["AgentName"]?.ToString() ?? "Admin";
            string timestamp = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

            string logEntry = $"<div class='log-entry' style='background:#fff0f6; border-left:3px solid #eb2f96; padding:5px; margin-top:5px;'>" +
                              $"{timestamp} ,REOPENED by {agentName}</div>";

            req.status = "F";
            req.ReqDetails = (req.ReqDetails ?? "") + logEntry;
            req.ReqCloseBy = null;
            req.ReqCloseDate = null;

            _db.Configuration.ValidateOnSaveEnabled = false;
            await _db.SaveChangesAsync();
            _db.Configuration.ValidateOnSaveEnabled = true;

            return RedirectToAction("Details", new { id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}