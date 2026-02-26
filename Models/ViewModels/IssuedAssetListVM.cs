using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace SRM.Models.ViewModels
{
	public class IssuedAssetListVM
	{
        public string SelectedPno { get; set; }
        public int? SelectedBrandId { get; set; }

        // 🔹 Dropdowns
        public List<SelectListItem> BrandList { get; set; }
        public List<SelectListItem> PnoList { get; set; }

        // 🔹 Result Table
        public List<InvIssueDetail> Assets { get; set; }
      

        public List<InvIssueDetail>Location { get; set; }
    }
}