using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SRM.Models
{

    [Table("invBrand")]
    public class Brand
    {
        [Key]
        public int ID { get; set; }

        [StringLength(75)]
        public string Name { get; set; }
    }
}