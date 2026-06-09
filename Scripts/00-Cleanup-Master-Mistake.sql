/*
  Cleanup script for Tabsan EduSphere v1.0.
  Drops the database if it exists so a fresh deployment can start clean.
  
  SCRIPT EXECUTION ORDER:
  0. 00-Cleanup-Master-Mistake.sql   - THIS SCRIPT: drops existing database
  1. 01-Schema-Current.sql           - Creates all tables and schema
  2. 02-Seed-Core.sql                - Seeds core data
  3. 03-FullDummyData.sql            - Comprehensive demo data
  4. 04-Maintenance-Indexes-And-Views.sql - Performance indexes
  5. 05-PostDeployment-Checks.sql    - Data validation
  6. 06-Create-SuperAdmin-User.sql   - Additional SuperAdmin user
  7. 09-Restructure-Sidebar-Menu.sql - Sidebar menu setup
*/

USE [master];
GO

IF DB_ID(N'Tabsan-EduSphere') IS NOT NULL
BEGIN
    ALTER DATABASE [Tabsan-EduSphere] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [Tabsan-EduSphere];
    PRINT 'Database [Tabsan-EduSphere] dropped successfully.';
END
ELSE
BEGIN
    PRINT 'Database [Tabsan-EduSphere] does not exist. No cleanup needed.';
END
GO
