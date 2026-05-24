-- Group C / International Bookstore — create database (reference only).
-- The app also creates GroupC_DB on startup via DatabaseInitializer.EnsureDatabaseExists.
-- Run against (localdb)\MSSQLLocalDB (or your configured server) in a New Query window.

IF DB_ID(N'GroupC_DB') IS NULL
BEGIN
    CREATE DATABASE [GroupC_DB];
END;
GO
