using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using SRM.Models;

namespace SRM.Data
{
    public class AppDbContext : DbContext
    {
            public AppDbContext() : base("name=DBSRM")
            {
            }

        public DbSet<Program_Setup> Programs { get; set; }
        public DbSet<Agent> agent { get; set; }
        public DbSet<Group> groups { get; set; }
        public DbSet<Category> invCategory { get; set; }
        public DbSet<Brand> invBrand { get; set; }
        public DbSet<EmployeeProfile> Employees { get; set; }
        public DbSet<InvIssueDetail> InvIssueDetails { get; set; }

        public System.Data.Entity.DbSet<SRM.Models.Location> Locations { get; set; }
    }
}