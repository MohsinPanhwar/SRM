using System.Collections.Generic;
namespace SRM.Models.ViewModels
{
    public class EmployeeRequestStats
    {
        public string Employee { get; set; }
        public int Count { get; set; }
    }

    public class DashboardVM
    {
        public int TotalRequests { get; set; }
        public int OpenRequests { get; set; }
        public int ClosedRequests { get; set; }
        public int HighPriorityRequests { get; set; }

        public Dictionary<int, int> RequestsByPriority { get; set; }
        public List<Request_Master> RecentRequests { get; set; }

        // New stats:
        public List<EmployeeRequestStats> MostReportedBy { get; set; }
        public List<EmployeeRequestStats> MostResolvedBy { get; set; }
        public List<EmployeeRequestStats> MostForwardedBy { get; set; }

    }
}