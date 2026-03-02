using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SRM.Models
{
   
    [Table("IncidentLocation")]
    public class IncidentLocation
    {
        [Key]
        [StringLength(3)]
        [Column(TypeName = "char")]
        public string LocationCode { get; set; }

        [StringLength(50)]
        public string LocationName { get; set; }
    }
}