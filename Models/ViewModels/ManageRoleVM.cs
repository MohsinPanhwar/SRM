using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRM.Models.ViewModels
{
    public class ManageRoleVM
    {
        // Role Info
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public int UserCount { get; set; }

        // Privileges (Matches the flags above)
        public bool CanAddEditEngineer { get; set; }
        public bool CanLogNewRequest { get; set; }
        // ... add the rest of the 17 booleans here ...
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

        // List for the table at the bottom
        public List<ManageRoleVM> ExistingRoles { get; set; }
    }
}