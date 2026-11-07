-- Canonical schema for this assessment. Run on SQL Server 2022 Developer.
-- Re-running keeps existing tables/data; it does not upgrade an older schema.
USE master;
GO
IF DB_ID(N'TaskBoard') IS NULL
    EXEC(N'CREATE DATABASE TaskBoard');
GO
USE TaskBoard;
GO
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        Email NVARCHAR(254) NOT NULL,
        CONSTRAINT CK_Users_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CK_Users_Email_NotBlank CHECK (LEN(LTRIM(RTRIM(Email))) > 0)
    );
END;
GO
IF OBJECT_ID(N'dbo.Tasks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tasks (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tasks PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        AssignedUserId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Tasks_Status DEFAULT N'Pending',
        CreatedAt DATETIME2(3) NOT NULL
            CONSTRAINT DF_Tasks_CreatedAt DEFAULT SYSUTCDATETIME(),
        AdditionalData NVARCHAR(MAX) NOT NULL
            CONSTRAINT DF_Tasks_AdditionalData DEFAULT N'{}',
        CONSTRAINT FK_Tasks_Users FOREIGN KEY (AssignedUserId)
            REFERENCES dbo.Users(Id),
        CONSTRAINT CK_Tasks_Title_NotBlank CHECK (LEN(LTRIM(RTRIM(Title))) > 0),
        CONSTRAINT CK_Tasks_Status CHECK
            (Status COLLATE Latin1_General_100_BIN2 IN
                (N'Pending', N'InProgress', N'Done')),
        CONSTRAINT CK_Tasks_AdditionalData_Json CHECK
            (ISJSON(AdditionalData, OBJECT) = 1)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.Tasks')
               AND name = N'IX_Tasks_AssignedUserId_Status_CreatedAt')
    CREATE INDEX IX_Tasks_AssignedUserId_Status_CreatedAt
        ON dbo.Tasks (AssignedUserId, Status, CreatedAt DESC);
GO
-- The Pending -> Done transition is checked in the service before saving.
-- A row-level CHECK constraint cannot compare old and new row values.
