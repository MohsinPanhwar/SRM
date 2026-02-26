using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SRM.Models
{
    [Table("TMSDB_EMPLOYEEPROFILE")]
    public class EmployeeProfile
    {
        [Key]
        public string Pno { get; set; }

        public string emp_name { get; set; }

        public string Pay_group { get; set; }

        public string Dept_code { get; set; }

        public string Div_code { get; set; }

        public string Sec_code { get; set; }

        public string Loc_code { get; set; }

        public string Shift_code { get; set; }

        public string Emp_designation { get; set; }

        public string Office_ext { get; set; }

        public string Contact_no { get; set; }

        public string Status_id { get; set; }

        public int? Employee_type_id { get; set; }

        public DateTime? Last_promotion_date { get; set; }

        public string Gender { get; set; }

        public string Emp_title { get; set; }

        public DateTime? Emp_dob { get; set; }

        public string Batch_No { get; set; }

        public int? Register_ID { get; set; }

        public int? Mgr_ID { get; set; }

        public string Blood_Group { get; set; }

        public string Email { get; set; }

        public int? ATTENDANCE_DEPT { get; set; }

        public string ATTENDANCE_DEPT_CODE { get; set; }

        public DateTime? Appointment_Date { get; set; }

        public string Religion { get; set; }

        public string Sect { get; set; }

        public string Marital_Status { get; set; }

        public string Ex_Service { get; set; }

        public int? Domicile { get; set; }

        public string Auto { get; set; }

        public int? Vip { get; set; }

        public string Exempted { get; set; }

        public string Exempted_Reason { get; set; }

        public string Adjustments { get; set; }

        public string Attendance_Loc { get; set; }

        public string NIC { get; set; }

        public string NTN { get; set; }

        public string ADDRESS { get; set; }

        public string UPDATED_BY { get; set; }

        public DateTime? UPDATED_ON { get; set; }

        public string DEPT { get; set; }

        public string LOC { get; set; }

        public DateTime? data_download_dt { get; set; }

        public string ip_address { get; set; }

        public string roomno { get; set; }

        public string mobileno { get; set; }

        public string Location { get; set; }

        public string ContractType { get; set; }

        public string MobileOperator { get; set; }
    }
}