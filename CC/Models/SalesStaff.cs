using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Models
{
    internal class SalesStaff : Employee
    {
        private string salesArea = string.Empty;
        private bool canManageSales;
        private bool canViewInventory;

        // Default Constructor
        public SalesStaff()
        {
        }

        // Constructor
        public SalesStaff(
            int employeeId,
            string firstName,
            string lastName,
            string contactNumber,
            string username,
            string password,
            string position,
            string salesArea,
            bool canManageSales,
            bool canViewInventory)
            : base(
                employeeId,
                firstName,
                lastName,
                contactNumber,
                username,
                password,
                position)
        {
            this.salesArea = salesArea;
            this.canManageSales = canManageSales;
            this.canViewInventory = canViewInventory;
        }

        // Sales Area
        public string getSalesArea()
        {
            return salesArea;
        }

        public void setSalesArea(string salesArea)
        {
            this.salesArea = salesArea;
        }

        // Manage Sales
        public bool getCanManageSales()
        {
            return canManageSales;
        }

        public void setCanManageSales(bool canManageSales)
        {
            this.canManageSales = canManageSales;
        }

        // View Inventory
        public bool getCanViewInventory()
        {
            return canViewInventory;
        }

        public void setCanViewInventory(bool canViewInventory)
        {
            this.canViewInventory = canViewInventory;
        }
    }
}
