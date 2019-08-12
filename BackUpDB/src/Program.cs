using System;
using Microsoft.Extensions.Configuration;

namespace BackUpDB
{
  public class Program
  {
    static Program()
    {
      CurrentEnv = Environment.GetEnvironmentVariable("NETCORE_ENV");
    }
    public static string CurrentEnv;
    public static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private static NLog.Logger _nLogger;
    private static IConfigurationRoot Configuration { get; set; }
    private static void Main()
    {

      _nLogger = logger.WithProperty("side", CurrentEnv);
      _nLogger.Info($"Starting up backup process:");


      Console.WriteLine(CurrentEnv);

      var isDevelopment = string.IsNullOrEmpty(CurrentEnv) ||
                          CurrentEnv.ToLower() == "development";

      // gets connection string from user secrets
      var builder = new ConfigurationBuilder();
      builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

      if (!isDevelopment) //production
        builder.AddUserSecrets<Program>();

      Configuration = builder.Build();

      var dbs = new AppSets();
      Configuration.GetSection(nameof(AppSets)).Bind(dbs);

      Console.WriteLine(BackUpManager.BackUpDataBase(dbs.ConnectionString, dbs.Query));

      Console.ReadKey();
    }

  }

  public class AppSets
  {
    public string ConnectionString { get; set; }
    public string Query { get; set; }
    public string BackUpFolder { get; set; }
  }


  public class GoogleDriveSets
  {
    public string auth_provider_x509_cert_url { get; set; }
    public string auth_uri { get; set; }
    public string client_id { get; set; }
    public string client_secret { get; set; }
    public string project_id { get; set; }
    public string redirect_uris { get; set; }
    public string token_uri { get; set; }
    public string parent { get; set; }
    public string UserName { get; set; }

  }

}
