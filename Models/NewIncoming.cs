using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SRM.Models
{
    [Table("NewIncoming")]
    public class NewIncoming
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        public int? RequestID { get; set; }

        public string Location { get; set; }

        public int? Ext { get; set; }

        public string UserName { get; set; }

        public int? Staffno { get; set; }

        public string IP { get; set; }

        public string ProdName { get; set; }

        public string Model { get; set; }

        public string Serial { get; set; }

        public string Ram { get; set; }

        public string HDD { get; set; }

        public string LNIATA { get; set; }

        public string Fault { get; set; }

        public string Diagnosed { get; set; }
        [Column("DataBackup")]
        public string DataBackup { get; set; }

        public string WarrantyDateOut { get; set; }

        public string DateIn { get; set; }

        public string Status { get; set; }

        public string uid { get; set; }

        public DateTime? time_stamp { get; set; }

        public string assignto { get; set; }

        public string userdetails { get; set; }

        public string userip { get; set; }
    }
}