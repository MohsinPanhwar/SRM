using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using SRM.Models;

namespace SRM.Models
{
    public class Program_Setup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Program_Id { get; set; }

        public string Program_Name { get; set; }
      
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string uid { get; set; }

    }
}