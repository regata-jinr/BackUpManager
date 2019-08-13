USE [testdb]
GO

INSERT INTO [BackUpTest]
	(dt1)
VALUES
	(CONVERT(date, getdate()))
GO

USE [testdb]
GO

SELECT *
FROM [BackUpTest]
GO