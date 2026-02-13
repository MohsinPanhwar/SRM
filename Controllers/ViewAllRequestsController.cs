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
            var vm = new AllRequestsViewModel
            {
                FromDate = fromDate ?? DateTime.Now.AddDays(-7),
                ToDate = toDate ?? DateTime.Now,
                StatusFilter = statusFilter ?? "All"
            };

            var query = _db.Request_Master.AsQueryable();

            // Date Filtering
            query = query.Where(r => r.RequestDate >= vm.FromDate && r.RequestDate <= vm.ToDate);

            // Status Filtering (Mapping DB codes)
            if (vm.StatusFilter != "All")
            {
                query = query.Where(r => r.status == vm.StatusFilter);
            }

            // Text Search
            if (!string.IsNullOrEmpty(searchText))
            {
                if (searchBy == "RequestID")
                {
                    int id = int.TryParse(searchText, out id) ? id : 0;
                    query = query.Where(r => r.RequestID == id);
                }
                else
                {
                    query = query.Where(r => r.ReqSummary.Contains(searchText));
                }
            }

            vm.RequestList = query.OrderByDescending(r => r.RequestID).ToList();

            // Summary Statistics (Global counts)
            vm.QueueCount = _db.Request_Master.Count(r => r.status == "Q"); // Assuming Q = Queue
            vm.ForwardedCount = _db.Request_Master.Count(r => r.status == "F"); // Assuming F = Forwarded
            vm.ResolvedCount = _db.Request_Master.Count(r => r.status == "R"); // Assuming R = Resolved
            vm.ClosedCount = _db.Request_Master.Count(r => r.status == "C"); // Assuming C = Closed

            return View("~/Views/ServiceRequest/ViewAllRequests.cshtml", vm);
        }
    }
}