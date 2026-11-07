USE TaskBoard;
GO
-- Synthetic demo data only. Safe to re-run without duplicating these records.
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'cristian@example.test')
    INSERT dbo.Users (Name, Email)
    VALUES (N'Cristian Demo', N'cristian@example.test');
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'alex@example.test')
    INSERT dbo.Users (Name, Email)
    VALUES (N'Alex Demo', N'alex@example.test');
DECLARE @DemoUserId INT =
    (SELECT MIN(Id) FROM dbo.Users WHERE Email = N'cristian@example.test');
IF NOT EXISTS (SELECT 1 FROM dbo.Tasks WHERE Title = N'Demonstrate SQL JSON')
    INSERT dbo.Tasks (Title, AssignedUserId, AdditionalData)
    VALUES (N'Demonstrate SQL JSON', @DemoUserId,
      N'{"priority":"High","dueDate":"2026-11-10","tags":["demo","sql"],"metadata":{"source":"assessment"}}');
GO
