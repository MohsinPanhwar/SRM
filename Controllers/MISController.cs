using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SRM.Data;
using SRM.Models.ViewModels;

namespace SRM.Controllers
{
    public class MISController : Controller
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
            // FIX: Use the class instead of an anonymous type
            var results = new MisResult();

            string cat = string.IsNullOrEmpty(category) ? "Activities" : category.Trim();

            switch (cat)
            {
                case "Activities":
                    int aOpen = _db.ActivityMasters.Count(x => x.status != "C");
                    int aClosed = _db.ActivityMasters.Count(x => x.status == "C");
                    results.labels.AddRange(new[] { "Open", "Closed" });
                    results.data.Add(aOpen);
                    results.data.Add(aClosed);
                    results.kpi1 = aOpen;   // Now this is allowed!
                    results.kpi2 = aClosed;
                    results.kpi3 = aOpen + aClosed;
                    break;

                case "Requests":
                    int rOpen = _db.Request_Master.Count(x => x.status != "C" && x.status != "F");
                    int rFwd = _db.Request_Master.Count(x => x.status == "F");
                    int rClosed = _db.Request_Master.Count(x => x.status == "C");
                    results.labels.AddRange(new[] { "Open", "Forwarded", "Closed" });
                    results.data.Add(rOpen);
                    results.data.Add(rFwd);
                    results.data.Add(rClosed);
                    results.kpi1 = rOpen;
                    results.kpi2 = rFwd;
                    results.kpi3 = rOpen + rFwd + rClosed;
                    break;

                case "Assets":
                    results.kpi1 = _db.InvIssueDetails.Count();
                    results.kpi2 = _db.InvIssueDetails.Select(x => x.Location_ID).Distinct().Count();
                    results.kpi3 = results.kpi1;
                    // Add some chart data for assets
                    results.labels.Add("Total Assets");
                    results.data.Add(results.kpi1);
                    break;

                case "Incidents":
                    int iRes = _db.ActivityMasters.Count(x => x.ServiceRequestID != null && x.status == "C");
                    int iUnres = _db.ActivityMasters.Count(x => x.ServiceRequestID != null && x.status != "C");
                    results.labels.AddRange(new[] { "Resolved", "Unresolved" });
                    results.data.Add(iRes);
                    results.data.Add(iUnres);
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