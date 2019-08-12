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


    DBBackUpTests()
    {
      var builder = new ConfigurationBuilder();
      builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
      Configuration = builder.Build();
      appSets = new BackUpDB.AppSets();
      Configuration.GetSection(nameof(AppSets)).Bind(appSets);
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