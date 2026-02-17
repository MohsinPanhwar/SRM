using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SRM.Models
{
    [Table("Role", Schema = "dbo")]
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("RoleID")]
        public int Role_Id { get; set; }

        [Column("Role_Name")]
        [StringLength(100)]
        public string Role_Name { get; set; }

        [Column("Privilege")]
        [StringLength(500)]
        public string Privilege { get; set; }

        [Column("program_id")]
        public int? program_Id { get; set; }
    }
}