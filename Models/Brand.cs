using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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