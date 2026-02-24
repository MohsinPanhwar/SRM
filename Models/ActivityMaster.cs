using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SRM.Models
{
    [Table("ActivityMaster")]
    public class ActivityMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int sno { get; set; }

        public int? Activity { get; set; }

        [StringLength(5)]
        public string Location { get; set; }

        public DateTime? ActivityDate { get; set; }

        [StringLength(50)]
        public string ReportBy { get; set; }

        public DateTime? ReportDate { get; set; }

        public string Detail { get; set; }

        [StringLength(10)]
        public string ClosedBy { get; set; }

        public DateTime? ClosedDate { get; set; }

        public string ResolutionDetail { get; set; }

        [StringLength(1)]
        public string status { get; set; }

        public string workLog { get; set; }

        public int? ServiceRequestID { get; set; }

        public int? program_id { get; set; }

        public DateTime? ActivityEndDate { get; set; }
    }

}