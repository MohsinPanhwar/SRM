using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SRM.Models
{
    [Table("InvIssueDetail")]
    public class InvIssueDetail
    {
        [Key]
        public int sno { get; set; }
        public DateTime entry_date { get; set; }
        public string enter_by { get; set; }

        public int CategoryID { get; set; }
        public int BrandID { get; set; }
        public string modelNumber { get; set; }

        [Column("station")]
        public string Location_ID { get; set; }

        // ══════════════════════════════════════════════════════════════
        // FIX 1: NOT MAPPED PROPERTIES
        // These caused the "Invalid Column Name" errors in your screenshot.
        // ══════════════════════════════════════════════════════════════

        [NotMapped]
        public string CategoryName { get; set; }

        [NotMapped]
        public string BrandName { get; set; }

        [NotMapped]
        public string Locations { get; set; }

        [NotMapped]
        public string station { get; set; }

        // ══════════════════════════════════════════════════════════════
        // FIX 2: COLUMN MAPPING
        // Based on your DB screenshot, the column is named "Location".
        // Use [Column("Location")] if that is where the ID '5' is stored.
        // ══════════════════════════════════════════════════════════════

       

        // ══════════════════════════════════════════════════════════════
        // FIX 3: DATA TYPES & OPTIONAL FIELDS
        // Removed [Required] from fields that might be empty.
        // ══════════════════════════════════════════════════════════════

        [Required]
        public string serialNumber { get; set; }

        public string assetTag { get; set; }
        public string condition { get; set; }
        public string accessories { get; set; }
        public string other_accessories { get; set; }

        // Changed to string to match your HTML date input behavior
        public string warranty { get; set; }

        public string previousUser { get; set; }

        [Required]
        public string Issued_to_PNO { get; set; }

        public string otherserialNumber { get; set; }
        public string gatepass { get; set; }
        public string remarks { get; set; }
        public string ponum { get; set; }

        public DateTime IssueDate { get; set; }
        public string TelExt { get; set; }
    }
}