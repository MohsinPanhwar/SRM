using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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