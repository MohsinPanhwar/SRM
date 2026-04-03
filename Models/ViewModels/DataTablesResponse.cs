using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRM.Models.ViewModels
{
    public class DataTablesResponse
    {
        public int draw { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }
        public List<Request_Master> data { get; set; }
    }
}