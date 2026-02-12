using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRM.Models.ViewModels
{
    public class ManageUserVM
    {
        public int Sno { get; set; }

        public string ImagePath { get; set; }

        public string Name { get; set; }
        public string Pno { get; set; }

        public DateTime? LastLoginDate { get; set; }
        public string LastLoginIP { get; set; }

        public string Email { get; set; }
        public string MobileNo { get; set; }

        public string Status { get; set; }
    }

}