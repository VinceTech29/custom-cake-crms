using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Models
{
    internal class Employee
    {
        private int employeeId;
        private string firstName = string.Empty;
        private string lastName = string.Empty;
        private string contactNumber = string.Empty;
        private string username = string.Empty;
        private string password = string.Empty;
        private string position = string.Empty;

        // Default Constructor
        public Employee()
        {
        }

        // Constructor
        public Employee(
            int employeeId,
            string firstName,
            string lastName,
            string contactNumber,
            string username,
            string password,
            string position)
        {
            this.employeeId = employeeId;
            this.firstName = firstName;
            this.lastName = lastName;
            this.contactNumber = contactNumber;
            this.username = username;
            this.password = password;
            this.position = position;
        }

        // Employee ID
        public int getEmployeeId()
        {
            return employeeId;
        }

        public void setEmployeeId(int employeeId)
        {
            this.employeeId = employeeId;
        }

        // First Name
        public string getFirstName()
        {
            return firstName;
        }

        public void setFirstName(string firstName)
        {
            this.firstName = firstName;
        }

        // Last Name
        public string getLastName()
        {
            return lastName;
        }

        public void setLastName(string lastName)
        {
            this.lastName = lastName;
        }

        // Contact Number
        public string getContactNumber()
        {
            return contactNumber;
        }

        public void setContactNumber(string contactNumber)
        {
            this.contactNumber = contactNumber;
        }

        // Username
        public string getUsername()
        {
            return username;
        }

        public void setUsername(string username)
        {
            this.username = username;
        }

        // Password
        public string getPassword()
        {
            return password;
        }

        public void setPassword(string password)
        {
            this.password = password;
        }

        // Position
        public string getPosition()
        {
            return position;
        }

        public void setPosition(string position)
        {
            this.position = position;
        }
    }
}
