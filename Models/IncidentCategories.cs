using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SRM.Models
{
    [Table("IncidentCategories")]
    public class IncidentCategories
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int cat_id { get; set; }

        [StringLength(100)]
        public string cat_name { get; set; }

        public int? program_id { get; set; }
    }
}