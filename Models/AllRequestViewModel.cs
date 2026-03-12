using System;
using System.Collections.Generic;

namespace SRM.Models
{
    public class AllRequestsViewModel
    {
        // Filters
        public string SearchBy { get; set; }
        public string SearchText { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string StatusFilter { get; set; }

        // Data List
        public List<Request_Master> RequestList { get; set; }

        // Status Counts
        public int QueueCount { get; set; }
        public int ForwardedCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ClosedCount { get; set; }
        public int TotalCount => QueueCount + ForwardedCount + ResolvedCount + ClosedCount;
        public string SelectedStatus { get; set; } // Add this
    }
}