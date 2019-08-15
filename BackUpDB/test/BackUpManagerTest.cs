using Xunit;
using System.Data.SqlClient;
using System.Linq;

namespace BackUpDB
{

  public class DBBackUpTests : IClassFixture<BackUpManager>
  {
    BackUpManager _bm;
    public DBBackUpTests(BackUpManager bm)
    {
      _bm = bm;
    }

    [Fact]
    public void BackUpFileExist()
    {
      if (!System.IO.Directory.Exists("test"))
        System.IO.Directory.CreateDirectory("test");

      if (!System.IO.File.Exists("test/BackUpTestDb.sql"))
        System.IO.File.Copy("../../../test/BackUpTestDb.sql", "test/BackUpTestDb.sql");

      Assert.True(_bm.BackUpDataBase());
      Assert.True(System.IO.File.Exists($"C:\\Program Files\\Microsoft SQL Server\\MSSQL14.REGATALOCAL\\MSSQL\\Backup\\testdb-{System.DateTime.Now.Date.ToString("dd.MM.yyyy")}.bak"));
    }

    [Fact]
    public void CheckMovedFile()
    {
      Assert.True(_bm.MoveFileToGDriveFolder());
      Assert.True(System.IO.File.Exists($"D:\\GoogleDrive\\testdb-{System.DateTime.Now.Date.ToString("dd.MM.yyyy")}.bak"));
    }

    [Fact]
    public void BackUpFileCorrect()
    {
      System.Threading.Thread.Sleep(3000);
      var validateQueries = System.IO.File.ReadLines($"{System.Environment.CurrentDirectory}/../../../test/ValidateBackUpFile.sql").ToList();

      using (var connection = new SqlConnection("Data Source=RUMLAB\\REGATALOCAL;Initial Catalog=testdb;Integrated Security=True"))
      {
        connection.Open();
        foreach (var vQuery in validateQueries)
        {
          using (var command = new SqlCommand(vQuery, connection))
          {
            if (vQuery == validateQueries.Last())
              Assert.Equal(System.DateTime.Now.Date.ToString("dd.MM.yyyy"), command.ExecuteScalar());
            else
              command.ExecuteScalar();
            System.Threading.Thread.Sleep(1000);
          }
        }
      }
    }



  }// DBBackUpTests
}//BackUpDB
