using System;
using System.Collections.Generic;

namespace SRM.Models.ViewModels
{
    public class ManageRoleVM
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public int? program_Id { get; set; }
        public string ProgramName { get; set; }
        public int UserCount { get; set; }
        public List<ManageRoleVM> ExistingRoles { get; set; }

        // Privilege properties
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