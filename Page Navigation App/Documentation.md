# Boola POS - Jewelry Shop Management System

![Boola Logo](Images/BOOLA%20LOGO.png)

**Version 1.0.0**  
**Last Updated: May 7, 2025**

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
16. [Developer Guidelines](#developer-guidelines)
    - [Code Architecture](#code-architecture)
    - [Coding Standards](#coding-standards)
    - [GitHub Copilot Guidelines](#github-copilot-guidelines)
    - [Dependency Management](#dependency-management)
    - [Testing Guidelines](#testing-guidelines)

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

## Developer Guidelines

This section outlines the development standards and architecture guidelines for maintaining and extending the Boola POS system. Following these standards will ensure consistent, maintainable, and high-quality code, especially when using GitHub Copilot for code generation.

### Code Architecture

Boola POS follows the MVVM (Model-View-ViewModel) architecture pattern, which is standard for WPF applications:

1. **Model Layer**
   - Location: `/Model` directory
   - Purpose: Represents the business data and business logic
   - Guidelines:
     - Models should be simple POCOs with properties and minimal business logic
     - Use data annotations for validation where appropriate
     - Avoid UI-specific code in models
     - Keep models serializable for data persistence

2. **View Layer**
   - Location: `/View` directory
   - Purpose: Represents the UI elements the user interacts with
   - Guidelines:
     - XAML files should focus on presentation only
     - Code-behind files should be minimal, containing only view-specific logic
     - Use data binding to connect to ViewModels
     - Implement UI consistency using styles and templates from `/Resources`

3. **ViewModel Layer**
   - Location: `/ViewModel` directory
   - Purpose: Acts as a bridge between the Model and View
   - Guidelines:
     - Implement `INotifyPropertyChanged` for data binding
     - Use commands for user interactions
     - Keep business logic separate from UI logic
     - Maintain a clear 1:1 relationship with Views where possible

4. **Data Access Layer**
   - Location: `/Data` directory
   - Purpose: Manages database operations using Entity Framework Core
   - Guidelines:
     - Use the Repository pattern for data access
     - Keep database context configurations in one place
     - Use migrations for database schema updates
     - Implement proper error handling for database operations

5. **Services Layer**
   - Location: `/Services` directory
   - Purpose: Provides reusable functionality across the application
   - Guidelines:
     - Implement the interface-implementation pattern
     - Services should be stateless where possible
     - Use dependency injection for service resolution

### Coding Standards

1. **Naming Conventions**
   - Use PascalCase for class names, properties, and methods
   - Use camelCase for local variables and parameters
   - Prefix private fields with underscore (_)
   - Use descriptive names that indicate purpose
   - Avoid abbreviations unless widely recognized

2. **Code Style**
   - Use 4 spaces for indentation (not tabs)
   - Keep lines under 100 characters when possible
   - Use blank lines to separate logical blocks of code
   - Place braces on new lines
   - Group using statements by namespace

3. **UI and Resource Management**
   - **ALWAYS use MahApps resources at any cost**
   - **STRICTLY avoid local resources**
   - Use MahApps.Metro controls and themes for consistent UI
   - Reference MahApps resource dictionaries in App.xaml
   - For custom styling, extend MahApps styles rather than creating from scratch
   - Use MahApps IconPacks for application icons and glyphs

4. **Comments**
   - Write XML documentation for public methods and classes
   - Add summary comments for complex algorithms
   - Don't comment obvious code
   - Keep comments up-to-date with code changes
   - Use TODO comments for future improvements (format: `// TODO: Action item`)

5. **Error Handling**
   - Use try-catch blocks for anticipated exceptions
   - Log exceptions with appropriate severity levels
   - Provide user-friendly error messages
   - Avoid catching generic exceptions without specific handling
   - Use async/await pattern properly with exception handling

### GitHub Copilot Guidelines

When using GitHub Copilot for code generation, follow these guidelines:

1. **Prompt Construction**
   - Provide clear, specific instructions about the desired functionality
   - Specify which architectural layer the code belongs to
   - Mention any existing patterns or conventions to follow
   - Include requirements for error handling and validation

2. **Code Review**
   - Always review Copilot-generated code before committing
   - Ensure generated code follows the project's architecture patterns
   - Check for proper error handling and edge cases
   - Verify proper implementation of interfaces and inheritance

3. **Preferred Patterns**
   - Direct Copilot to use the MVVM pattern for UI components
   - Request implementation of INotifyPropertyChanged for ViewModels
   - Ask for Command pattern implementations for user actions
   - Prefer async/await over direct threading

4. **Integration Guidelines**
   - Have Copilot generate unit tests alongside implementation code
   - Request documentation comments in XML format
   - Ensure generated code respects existing namespaces
   - Validate database operations follow EF Core best practices

### Dependency Management

1. **NuGet Packages**
   - Keep packages updated to their latest stable versions
   - Document package purpose in comments or commit messages
   - Favor well-maintained packages with active communities
   - Avoid packages with excessive dependencies

2. **Version Control**
   - Commit package changes separately from code changes
   - Include package version updates in release notes
   - Pin versions in production to avoid unexpected updates

### Testing Guidelines

1. **Unit Testing**
   - Write unit tests for all business logic
   - Use MSTest for testing framework
   - Follow AAA pattern (Arrange, Act, Assert)
   - Mock external dependencies

2. **UI Testing**
   - Test XAML bindings and UI interactions
   - Verify UI behavior across different window sizes
   - Test accessibility features

3. **Integration Testing**
   - Test database operations with test database
   - Verify service integrations work correctly
   - Test complete user workflows end-to-end

---

© 2025 Boola POS. All rights reserved.