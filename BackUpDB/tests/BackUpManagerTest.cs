using Xunit;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.IO;

namespace BackUpDB.tests
{

  public class DBBackUpTests
  {
    private BackUpDB.AppSets appSets;
    private IConfigurationRoot Configuration { get; set; }


    DBBackUpTests() : IClassFixture<BackUpManager>
    {
    }

  [Fact]
  public void ConnectionAndQueryingTest()
  {
    Assert.True(BackUpManager.BackUpDataBase(appSets));
  }

  [Fact]
  public void FileExistingTest()
  {
    var conStrBuilder = new SqlConnectionStringBuilder(appSets.ConnectionString);
    var FileName = $"{appSets.BackUpFolder}/{conStrBuilder.InitialCatalog}-{System.DateTime.Now.Date.ToShortDateString()}.bak";
    Assert.True(File.Exists(FileName));
  }

}

}