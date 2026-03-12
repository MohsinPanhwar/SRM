using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRM.Models.ViewModels
{
	public class IncidentVM
	{
        // For the Table View
        public int IncidentID { get; set; }
        public string IncidentName { get; set; }
        public int sno { get; set; }
        public string CategoryName { get; set; }
        public string LocationName { get; set; }
        public DateTime? IncidentDate { get; set; }
        public string ReportBy { get; set; }
        public DateTime? ReportDate { get; set; }
        public string status { get; set; }
        public string Incident { get; set; }
        // For Detail View / WorkLog
        public string Detail { get; set; }
        public string ResolutionDetail { get; set; }
        public string workLog { get; set; }
        public int? ServiceRequestID { get; set; }
        public string GroupName { get; set; } // To show "Network", "Hardware", etc.
        public string ManagerName { get; set; } // To show "P56814-MOHAMMAD SHOAIB"
        public string IncidentType { get; set; } // To show "Network", "Software", etc.
    }
}