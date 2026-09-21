using CC.domain.Entities;
using CC.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CC.infrastructure.Data
{
    public class MasterCrmDbContext : IdentityDbContext
    {
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CompanyDatabase> CompanyDatabases => Set<CompanyDatabase>();
        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
        public DbSet<SubscriptionStatus> SubscriptionStatuses => Set<SubscriptionStatus>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();

        public MasterCrmDbContext(
            DbContextOptions<MasterCrmDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Company>(entity =>
            {
                entity.HasKey(x => x.CompanyId);

                entity.Property(x => x.CompanyCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.CompanyName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.IsActive);
                entity.Property(x => x.CreatedDate).HasColumnName("CreatedAt");

                entity.Ignore(x => x.AddressId);
                entity.Ignore(x => x.Address);
                entity.Ignore(x => x.ContactEmail);
                entity.Ignore(x => x.ContactPhone);
                entity.Ignore(x => x.Customers);
                entity.Ignore(x => x.Users);

                entity.HasIndex(x => x.CompanyCode)
                    .IsUnique();

                // Master CRM only manages company accounts and their database mappings.
                // Tenant-level entities belong to their respective tenant databases.
                entity.Ignore(x => x.Address);
                entity.Ignore(x => x.AddressId);
                entity.Ignore(x => x.ContactEmail);
                entity.Ignore(x => x.ContactPhone);
                entity.Property(x => x.CreatedDate).HasColumnName("CreatedAt");
                entity.Ignore(x => x.Users);
                entity.Ignore(x => x.Customers);
                entity.Ignore(x => x.Subscriptions);
            });

            builder.Entity<CompanyDatabase>(entity =>
            {
                entity.HasKey(x => x.CompanyDatabaseId);

                entity.Property(x => x.ServerName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.DatabaseName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.IsActive);

                entity.HasOne(x => x.Company)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasKey(x => x.PlanId);
                entity.Property(x => x.PlanName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Price).HasPrecision(18, 2);
            });

            builder.Entity<SubscriptionStatus>(entity =>
            {
                entity.HasKey(x => x.StatusId);
                entity.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
            });

            builder.Entity<Subscription>(entity =>
            {
                entity.HasKey(x => x.SubscriptionId);
                entity.HasOne(x => x.Company)
                    .WithMany(x => x.Subscriptions)
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Plan)
                    .WithMany(x => x.Subscriptions)
                    .HasForeignKey(x => x.PlanId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Status)
                    .WithMany(x => x.Subscriptions)
                    .HasForeignKey(x => x.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
