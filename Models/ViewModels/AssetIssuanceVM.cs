using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SRM.Models.ViewModels
{
	public class AssetIssuanceVM
	{
        public string Pno { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Office_Ext { get; set; }
        public string Location { get; set; }
        public string RoomNo { get; set; }


        public string IpAddress { get; set; }

        public InvIssueDetail Issue { get; set; }

        // 🔹 Dropdowns
        public List<SelectListItem> CategoryList { get; set; }
        public List<SelectListItem> BrandList { get; set; }
        public List<SelectListItem> LocationList { get; set; }

        public List<SRM.Models.InvIssueDetail> RecentAssets { get; set; }
    }
}