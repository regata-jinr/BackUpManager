USE master;
USE testdb;
INSERT INTO BackUpTestTable	(dt1) VALUES (CONVERT(date, getdate()));
DECLARE @FileName varchar(100) SELECT @FileName = (SELECT 'C:\\Program Files\\Microsoft SQL Server\\MSSQL14.REGATALOCAL\\MSSQL\\Backup\\testdb-' + convert(varchar(10),getdate(),104) + '.bak') BACKUP DATABASE [testdb] TO DISK = @FileName;