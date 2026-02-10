using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace SRM.Models.ViewModels
{
    public class ManageGroupVM
    {
        public int GroupId { get; set; }
        public string ProgramName { get; set; }
        public string GroupName { get; set; }
        public string Manager { get; set; } // This will now hold the PNo from the dropdown
        public int MembersCount { get; set; }

        // Dropdown data containers
        public List<SelectListItem> ProgramList { get; set; }
        public List<SelectListItem> AgentList { get; set; }

        // Container for the list of existing groups
        public List<ManageGroupVM> ExistingGroups { get; set; }
    }
}