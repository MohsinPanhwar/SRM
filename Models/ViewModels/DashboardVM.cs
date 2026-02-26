using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRM.Models.ViewModels
{
    public class DashboardVM
    {
        // Metric Totals
        public int TotalRequests { get; set; }
        public int OpenRequests { get; set; }
        public int ClosedRequests { get; set; }
        public int HighPriorityRequests { get; set; }

        // Data for the recent activity list
        public List<Request_Master> RecentRequests { get; set; }
    }
}