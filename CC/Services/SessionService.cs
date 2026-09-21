using System.Collections.Generic;
using CC.Models;

namespace CC.Services
{
    public static class SessionService
    {
        public static CurrentUser? CurrentUser { get; set; }

        // In-memory stores for UI testing
        public static List<Customer> Customers { get; } = new List<Customer>();
        public static List<SalesOrder> Orders { get; } = new List<SalesOrder>();
        public static List<Payment> Payments { get; } = new List<Payment>();
        public static List<CustomerFollowUp> FollowUps { get; } = new List<CustomerFollowUp>();
        public static List<CustomerInquiry> Inquiries { get; } = new List<CustomerInquiry>();

        public static void EnsureSeedData()
        {
            if (!Customers.Any())
            {
                Customers.AddRange(new[]
                {
                    new Customer { CustomerId = 1, FirstName = "Maria",   LastName = "Santos",   Email = "maria.santos@email.com", Phone = "+63 917 234 5678", RegisteredDate = new System.DateTime(2023, 3, 15) },
                    new Customer { CustomerId = 2, FirstName = "Jose",    LastName = "Reyes",    Email = "jose.reyes@gmail.com",   Phone = "+63 918 456 7890", RegisteredDate = new System.DateTime(2024, 1, 20) },
                    new Customer { CustomerId = 3, FirstName = "Ana",     LastName = "Lim",      Email = "analim@yahoo.com",       Phone = "+63 920 111 2233", RegisteredDate = new System.DateTime(2024, 6, 5)  },
                    new Customer { CustomerId = 4, FirstName = "Roberto", LastName = "Cruz",     Email = "rcruz@outlook.com",      Phone = "+63 915 789 0123", RegisteredDate = new System.DateTime(2025, 2, 10) },
                    new Customer { CustomerId = 5, FirstName = "Carla",   LastName = "Mendoza",  Email = "carla.m@email.com",      Phone = "+63 919 345 6789", RegisteredDate = new System.DateTime(2023, 9, 12) },
                    new Customer { CustomerId = 6, FirstName = "Patricia",LastName = "Tan",      Email = "ptan@gmail.com",         Phone = "+63 921 567 8901", RegisteredDate = new System.DateTime(2022, 11, 8)  },
                    new Customer { CustomerId = 7, FirstName = "Michael", LastName = "Gonzales", Email = "mgonzales@company.ph",   Phone = "+63 916 234 5678", RegisteredDate = new System.DateTime(2025, 5, 18) },
                });
            }

            if (!Orders.Any())
            {
                var customers = Customers;
                Orders.AddRange(new[]
                {
                    new SalesOrder
                    {
                        OrderId = 45,
                        CustomerId = 6,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 6),
                        OrderDate = new System.DateTime(2026, 8, 28),
                        DeliveryDate = new System.DateTime(2026, 8, 27),
                        StatusId = 2, // Processing
                        TotalAmount = 6800m,
                        DesignTheme = "Watercolor pastel with baby blue...",
                        OrderDetails = new List<SalesOrderDetail>
                        {
                            new SalesOrderDetail { ItemDescription = "Watercolor pastel with baby blue...", Quantity = 1, UnitPrice = 6800m }
                        }
                    },
                    new SalesOrder
                    {
                        OrderId = 44,
                        CustomerId = 1,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 1),
                        OrderDate = new System.DateTime(2026, 8, 22),
                        DeliveryDate = new System.DateTime(2026, 8, 21),
                        StatusId = 3, // Completed
                        TotalAmount = 2800m,
                        DesignTheme = "Minimalist white with dried flow...",
                        OrderDetails = new List<SalesOrderDetail>
                        {
                            new SalesOrderDetail { ItemDescription = "Minimalist white with dried flow...", Quantity = 1, UnitPrice = 2800m }
                        }
                    },
                    new SalesOrder
                    {
                        OrderId = 43,
                        CustomerId = 2,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 2),
                        OrderDate = new System.DateTime(2026, 9, 5),
                        DeliveryDate = new System.DateTime(2026, 9, 4),
                        StatusId = 1, // Confirmed
                        TotalAmount = 3500m,
                        DesignTheme = "Corporate logo print, navy butte...",
                        OrderDetails = new List<SalesOrderDetail>
                        {
                            new SalesOrderDetail { ItemDescription = "Corporate logo print, navy butte...", Quantity = 1, UnitPrice = 3500m }
                        }
                    },
                    new SalesOrder
                    {
                        OrderId = 42,
                        CustomerId = 7,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 7),
                        OrderDate = new System.DateTime(2026, 8, 18),
                        DeliveryDate = new System.DateTime(2026, 8, 17),
                        StatusId = 3, // Completed
                        TotalAmount = 4200m,
                        DesignTheme = "Boho floral, eucalyptus and rose...",
                        OrderDetails = new List<SalesOrderDetail>
                        {
                            new SalesOrderDetail { ItemDescription = "Boho floral, eucalyptus and rose...", Quantity = 1, UnitPrice = 4200m }
                        }
                    },
                    new SalesOrder
                    {
                        OrderId = 41,
                        CustomerId = 3,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 3),
                        OrderDate = new System.DateTime(2026, 8, 10),
                        DeliveryDate = new System.DateTime(2026, 8, 9),
                        StatusId = 5, // Cancelled
                        TotalAmount = 2200m,
                        DesignTheme = "Black and gold, elegant minimal",
                        OrderDetails = new List<SalesOrderDetail>
                        {
                            new SalesOrderDetail { ItemDescription = "Black and gold, elegant minimal", Quantity = 1, UnitPrice = 2200m }
                        }
                    },
                    new SalesOrder
                    {
                        OrderId = 40,
                        CustomerId = 6,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 6),
                        OrderDate = new System.DateTime(2026, 7, 30),
                        DeliveryDate = new System.DateTime(2026, 7, 29),
                        StatusId = 3, // Completed
                        TotalAmount = 9800m,
                        DesignTheme = "Watercolor blue-green ombre, gol...",
                        OrderDetails = new List<SalesOrderDetail>
                        {
                            new SalesOrderDetail { ItemDescription = "Watercolor blue-green ombre, gol...", Quantity = 1, UnitPrice = 9800m }
                        }
                    }
                });
            }

            if (!Payments.Any())
            {
                Payments.AddRange(new[]
                {
                    new Payment { PaymentId = 1, OrderId = 45, Order = Orders.FirstOrDefault(o => o.OrderId == 45), Amount = 3000m, MethodId = 2, StatusId = 1, PaymentDate = System.DateTime.Now.AddDays(-4), TransactionReference = "GCASH-991203" },
                    new Payment { PaymentId = 2, OrderId = 44, Order = Orders.FirstOrDefault(o => o.OrderId == 44), Amount = 2800m, MethodId = 3, StatusId = 2, PaymentDate = System.DateTime.Now.AddDays(-3), TransactionReference = "BDO-883019" },
                    new Payment { PaymentId = 3, OrderId = 43, Order = Orders.FirstOrDefault(o => o.OrderId == 43), Amount = 1850m, MethodId = 1, StatusId = 2, PaymentDate = System.DateTime.Now.AddDays(-8), TransactionReference = "CASH-1003" },
                    new Payment { PaymentId = 4, OrderId = 42, Order = Orders.FirstOrDefault(o => o.OrderId == 42), Amount = 4200m, MethodId = 2, StatusId = 2, PaymentDate = System.DateTime.Now.AddDays(-60), TransactionReference = "GCASH-440192" },
                });
            }

            if (!FollowUps.Any())
            {
                var customers = Customers;
                FollowUps.AddRange(new[]
                {
                    new CustomerFollowUp { FollowUpId = 1, CustomerId = 1, Customer = customers.FirstOrDefault(c => c.CustomerId == 1), StaffUserId = CurrentUser?.UserId ?? 1, StatusId = 0, FollowUpDate = System.DateTime.Now.AddDays(-1), NextFollowUpDate = System.DateTime.Now.AddDays(2), Notes = "Confirm cake flavor choice and dietary restrictions" },
                    new CustomerFollowUp { FollowUpId = 2, CustomerId = 2, Customer = customers.FirstOrDefault(c => c.CustomerId == 2), StaffUserId = CurrentUser?.UserId ?? 1, StatusId = 0, FollowUpDate = System.DateTime.Now.AddDays(1), NextFollowUpDate = System.DateTime.Now.AddDays(3), Notes = "Send design draft for approval" },
                    new CustomerFollowUp { FollowUpId = 3, CustomerId = 3, Customer = customers.FirstOrDefault(c => c.CustomerId == 3), StaffUserId = CurrentUser?.UserId ?? 1, StatusId = 1, FollowUpDate = System.DateTime.Now.AddDays(-4), NextFollowUpDate = null, Notes = "Payment reminder sent - payment received" },
                    new CustomerFollowUp { FollowUpId = 4, CustomerId = 4, Customer = customers.FirstOrDefault(c => c.CustomerId == 4), StaffUserId = CurrentUser?.UserId ?? 1, StatusId = 0, FollowUpDate = System.DateTime.Now.AddDays(2), NextFollowUpDate = System.DateTime.Now.AddDays(4), Notes = "Inquire about event venue address and delivery window" },
                    new CustomerFollowUp { FollowUpId = 5, CustomerId = 5, Customer = customers.FirstOrDefault(c => c.CustomerId == 5), StaffUserId = CurrentUser?.UserId ?? 1, StatusId = 0, FollowUpDate = System.DateTime.Now.AddDays(3), NextFollowUpDate = System.DateTime.Now.AddDays(5), Notes = "Confirm final payment for Christmas order" },
                    new CustomerFollowUp { FollowUpId = 6, CustomerId = 6, Customer = customers.FirstOrDefault(c => c.CustomerId == 6), StaffUserId = CurrentUser?.UserId ?? 1, StatusId = 1, FollowUpDate = System.DateTime.Now.AddDays(-7), NextFollowUpDate = null, Notes = "Wedding cake design approved and finalized" },
                    new CustomerFollowUp { FollowUpId = 7, CustomerId = 7, Customer = customers.FirstOrDefault(c => c.CustomerId == 7), StaffUserId = CurrentUser?.UserId ?? 1, StatusId = 0, FollowUpDate = System.DateTime.Now.AddDays(4), NextFollowUpDate = System.DateTime.Now.AddDays(6), Notes = "Coordinate delivery address for corporate event" },
                });
            }

            if (!Inquiries.Any())
            {
                var customers = Customers;
                Inquiries.AddRange(new[]
                {
                    new CustomerInquiry
                    {
                        InquiryId = 89,
                        InquiryCode = "INQ-2026-0089",
                        CustomerId = 1,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 1),
                        CakeType = "3-tier wedding cake, fondant wi...",
                        EventDate = new System.DateTime(2026, 9, 20),
                        AssignedTo = "Staff - Lea R.",
                        Status = "Quoted",
                        EstimatedBudget = 8500m,
                        Notes = "3-tier wedding cake with floral pastel accents and edible pearls."
                    },
                    new CustomerInquiry
                    {
                        InquiryId = 90,
                        InquiryCode = "INQ-2026-0090",
                        CustomerId = 2,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 2),
                        CakeType = "Corporate logo cake for compa...",
                        EventDate = new System.DateTime(2026, 9, 5),
                        AssignedTo = "Staff - Mark P.",
                        Status = "Approved",
                        EstimatedBudget = 5000m,
                        Notes = "Company anniversary cake with edible printed logo."
                    },
                    new CustomerInquiry
                    {
                        InquiryId = 91,
                        InquiryCode = "INQ-2026-0091",
                        CustomerId = 3,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 3),
                        CakeType = "Birthday drip cake",
                        EventDate = new System.DateTime(2026, 9, 10),
                        AssignedTo = "Staff - Lea R.",
                        Status = "In Progress",
                        EstimatedBudget = 3200m,
                        Notes = "Chocolate drip cake with macarons and fresh strawberries."
                    },
                    new CustomerInquiry
                    {
                        InquiryId = 92,
                        InquiryCode = "INQ-2026-0092",
                        CustomerId = 7,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 7),
                        CakeType = "Themed Halloween corporate e...",
                        EventDate = new System.DateTime(2026, 10, 30),
                        AssignedTo = "Unassigned",
                        Status = "New",
                        EstimatedBudget = 6000m,
                        Notes = "Spooky theme corporate party cupcakes and centerpiece cake."
                    },
                    new CustomerInquiry
                    {
                        InquiryId = 88,
                        InquiryCode = "INQ-2026-0088",
                        CustomerId = 6,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 6),
                        CakeType = "Christening cake, watercolor style",
                        EventDate = new System.DateTime(2026, 8, 28),
                        AssignedTo = "Staff - Mark P.",
                        Status = "Converted",
                        EstimatedBudget = 6800m,
                        Notes = "Pastel blue and white watercolor effect for baby dedication."
                    },
                    new CustomerInquiry
                    {
                        InquiryId = 85,
                        InquiryCode = "INQ-2026-0085",
                        CustomerId = 4,
                        Customer = customers.FirstOrDefault(c => c.CustomerId == 4),
                        CakeType = "Debut cake",
                        EventDate = new System.DateTime(2026, 8, 15),
                        AssignedTo = "Staff - Lea R.",
                        Status = "Closed",
                        EstimatedBudget = 7500m,
                        Notes = "Client decided on catering dessert package."
                    }
                });
            }
        }
    }

    public class CurrentUser
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? TenantServer { get; set; }
        public string? TenantDatabase { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
