USE TaskBoard;
GO
-- Required filter demonstration: user + status + a JSON scalar, ordered by UTC.
DECLARE @UserId INT = (SELECT MIN(Id) FROM dbo.Users);
DECLARE @Status NVARCHAR(20) = N'Pending';
DECLARE @Priority NVARCHAR(20) = N'High';
SELECT t.Id, t.Title, t.AssignedUserId, t.Status, t.CreatedAt,
       ISJSON(t.AdditionalData, OBJECT) AS IsValidObject,
       JSON_VALUE(t.AdditionalData, '$.priority') AS Priority,
       JSON_QUERY(t.AdditionalData, '$.tags') AS Tags,
       JSON_QUERY(t.AdditionalData, '$.metadata') AS Metadata
FROM dbo.Tasks AS t
WHERE t.AssignedUserId = @UserId
  AND t.Status = @Status
  AND JSON_VALUE(t.AdditionalData, '$.priority') = @Priority
ORDER BY t.CreatedAt DESC, t.Id DESC;
GO
-- OPENJSON expands each tag into its own result row. SQL compatibility >= 130.
SELECT t.Id, tag.[value] AS Tag
FROM dbo.Tasks AS t
CROSS APPLY OPENJSON(t.AdditionalData, '$.tags') AS tag
ORDER BY t.Id, tag.[key];
GO
-- Optional JSON_MODIFY demo. Roll back so the demo changes no persistent data.
BEGIN TRANSACTION;
UPDATE dbo.Tasks
SET AdditionalData = JSON_MODIFY(AdditionalData, '$.priority', N'Low')
WHERE Id = (SELECT MIN(Id) FROM dbo.Tasks);
SELECT Id, JSON_VALUE(AdditionalData, '$.priority') AS ChangedPriority
FROM dbo.Tasks WHERE Id = (SELECT MIN(Id) FROM dbo.Tasks);
ROLLBACK TRANSACTION;
GO
