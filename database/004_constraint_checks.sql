USE TaskBoard;
GO
-- Development checks. Invalid writes should fail; every change is rolled back.
SET XACT_ABORT OFF;
BEGIN TRANSACTION;
DECLARE @UserId INT = (SELECT MIN(Id) FROM dbo.Users);
IF @UserId IS NULL
    THROW 51000, 'Run 002_seed.sql before these checks.', 1;

BEGIN TRY
    INSERT dbo.Tasks (Title, AssignedUserId, AdditionalData)
    VALUES (N'Invalid JSON probe', @UserId, N'{broken');
    THROW 51000, 'FAIL: invalid JSON was accepted.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 547 THROW;
    PRINT 'PASS: invalid JSON rejected.';
END CATCH;

BEGIN TRY
    INSERT dbo.Tasks (Title, AssignedUserId, AdditionalData)
    VALUES (N'Array probe', @UserId, N'[]');
    THROW 51000, 'FAIL: non-object JSON was accepted.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 547 THROW;
    PRINT 'PASS: JSON array rejected by object constraint.';
END CATCH;

BEGIN TRY
    INSERT dbo.Tasks (Title, AssignedUserId)
    VALUES (N'Orphan probe', -1);
    THROW 51000, 'FAIL: orphan task was accepted.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 547 THROW;
    PRINT 'PASS: missing user rejected.';
END CATCH;
ROLLBACK TRANSACTION;
GO
