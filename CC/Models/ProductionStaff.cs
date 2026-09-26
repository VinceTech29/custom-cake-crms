using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Models
{
    internal class ProductionStaff : Employee
    {
        private string productionArea = string.Empty;
        private bool canManageProduction;
        private bool canManageInventory;

        
        public ProductionStaff()
        {
        }

        
        public ProductionStaff(
            int employeeId,
            string firstName,
            string lastName,
            string contactNumber,
            string username,
            string password,
            string position,
            string productionArea,
            bool canManageProduction,
            bool canManageInventory)
            : base(
                employeeId,
                firstName,
                lastName,
                contactNumber,
                username,
                password,
                position)
        {
            this.productionArea = productionArea;
            this.canManageProduction = canManageProduction;
            this.canManageInventory = canManageInventory;
        }

        // Production Area
        public string getProductionArea()
        {
            return productionArea;
        }

        public void setProductionArea(string productionArea)
        {
            this.productionArea = productionArea;
        }

        // Manage Production
        public bool getCanManageProduction()
        {
            return canManageProduction;
        }

        public void setCanManageProduction(bool canManageProduction)
        {
            this.canManageProduction = canManageProduction;
        }

        // Manage Inventory
        public bool getCanManageInventory()
        {
            return canManageInventory;
        }

        public void setCanManageInventory(bool canManageInventory)
        {
            this.canManageInventory = canManageInventory;
        }
    }
}
