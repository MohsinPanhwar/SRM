using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SRM.Models
{
    [Table("Groups")]
    public class Group
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int gid { get; set; }

        public string gname { get; set; }

        public int? program_id { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string uid { get; set; }

        
        public string manager_pno { get; set; }
    }
}
