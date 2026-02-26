using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SRM.Models
{
    [Table("invCategory")]   // exact table name
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Column("CategoryName")]
        [StringLength(75)]
        public string CategoryName { get; set; }
    }
}