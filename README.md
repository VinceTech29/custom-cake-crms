# CC Custom Cake CRM

A multi-tenant **Customer Relationship Management (CRM) system** designed for custom cake businesses. The system centralizes customer records, orders, payments, follow-ups, subscriptions, reporting, and business management through a role-based desktop application.

## Overview

**CC Custom Cake CRM** is built to help custom cake businesses manage customer relationships and daily transactions in one centralized system.

The system follows a **database-per-tenant architecture**, where each registered business has its own operational database while platform-level information is managed through a central Master Database.

The system is designed around the following workflow:

```text
Customer Inquiry
      ↓
Quotation
      ↓
Order Confirmation
      ↓
Payment
      ↓
Order Processing
      ↓
Completed
      ↓
Follow-Up / Feedback
