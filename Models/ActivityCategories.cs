using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SRM.Models
{
    [Table("ActivityCategories")]
    public class ActivityCategories
    {
        [Key]
        [StringLength(3)]
        [Column(TypeName = "char")]
        public string cat_id { get; set; }

        [StringLength(50)]
        public string cat_name { get; set; }

        public int? program_id { get; set; }
    }
}