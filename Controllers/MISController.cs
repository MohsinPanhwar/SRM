using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SRM.Data;
using SRM.Models.ViewModels; // Import your VM namespace

namespace SRM.Controllers
{
    public class MISController : BaseController
    {
        private readonly AppDbContext _db = new AppDbContext();

        // GET: MIS/Dashboard
        public ActionResult Dashboard()
        {
            // Initialize the ViewModel we defined earlier
            var viewModel = new DashboardVM();

            try
            {
                // 1. Get High-Level Stats from Request_Master
                viewModel.TotalRequests = _db.Request_Master.Count();

                // Assuming 'C' is Closed, anything else is Open
                viewModel.OpenRequests = _db.Request_Master.Count(r => r.status != "C");
                viewModel.ClosedRequests = _db.Request_Master.Count(r => r.status == "C");

                // Logic for High Priority (adjust if your priority scale is different)
                viewModel.HighPriorityRequests = _db.Request_Master.Count(r => r.Priority <= 2);

                // 2. Get Recent Activities (Last 5)
                viewModel.RecentRequests = _db.Request_Master
                    .OrderByDescending(r => r.RequestDate)
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                // Simple error logging
                System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex.Message}");
                // Ensure list isn't null even on error
                viewModel.RecentRequests = new List<SRM.Models.Request_Master>();
            }

            return View("~/Views/MIS/Dashboard.cshtml",viewModel);
        }
        // GET: MIS/Index
        public ActionResult MIS()
        {
            return View("~/Views/MIS/Mis.cshtml");
        }

        public ActionResult RequestMaster()
        {
            var vm = new DashboardVM();

            vm.TotalRequests = _db.Request_Master.Count();
            vm.OpenRequests = _db.Request_Master.Where(r => r.status != "C" || r.status == null).Count();
            vm.ClosedRequests = _db.Request_Master.Where(r => r.status == "C").Count();

            vm.RequestsByPriority = _db.Request_Master
                .GroupBy(r => r.Priority ?? 0)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Priority, x => x.Count);

            vm.RecentRequests = _db.Request_Master
                                  .OrderByDescending(r => r.RequestDate)
                                  .Take(10)
                                  .ToList();

            // ⭐ Most reported by who logged the request
            vm.MostReportedBy = _db.Request_Master
                .Where(r => r.RequestLogBy != null)
                .GroupBy(r => r.RequestLogBy)
                .Select(g => new EmployeeRequestStats
                {
                    Employee = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5) // top 5
                .ToList();

            // ⭐ Most resolved by who closed the request
            vm.MostResolvedBy = _db.Request_Master
                .Where(r => r.ReqCloseBy != null)
                .GroupBy(r => r.ReqCloseBy)
                .Select(g => new EmployeeRequestStats
                {
                    Employee = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            // ⭐ Most forwarded by who forwarded
            vm.MostForwardedBy = _db.Request_Master
                .Where(r => r.Forward_By != null)
                .GroupBy(r => r.Forward_By)
                .Select(g => new EmployeeRequestStats
                {
                    Employee = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            return View("~/Views/MIS/Mis.cshtml",vm);
        }
        protected override void Dispose(bool disposing)
            {
                if (disposing) { _db.Dispose(); }
                base.Dispose(disposing);
            }
        }
    }