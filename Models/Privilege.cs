using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SRM.Models
{
    public class Privilege
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int privilege_id { get; set; }

        // Boolean flags for each permission
        public bool CanAddEditEngineer { get; set; }
        public bool CanLogNewRequest { get; set; }
        public bool CanViewForwardAny { get; set; }
        public bool CanOnlyViewAny { get; set; }
        public bool CanViewForwardOwn { get; set; }
        public bool CanAddEditGroups { get; set; }
        public bool CanAddNewIncident { get; set; }
        public bool CanViewIncident { get; set; }
        public bool CanLogNOC { get; set; }
        public bool CanViewOwnGroup { get; set; }
        public bool CanReopenAny { get; set; }
        public bool CanViewEditMessage { get; set; }
        public bool CanSendSMS { get; set; }
        public bool CanHardware { get; set; }
        public bool CanSendPassword { get; set; }
        public bool CanViewReports { get; set; }
        public bool CanAddEditAsset { get; set; }
    }
}