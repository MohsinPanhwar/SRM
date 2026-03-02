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