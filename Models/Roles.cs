using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using SRM.Models;

namespace SRM.Models
{
    public class Roles
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Role_Id { get; set; }

        public string Role_Name { get; set; }

        public int Privilege_Id { get; set; }

    }
}