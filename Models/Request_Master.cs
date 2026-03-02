using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SRM.Models
{
    [Table("Request_Master")]
    public partial class Request_Master
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestID { get; set; }

        public DateTime? RequestDate { get; set; }

        [StringLength(10)]
        public string RequestLogBy { get; set; }

        [StringLength(10)]
        public string RequestFor { get; set; }

        public int? Priority { get; set; }

        [StringLength(100)]
        public string parea { get; set; }

        [StringLength(1)]
        public string status { get; set; }

        [StringLength(200)]
        public string ReqSummary { get; set; }

        [StringLength(100)]
        public string RequestedIPAddress { get; set; }

        [Column(TypeName = "text")]
        public string ReqDetails { get; set; }

        public DateTime? ForwardedDate { get; set; }

        [StringLength(10)]
        public string Forward_To { get; set; }

        [StringLength(250)]
        public string ForwardedRemarks { get; set; }

        public DateTime? ReqCloseDate { get; set; }

        [StringLength(10)]
        public string ReqCloseBy { get; set; }

        [StringLength(10)]
        public string Forward_By { get; set; }

        [StringLength(1)]
        public string Forward_To_Type { get; set; }

        [StringLength(10)]
        public string ownership { get; set; }

        [StringLength(50)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public string uid { get; set; }

        [StringLength(20)]
        public string Location { get; set; }

        [StringLength(100)]
        public string ActualPArea { get; set; }

        public DateTime? ReqResolveDate { get; set; }

        [StringLength(10)]
        public string ReqResolveBy { get; set; }

        public int? program_id { get; set; }

        [Column(TypeName = "text")]
        public string InitialRequest { get; set; }

        [StringLength(1)]
        public string UnsatisfactoryClosed { get; set; }

        [StringLength(20)]
        public string LogPortal { get; set; }
    }
}