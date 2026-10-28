-- =========================================================================
-- Migration Script: Production Database Migration to .NET 10 Modular Monolith
-- Solution: JewerlySystem
-- Preserves ALL production data: AspNetUsers, Contracts, Payments, Items, 
-- Jewelry, Watches, Persons, Invoices, DailyRecords.
-- =========================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

PRINT 'Starting migration to .NET 10 Modular Monolith...';

-- 1. DROP OBSOLETE IDENTITYSERVER TABLES (Safe: only holds transient tokens/keys)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'DeviceCodes')
BEGIN
    DROP TABLE [DeviceCodes];
    PRINT 'Dropped obsolete IdentityServer table: DeviceCodes';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PersistedGrants')
BEGIN
    DROP TABLE [PersistedGrants];
    PRINT 'Dropped obsolete IdentityServer table: PersistedGrants';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Keys')
BEGIN
    DROP TABLE [Keys];
    PRINT 'Dropped obsolete IdentityServer table: Keys';
END

-- 2. CREATE PASSKEY CREDENTIALS TABLE (FIDO2 / WebAuthn)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PasskeyCredentials')
BEGIN
    CREATE TABLE [PasskeyCredentials] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [CredentialId] varbinary(max) NOT NULL,
        [PublicKey] varbinary(max) NOT NULL,
        [UserHandle] varbinary(max) NOT NULL,
        [SignatureCounter] bigint NOT NULL,
        [CredType] nvarchar(50) NOT NULL,
        [AaGuid] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [LastUsedAt] datetime2 NULL,
        CONSTRAINT [PK_PasskeyCredentials] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_PasskeyCredentials_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_PasskeyCredentials_UserId] ON [PasskeyCredentials] ([UserId]);
    PRINT 'Created table: PasskeyCredentials';
END
ELSE
BEGIN
    PRINT 'Table PasskeyCredentials already exists.';
END

-- 3. ENSURE AUDIT COLUMNS EXIST (CreatedAt, LastModifiedAt)
DECLARE @Tables TABLE (TableName NVARCHAR(100));
INSERT INTO @Tables VALUES 
    ('Contracts'), 
    ('Payments'), 
    ('Items'), 
    ('Persons'), 
    ('DailyRecords'), 
    ('Invoices');

DECLARE @Table NVARCHAR(100);
DECLARE TableCursor CURSOR FOR SELECT TableName FROM @Tables;
OPEN TableCursor;
FETCH NEXT FROM TableCursor INTO @Table;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- CreatedAt
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(@Table) AND name = 'CreatedAt')
    BEGIN
        EXEC('ALTER TABLE [' + @Table + '] ADD [CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME();');
        PRINT 'Added CreatedAt column to ' + @Table;
    END

    -- LastModifiedAt
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(@Table) AND name = 'LastModifiedAt')
    BEGIN
        EXEC('ALTER TABLE [' + @Table + '] ADD [LastModifiedAt] datetime2 NULL;');
        PRINT 'Added LastModifiedAt column to ' + @Table;
    END

    FETCH NEXT FROM TableCursor INTO @Table;
END

CLOSE TableCursor;
DEALLOCATE TableCursor;

PRINT 'Migration completed successfully without data loss.';
COMMIT TRANSACTION;
