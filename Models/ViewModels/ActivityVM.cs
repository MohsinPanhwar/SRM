using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRM.Models.ViewModels
{
    public class ActivityVM
    {
        public int sno { get; set; }
        public string ActivityName { get; set; }
        public string LocationName { get; set; }
        public DateTime? ActivityDate { get; set; }

        // Add this line to fix the error:
        public DateTime? ActivityEndDate { get; set; }

        public DateTime? ReportDate { get; set; }
        public string ReportBy { get; set; }
        public string Detail { get; set; }
        public string workLog { get; set; }
        public string ResolutionDetail { get; set; }
        public string status { get; set; }
    }
}