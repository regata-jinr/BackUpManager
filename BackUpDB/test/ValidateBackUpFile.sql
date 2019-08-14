use master;
ALTER DATABASE [testdb] SET OFFLINE WITH ROLLBACK IMMEDIATE;
DECLARE @FileName varchar(100) SELECT @FileName = (SELECT 'C:\\Program Files\\Microsoft SQL Server\\MSSQL14.REGATALOCAL\\MSSQL\\Backup\\testdb-' + convert(varchar(10),getdate(),104) + '.bak') RESTORE DATABASE [testdb] FROM DISK = @FileName WITH RECOVERY;
ALTER DATABASE [testdb] SET ONLINE WITH ROLLBACK IMMEDIATE;
use testdb;
SELECT convert(VARCHAR(10),dt1,104) FROM [BackUpTestTable] order by dt1 desc;