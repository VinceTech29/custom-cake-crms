using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Models
{
    internal class Admin : Employee
    {
        private string adminLevel = string.Empty;
        private bool canManageEmployees;
        private bool canManageBusinessInformation;

        // Default Constructor
        public Admin()
        {
        }

        // Constructor
        public Admin(
            int employeeId,
            string firstName,
            string lastName,
            string contactNumber,
            string username,
            string password,
            string position,
            string adminLevel,
            bool canManageEmployees,
            bool canManageBusinessInformation)
            : base(
                employeeId,
                firstName,
                lastName,
                contactNumber,
                username,
                password,
                position)
        {
            this.adminLevel = adminLevel;
            this.canManageEmployees = canManageEmployees;
            this.canManageBusinessInformation = canManageBusinessInformation;
        }

        // Admin Level
        public string getAdminLevel()
        {
            return adminLevel;
        }

        public void setAdminLevel(string adminLevel)
        {
            this.adminLevel = adminLevel;
        }

        // Manage Employees
        public bool getCanManageEmployees()
        {
            return canManageEmployees;
        }

        public void setCanManageEmployees(bool canManageEmployees)
        {
            this.canManageEmployees = canManageEmployees;
        }

        // Manage Business Information
        public bool getCanManageBusinessInformation()
        {
            return canManageBusinessInformation;
        }

        public void setCanManageBusinessInformation(
            bool canManageBusinessInformation)
        {
            this.canManageBusinessInformation = canManageBusinessInformation;
        }
    }
}
