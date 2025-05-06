# Boola POS - Jewelry Shop Management System

![Boola Logo](Images/BOOLA%20LOGO.png)

**Version 1.0.0**  
**Last Updated: May 6, 2025**

## Table of Contents

1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
   - [System Requirements](#system-requirements)
   - [Installation](#installation)
   - [Login](#login)
3. [Dashboard](#dashboard)
4. [Customer Management](#customer-management)
   - [Adding New Customers](#adding-new-customers)
   - [Editing Customer Information](#editing-customer-information)
   - [Searching for Customers](#searching-for-customers)
5. [Product Management](#product-management)
   - [Adding New Products](#adding-new-products)
   - [Managing Inventory](#managing-inventory)
   - [HUID Tracking](#huid-tracking)
   - [Categories and Subcategories](#categories-and-subcategories)
6. [Stock Management](#stock-management)
   - [Stock Overview](#stock-overview)
   - [Stock Ledger Entries](#stock-ledger-entries)
   - [Stock Valuation](#stock-valuation)
7. [Sales and Orders](#sales-and-orders)
   - [Creating New Orders](#creating-new-orders)
   - [Order Processing](#order-processing)
   - [Order History](#order-history)
8. [Suppliers](#suppliers)
   - [Managing Suppliers](#managing-suppliers)
   - [Purchase Orders](#purchase-orders)
9. [Rate Master](#rate-master)
   - [Metal Rates Management](#metal-rates-management)
   - [Rate History](#rate-history)
10. [Repair Jobs](#repair-jobs)
    - [Creating Repair Tickets](#creating-repair-tickets)
    - [Tracking Repair Status](#tracking-repair-status)
    - [Delivery and Billing](#delivery-and-billing)
11. [Financial Management](#financial-management)
    - [Transactions](#transactions)
    - [EMI Management](#emi-management)
    - [GST Compliance](#gst-compliance)
12. [Reporting](#reporting)
    - [Sales Reports](#sales-reports)
    - [Inventory Reports](#inventory-reports)
    - [Financial Reports](#financial-reports)
    - [Custom Reports](#custom-reports)
13. [User Management](#user-management)
    - [User Roles and Permissions](#user-roles-and-permissions)
    - [Adding/Editing Users](#adding-editing-users)
14. [System Settings](#system-settings)
    - [Business Information](#business-information)
    - [Email Settings](#email-settings)
    - [Backup and Restore](#backup-and-restore)
15. [Troubleshooting](#troubleshooting)
    - [Common Issues](#common-issues)
    - [Support Contact](#support-contact)

## Introduction

Boola POS is a comprehensive jewelry shop management system designed specifically for jewelers. The application helps manage inventory, sales, customers, repairs, and business operations efficiently. This guide will walk you through all the features of the system to help you make the most of it.

## Getting Started

### System Requirements

- Windows 10 or higher
- 4GB RAM (8GB recommended)
- 1GB available disk space
- 1366x768 screen resolution or higher
- Internet connection (for updates and some features)

### Installation

1. Run the setup file `BoolaPOS_Setup.exe`
2. Follow the on-screen instructions
3. Once installed, the application will create a desktop shortcut
4. Launch the application from the desktop shortcut or start menu

### Login

1. When you first start the application, you'll be presented with a login screen
2. The default administrator credentials are:
   - Username: admin
   - Password: admin123
3. **Important**: Change the default password immediately after first login for security

## Dashboard

The dashboard provides an overview of your business at a glance, including:

- Today's sales summary
- Recent orders
- Low stock alerts
- Upcoming repair job deliveries
- Daily rate chart

Click on any card on the dashboard to navigate to the relevant section for more details.

## Customer Management

### Adding New Customers

1. Navigate to the **Customer** section from the main menu
2. Click on **Add New Customer**
3. Fill in the required information:
   - Name
   - Contact information (Phone, Email, Address)
   - ID proof (optional)
   - Customer notes (preferences, special requirements)
4. Click **Save** to add the customer to the database

### Editing Customer Information

1. In the Customer section, find the customer you want to edit
2. Click on the **Edit** icon (pencil) next to their name
3. Update the information as needed
4. Click **Save** to confirm changes

### Searching for Customers

The search bar at the top of the Customer section allows you to quickly find customers by:
- Name
- Phone number
- Email
- Customer ID

## Product Management

### Adding New Products

1. Navigate to the **Product** section
2. Click on **Add New Product**
3. Enter product details:
   - Product name
   - Category and subcategory
   - Weight
   - Making charges
   - Metal type
   - Purity
   - HUID (if applicable)
   - Stone details (if applicable)
   - Purchase price
   - Selling price
   - Barcode/SKU
   - Image (optional)
4. Click **Save** to add the product to inventory

### Managing Inventory

1. View all inventory items in the Product section
2. Filter products by category, metal type, or price range
3. Edit product details by clicking the Edit icon
4. Mark products as inactive instead of deleting them to maintain history

### HUID Tracking

1. For hallmarked jewelry, enter the HUID (Hallmarking Unique ID)
2. The system will maintain a complete history of the item with HUID tracking
3. Use the HUID search feature to quickly find specific items

### Categories and Subcategories

1. Go to the **Categories** section under Product
2. Add, edit, or manage product categories and subcategories
3. Assign categories to products for better organization and reporting

## Stock Management

### Stock Overview

1. The Stock section provides a complete view of your inventory
2. See total items, value, and breakdown by category
3. View items that need reordering based on minimum stock levels

### Stock Ledger Entries

1. Every stock movement is recorded in the Stock Ledger
2. View entries filtered by date range, product, or transaction type
3. Export stock movement reports as needed

### Stock Valuation

1. Get real-time valuation of your stock
2. View valuation by metal type, weight, or category
3. Track stock value changes over time

## Sales and Orders

### Creating New Orders

1. Navigate to the **Order** section
2. Click **New Order**
3. Select a customer or add a new one
4. Add products to the order from inventory
5. Apply discounts if applicable
6. Select payment method(s)
7. Finalize the order

### Order Processing

1. Process payments through the system
2. Generate invoice and receipts
3. Print or email receipts to customers
4. Track order status (Pending, Completed, Cancelled)

### Order History

1. View all past orders in the Order History section
2. Filter by date, customer, or order status
3. Reprint or email receipts for past orders
4. View order details including products sold and payment information

## Suppliers

### Managing Suppliers

1. Navigate to the **Supplier** section
2. Add new suppliers with contact information and terms
3. Edit existing supplier details
4. Track purchase history by supplier

### Purchase Orders

1. Create purchase orders for suppliers
2. Track outstanding and fulfilled purchase orders
3. Receive inventory against purchase orders
4. Maintain supplier payment records

## Rate Master

### Metal Rates Management

1. Navigate to the **Rate Master** section
2. Update daily metal rates (Gold, Silver, Platinum)
3. Set rates by purity (24K, 22K, 18K, etc.)
4. Apply rate changes to inventory valuation

### Rate History

1. View historical metal rates
2. Track rate trends with built-in charts
3. Export rate history reports

## Repair Jobs

### Creating Repair Tickets

1. Go to the **Repair Job** section
2. Click **New Repair Job**
3. Select customer or add a new one
4. Enter item details and repair requirements
5. Specify estimated completion date
6. Set repair charges
7. Issue receipt to customer

### Tracking Repair Status

1. Update repair status (Received, In Progress, Ready for Delivery)
2. Add notes to repair jobs
3. Send status updates to customers via SMS or email

### Delivery and Billing

1. Process delivery when repair is complete
2. Generate final bill and collect payment
3. Update repair job status to Delivered
4. Maintain complete repair history for future reference

## Financial Management

### Transactions

1. All financial transactions are recorded in the system
2. View daily, weekly, or monthly transaction reports
3. Filter transactions by type, payment method, or date range

### EMI Management

1. Set up EMI plans for eligible purchases
2. Track EMI payments and schedules
3. Send payment reminders to customers
4. Handle EMI completion and closure

### GST Compliance

1. The system automatically calculates applicable GST
2. Generate GST reports for tax filing
3. Maintain compliance with current GST regulations
4. Export GST data for your accountant

## Reporting

### Sales Reports

1. Generate daily, weekly, monthly, or custom period sales reports
2. View sales by product category, staff member, or payment method
3. Analyze sales trends with built-in charts

### Inventory Reports

1. Stock valuation reports
2. Slow-moving inventory identification
3. Stock aging analysis
4. Category-wise inventory reports

### Financial Reports

1. Profit and loss statements
2. Revenue reports
3. Tax reports
4. Payment method breakdown

### Custom Reports

1. Build custom reports based on specific criteria
2. Save report templates for future use
3. Schedule automatic report generation
4. Export reports in various formats (PDF, Excel, CSV)

## User Management

### User Roles and Permissions

1. Navigate to the **User** section
2. Configure user roles (Administrator, Manager, Sales Staff, etc.)
3. Set permissions for each role
4. Control access to sensitive areas of the system

### Adding/Editing Users

1. Add new users and assign roles
2. Edit existing user details and permissions
3. Deactivate users when needed
4. Reset user passwords

## System Settings

### Business Information

1. Go to **Settings** > **Business Information**
2. Update your business details
3. Configure invoice header and footer
4. Set up logo and branding elements

### Email Settings

1. Configure email settings for notifications
2. Set up email templates for various communications
3. Test email functionality

### Backup and Restore

1. Configure automatic backup schedule
2. Perform manual backups when needed
3. Restore from backup if necessary
4. Verify backup integrity periodically

## Troubleshooting

### Common Issues

1. **Login Issues**: Reset your password or contact the administrator
2. **Slow Performance**: Check system requirements and database size
3. **Printing Problems**: Verify printer connections and settings
4. **Data Synchronization**: Ensure proper network connectivity

### Support Contact

For additional support:
- Email: support@boolapos.com
- Phone: +94-XX-XXXXXXX
- Website: www.boolapos.com/support

---

© 2025 Boola POS. All rights reserved.