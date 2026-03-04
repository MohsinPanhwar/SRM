using SRM.Data;
using SRM.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace SRM.Controllers
{
    public class MISController : BaseController
    {
        private readonly AppDbContext _db = new AppDbContext();

        // 1. THIS LOADS THE PAGE
        // URL: /MIS/Dashboard
        public ActionResult Dashboard()
        {
            var viewModel = new DashboardVM();
            try
            {
                viewModel.TotalRequests = _db.Request_Master.Count();
                viewModel.OpenRequests = _db.Request_Master.Count(r => r.status != "C");
                viewModel.ClosedRequests = _db.Request_Master.Count(r => r.status == "C");
                viewModel.HighPriorityRequests = _db.Request_Master.Count(r => r.Priority <= 2);

                viewModel.RecentRequests = _db.Request_Master
                    .OrderByDescending(r => r.RequestDate)
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                viewModel.RecentRequests = new List<SRM.Models.Request_Master>();
            }

            return View("~/Views/MIS/Dashboard.cshtml", viewModel);
        }
        // csharp
        public ActionResult MIS()
        {
            var vm = new DashboardVM();
            vm.TotalRequests = _db.Request_Master.Count();
            vm.OpenRequests = _db.Request_Master.Count(r => r.status != "C");
            vm.ClosedRequests = _db.Request_Master.Count(r => r.status == "C");
            vm.HighPriorityRequests = _db.Request_Master.Count(r => r.Priority <= 2);
            vm.RecentRequests = _db.Request_Master.OrderByDescending(r => r.RequestDate).Take(5).ToList();
            return View("~/Views/MIS/Mis.cshtml", vm);
        }

        // 2. THIS PROVIDES DATA FOR THE AJAX CALL
        // URL: /MIS/Overall
        // Define a small helper class at the top of your Controller or inside the method
        public class MisResult
        {
            public List<string> labels { get; set; } = new List<string>();
            public List<int> data { get; set; } = new List<int>();
            public int kpi1 { get; set; }
            public int kpi2 { get; set; }
            public int kpi3 { get; set; }
        }

        [HttpGet]
        public JsonResult Overall(string category)
        {
            var results = new MisResult();
            string cat = string.IsNullOrEmpty(category) ? "Activities" : category.Trim();

            // --- NEW: Get the Global Filter from Session (Same as GroupController) ---
            int? globalProgramId = Session["AgentProgramId"] as int?;

            switch (cat)
            {
                case "Activities":
                    // Filter queries by Global Program ID
                    var activities = _db.ActivityMasters
                        .Where(x => !globalProgramId.HasValue || x.program_id == globalProgramId);

                    int aOpen = activities.Count(x => x.status != "C");
                    int aClosed = activities.Count(x => x.status == "C");

                    results.labels.AddRange(new[] { "Open", "Closed" });
                    results.data.AddRange(new[] { aOpen, aClosed });
                    results.kpi1 = aOpen;
                    results.kpi2 = aClosed;
                    results.kpi3 = aOpen + aClosed;
                    break;

                case "Requests":
                    var requests = _db.Request_Master
                        .Where(x => !globalProgramId.HasValue || x.program_id == globalProgramId);

                    int rOpen = requests.Count(x => x.status != "C" && x.status != "F");
                    int rResolved = requests.Count(x => x.status == "F");
                    int rClosed = requests.Count(x => x.status == "C");

                    results.labels.AddRange(new[] { "Open", "Resolved", "Closed" });
                    results.data.AddRange(new[] { rOpen, rResolved, rClosed });
                    results.kpi1 = rOpen;
                    results.kpi2 = rResolved;
                    results.kpi3 = rOpen + rResolved + rClosed;
                    break;

                case "Assets":
                    // Filter Asset Issuance by Program
                    var assets = _db.InvIssueDetails;
                    results.kpi1 = assets.Count();
                    results.kpi2 = assets.Select(x => x.Location_ID).Distinct().Count();
                    results.kpi3 = results.kpi1;
                    results.labels.Add("Total Assets");
                    results.data.Add(results.kpi1);
                    break;

                case "Incidents":
                    var incidents = _db.ActivityMasters
                        .Where(x => x.ServiceRequestID != null)
                        .Where(x => !globalProgramId.HasValue || x.program_id == globalProgramId);

                    int iRes = incidents.Count(x => x.status == "C");
                    int iUnres = incidents.Count(x => x.status != "C");

                    results.labels.AddRange(new[] { "Resolved", "Unresolved" });
                    results.data.AddRange(new[] { iRes, iUnres });
                    results.kpi1 = iRes;
                    results.kpi2 = iUnres;
                    results.kpi3 = iRes + iUnres;
                    break;
            }

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}