using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SRM.Models
{
	
        [Table("IncidentMaster")]
        public class IncidentMaster
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int sno { get; set; }

            public int? Incident { get; set; }

            [StringLength(5)]
            public string Location { get; set; }

            public DateTime? IncidentDate { get; set; }

            [StringLength(50)]
            public string ReportBy { get; set; }

            public DateTime? ReportDate { get; set; }

            [DataType(DataType.MultilineText)]
            public string Detail { get; set; }

            [StringLength(10)]
            public string ClosedBy { get; set; }

            public DateTime? ClosedDate { get; set; }

            [DataType(DataType.MultilineText)]
            public string ResolutionDetail { get; set; }

            [StringLength(1)]
            public string status { get; set; }

            [DataType(DataType.MultilineText)]
            public string workLog { get; set; }

            public int? ServiceRequestID { get; set; }

            public int? program_id { get; set; }

            [StringLength(15)]
            public string inctype { get; set; }

            public int? gid { get; set; }

            public DateTime? LastUpdate { get; set; }
        }
    }
