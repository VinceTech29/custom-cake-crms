using CC.domain.Entities;
using CC.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CC.infrastructure.Data
{
    public class CrmDbContext : DbContext
    {
        public CrmDbContext(DbContextOptions<CrmDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
        public DbSet<SubscriptionStatus> SubscriptionStatuses => Set<SubscriptionStatus>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<SystemUser> AppUsers => Set<SystemUser>();
        public DbSet<TermsAndConditions> TermsAndConditions => Set<TermsAndConditions>();
        public DbSet<TermsAcceptance> TermsAcceptances => Set<TermsAcceptance>();
        public DbSet<SystemAuditLog> SystemAuditLogs => Set<SystemAuditLog>();

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<CustomerInquiry> CustomerInquiries => Set<CustomerInquiry>();
        public DbSet<FollowUpStatus> FollowUpStatuses => Set<FollowUpStatus>();
        public DbSet<CustomerFollowUp> CustomerFollowUps => Set<CustomerFollowUp>();

        public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
        public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
        public DbSet<SalesOrderDetail> SalesOrderDetails => Set<SalesOrderDetail>();
        public DbSet<CakeCustomization> CakeCustomizations => Set<CakeCustomization>();

        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
        public DbSet<PaymentStatus> PaymentStatuses => Set<PaymentStatus>();
        public DbSet<Payment> Payments => Set<Payment>();

        public DbSet<ReportLog> ReportLogs => Set<ReportLog>();
        public DbSet<ReportParameter> ReportParameters => Set<ReportParameter>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Role
            modelBuilder.Entity<Role>()
                .HasKey(x => x.RoleId);

            modelBuilder.Entity<Role>()
                .Property(x => x.RoleName)
                .HasMaxLength(100)
                .IsRequired();

            // Company
            modelBuilder.Entity<Company>()
                .HasKey(x => x.CompanyId);

            modelBuilder.Entity<Company>()
                .Property(x => x.CompanyName)
                .HasMaxLength(200)
                .IsRequired();

            // Address
            modelBuilder.Entity<Address>()
                .HasKey(x => x.AddressId);

            // Subscription Plan
            modelBuilder.Entity<SubscriptionPlan>()
                .HasKey(x => x.PlanId);

            modelBuilder.Entity<SubscriptionPlan>()
                .Property(x => x.PlanName)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<SubscriptionPlan>()
                 .Property(x => x.Price)
                 .HasPrecision(18, 2);

            // Subscription Status
            modelBuilder.Entity<SubscriptionStatus>()
                .HasKey(x => x.StatusId);

            modelBuilder.Entity<SubscriptionStatus>()
                .Property(x => x.StatusName)
                .HasMaxLength(50)
                .IsRequired();

            // Subscription
            modelBuilder.Entity<Subscription>()
                .HasKey(x => x.SubscriptionId);

            modelBuilder.Entity<Subscription>()
                .HasOne(x => x.Company)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Subscription>()
                .HasOne(x => x.Plan)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Subscription>()
                .HasOne(x => x.Status)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // System User
            modelBuilder.Entity<SystemUser>()
                .HasKey(x => x.UserId);

            modelBuilder.Entity<SystemUser>()
                .HasOne(x => x.Company)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SystemUser>()
                .HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Customer
            modelBuilder.Entity<Customer>()
                .HasKey(x => x.CustomerId);

            modelBuilder.Entity<Customer>()
                .HasOne(x => x.Company)
                .WithMany(x => x.Customers)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasOne(x => x.Address)
                .WithMany()
                .HasForeignKey(x => x.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasOne(x => x.CreatedByUser)
                .WithMany(x => x.Customers)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Follow-up Status
            modelBuilder.Entity<FollowUpStatus>()
                .HasKey(x => x.StatusId);

            modelBuilder.Entity<FollowUpStatus>()
                .Property(x => x.StatusName)
                .HasMaxLength(50)
                .IsRequired();

            // Customer Follow-up
            modelBuilder.Entity<CustomerFollowUp>()
                .HasKey(x => x.FollowUpId);

            modelBuilder.Entity<CustomerFollowUp>()
                .HasOne(x => x.Customer)
                .WithMany(x => x.FollowUps)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CustomerFollowUp>()
                .HasOne(x => x.StaffUser)
                .WithMany(x => x.FollowUps)
                .HasForeignKey(x => x.StaffUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomerFollowUp>()
                .HasOne(x => x.Status)
                .WithMany(x => x.FollowUps)
                .HasForeignKey(x => x.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order Status
            modelBuilder.Entity<OrderStatus>()
                .HasKey(x => x.StatusId);

            modelBuilder.Entity<OrderStatus>()
                .Property(x => x.StatusName)
                .HasMaxLength(50)
                .IsRequired();


            // Sales Order
            modelBuilder.Entity<SalesOrder>()
                .HasKey(x => x.OrderId);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(x => x.Customer)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(x => x.CreatedByUser)
                .WithMany(x => x.SalesOrders)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(x => x.Status)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sales Order Detail
            modelBuilder.Entity<SalesOrderDetail>()
                .HasKey(x => x.OrderDetailId);

            modelBuilder.Entity<SalesOrderDetail>()
                .HasOne(x => x.Order)
                .WithMany(x => x.OrderDetails)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesOrderDetail>()
                .Property(x => x.UnitPrice)
                 .HasPrecision(18, 2);

            // Cake Customization
            modelBuilder.Entity<CakeCustomization>()
                .HasKey(x => x.CustomizationId);

            modelBuilder.Entity<CakeCustomization>()
                .HasOne(x => x.OrderDetail)
                .WithOne(x => x.CakeCustomization)
                .HasForeignKey<CakeCustomization>(x => x.OrderDetailId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment Method
            modelBuilder.Entity<PaymentMethod>()
                .HasKey(x => x.MethodId);

            modelBuilder.Entity<PaymentMethod>()
                .Property(x => x.MethodName)
                .HasMaxLength(50)
                .IsRequired();

            // Payment Status
            modelBuilder.Entity<PaymentStatus>()
                .HasKey(x => x.StatusId);

            modelBuilder.Entity<PaymentStatus>()
                .Property(x => x.StatusName)
                .HasMaxLength(50)
                .IsRequired();

            // Payment
            modelBuilder.Entity<Payment>()
                .HasKey(x => x.PaymentId);

            modelBuilder.Entity<Payment>()
                .HasOne(x => x.Order)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(x => x.Method)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.MethodId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(x => x.Status)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(x => x.ProcessedByUser)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.ProcessedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .Property(x => x.Amount)
                 .HasPrecision(18, 2);

            // Terms and Conditions
            modelBuilder.Entity<TermsAndConditions>()
                .HasKey(x => x.TermsId);

            modelBuilder.Entity<TermsAcceptance>()
                .HasKey(x => x.AcceptanceId);

            modelBuilder.Entity<TermsAcceptance>()
                .HasOne(x => x.Terms)
                .WithMany(x => x.Acceptances)
                .HasForeignKey(x => x.TermsId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TermsAcceptance>()
                .HasOne(x => x.User)
                .WithMany(x => x.TermsAcceptances)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Audit Log
            modelBuilder.Entity<SystemAuditLog>()
                .HasKey(x => x.LogId);

            modelBuilder.Entity<SystemAuditLog>()
                .HasOne(x => x.User)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Report Log
            modelBuilder.Entity<ReportLog>()
                .HasKey(x => x.ReportId);

            modelBuilder.Entity<ReportLog>()
                .HasOne(x => x.GeneratedByUser)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.GeneratedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Report Parameter
            modelBuilder.Entity<ReportParameter>()
                .HasKey(x => x.ReportParameterId);

            modelBuilder.Entity<ReportParameter>()
                .HasOne(x => x.Report)
                .WithMany(x => x.Parameters)
                .HasForeignKey(x => x.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            // Extended properties for Customer
            modelBuilder.Entity<Customer>()
                .Property(x => x.AddressText)
                .HasMaxLength(500);

            modelBuilder.Entity<Customer>()
                .Property(x => x.CakePreferences)
                .HasMaxLength(500);

            // Extended properties for SalesOrder
            modelBuilder.Entity<SalesOrder>()
                .Property(x => x.CakeSize)
                .HasMaxLength(100);

            modelBuilder.Entity<SalesOrder>()
                .Property(x => x.Flavor)
                .HasMaxLength(100);

            modelBuilder.Entity<SalesOrder>()
                .Property(x => x.DesignTheme)
                .HasMaxLength(200);

            modelBuilder.Entity<SalesOrder>()
                .Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            // Customer Inquiry
            modelBuilder.Entity<CustomerInquiry>()
                .HasKey(x => x.InquiryId);

            modelBuilder.Entity<CustomerInquiry>()
                .Property(x => x.InquiryCode)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<CustomerInquiry>()
                .Property(x => x.CakeType)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<CustomerInquiry>()
                .Property(x => x.AssignedTo)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<CustomerInquiry>()
                .Property(x => x.EstimatedBudget)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CustomerInquiry>()
                .Property(x => x.Status)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<CustomerInquiry>()
                .HasOne(x => x.Customer)
                .WithMany(x => x.Inquiries)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
