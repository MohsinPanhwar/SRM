using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SRM.Models
{
    [Table("TMSDB_EMPLOYEEPROFILE")]
    public class EmployeeProfile
    {
        [Key]
        [Column("Pno")]
        public string Pno { get; set; }
         

        [Column("emp_name")]
        public string Name { get; set; }

        [Column("Emp_designation")]
        public string Designation { get; set; }
         
        [Column("Dept_code")]
        public string Department { get; set; }
        
        [Column("Email")]
        public string Email { get; set; }

        [Column("mobileno")]
        public string Mobile { get; set; }

        [Column("office_ext")]
        public string Office_Ext { get; set; }

        [Column("Ip_Address")]
        public string Ip_Address { get; set; }

        [Column("Roomno")]
        public string Roomno{ get; set; }

        [Column("Location")] // Ensure this column name matches your SQL Table column name
        public string Location { get; set; }
    }  
}

