using dt191g_project.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace dt191g_project.Data
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        { }
            public DbSet<Company> Companies { get; set; }
            public DbSet<Service> Services { get; set; }
            public DbSet<TimeSlot> TimeSlots { get; set; }
            public DbSet<Booking> Bookings { get; set; }
            public DbSet<CustomerProfile> CustomerProfiles { get; set; }
            public DbSet<CompanyUser> CompanyUsers { get; set; }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                base.OnModelCreating(builder);

                // CompanyUser => ApplicationUser (fler-till-en)
                builder.Entity<CompanyUser>()
                    .HasOne(cu => cu.User)
                    .WithMany(u => u.CompanyUsers)
                    .HasForeignKey(cu => cu.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // CustomerProfile => ApplicationUser (en-till-en)
                builder.Entity<CustomerProfile>()
                    .HasOne(cp => cp.User)
                    .WithOne(u => u.CustomerProfile)
                    .HasForeignKey<CustomerProfile>(cp => cp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // TimeSlot => Company (fler-till-en)
                builder.Entity<TimeSlot>()
                    .HasOne(ts => ts.Company)
                    .WithMany(c => c.TimeSlots)
                    .HasForeignKey(ts => ts.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);

            // Service => Company (fler-till-en)
            builder.Entity<Service>()
                .HasOne(s => s.Company)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking => Service (fler-till-en)
            builder.Entity<Booking>()
                .HasOne(b => b.Service)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking => ApplicationUser (fler-till-en)
            builder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany()
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);
                
            }
        
    }
    
}
