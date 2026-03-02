using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SRM.Models
{
    [Table("Agent", Schema = "dbo")]
    public class Agent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("sno")]
        public int Sno { get; set; }

        [Column("pno")]
        [StringLength(50)]
        public string Pno { get; set; }

        [Column("Name")]
        [StringLength(50)]
        public string Name { get; set; }

        [Column("create_dt")]
        public DateTime? CreateDateTime { get; set; }

        [Column("close_dt")]
        public DateTime? CloseDateTime { get; set; }

        [Column("status")]
        [StringLength(1)]
        public string Status { get; set; }

        [Column("Close_Remarks")]
        [StringLength(200)]
        public string CloseRemarks { get; set; }

        [Column("isAdministrator")]
        [StringLength(1)]
        public string IsAdministrator { get; set; }

        [Column("LastLogin_DateTime")]
        public DateTime? LastLoginDateTime { get; set; }

        [Column("LastLogin_IP")]
        [StringLength(50)]
        public string LastLoginIp { get; set; }

        [Column("email")]
        [StringLength(100)]
        public string Email { get; set; }

        [Column("mobile")]
        [StringLength(50)]
        public string Mobile { get; set; }

        [Column("workarea")]
        [StringLength(500)]
        public string WorkArea { get; set; }

        [Column("gid")]
        public int? Gid { get; set; }

        [Column("Privilege")]
        [StringLength(100)]
        public string Privilege { get; set; }

        [Column("roleid")]
        public int? RoleId { get; set; }

        [Column("UserType")]
        [StringLength(1)]
        public string UserType { get; set; }

        [Column("uid")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        [StringLength(200)]
        public string Uid { get; set; }

        [Column("MobileOperator")]
        [StringLength(10)]
        public string MobileOperator { get; set; }

        [Column("program_id")]
        public int? ProgramId { get; set; }

        [Column("LastUpdate")]
        public DateTime? LastUpdate { get; set; }

        [Column("password")]
        public string Password { get; set; }
    }
}