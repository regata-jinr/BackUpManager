using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Diagnostics;

namespace BackUpDB
{
  public class BackUpManager
  {
    private IConfigurationRoot Configuration { get; set; }
    private readonly string CurrentEnv;
    private readonly string dbName = "";
    public AppSets CurrentAppSets;
    private readonly bool isDevelopment = false;

    private string ErrorMessage = "";

    public BackUpManager()
    {
      try
      {
        Console.WriteLine($"Starting up backup process:");
        CurrentEnv = Environment.GetEnvironmentVariable("NETCORE_ENV");

        if (!string.IsNullOrEmpty(CurrentEnv))
          isDevelopment = (CurrentEnv.ToLower() == "development");

        var builder = new ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        Console.WriteLine($"isDevelopment: {isDevelopment}");

        if (!isDevelopment) //production
          builder.AddUserSecrets<BackUpManager>();

        Configuration = builder.Build();

        CurrentAppSets = new AppSets();
        Configuration.GetSection(nameof(AppSets)).Bind(CurrentAppSets);
        dbName = CurrentAppSets.DBName;

        Console.WriteLine("Constructor has done");
      }
      catch (Exception ex)
      { ErrorMessage = ex.Message; }
    }

    public bool BackUpDataBase()
    {
      Console.WriteLine("Connect to the data base and run back up query");

      var conStrBuilder = new SqlConnectionStringBuilder(CurrentAppSets.ConnectionString);
      var queries = System.IO.File.ReadLines(CurrentAppSets.QueryFilePath).ToList();
      bool isSuccess = false;
      var connection = new SqlConnection(conStrBuilder.ConnectionString);
      SqlCommand command = null;
      try
      {
        connection.Open();
        foreach (var query in queries)
        {
          command = new SqlCommand(query, connection);
	  command.CommandTimeout = 60;
          Console.WriteLine($"Execute query:<{query}>");
          command.ExecuteScalar();
          System.Threading.Thread.Sleep(1000);
        }
        isSuccess = true;
      }
      catch (Exception ex)
      {
        ErrorMessage = ex.Message;
        Console.WriteLine(ErrorMessage);

      }
      finally
      {
        connection?.Dispose();
        command?.Dispose();
      }
      Console.WriteLine($"All scripts are done. Status of running is [{isSuccess}]");

      return isSuccess;
    }

    public bool MoveFileToGDriveFolder()
    {
      Console.WriteLine($"Start moving file to google drive folder");

      bool isSuccess = false;
      var fileName = $"{CurrentAppSets.BackUpFolder}//{dbName}-{System.DateTime.Now.Date.ToString("dd.MM.yyyy")}.bak";
      if (!System.IO.File.Exists(fileName))
      {
        Console.WriteLine($"File not found: {fileName}");
        return false;
      }
      try
      {
        System.IO.File.Copy(fileName, $"{CurrentAppSets.GDriveFolder}//{dbName}-{System.DateTime.Now.Date.ToString("dd.MM.yyyy")}.bak", true);
        isSuccess = true;
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
	ErrorMessage = ex.Message;
      }
      Console.WriteLine($"Moving has done with status - [{isSuccess}]");

      return isSuccess;

    }

    public void Notify(bool dbBackUpIsSuccess, bool fileMovingIsSuccess)
    {
      if (string.IsNullOrEmpty(CurrentAppSets.Email)) return;

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      var fromAddress = new MailAddress(CurrentAppSets.Email, "DB BackUp service");
      var toAddress = new MailAddress(CurrentAppSets.Email, "");
      string fromPassword = CurrentAppSets.EmailPassword;
      string subject = "[ERROR]Резервное копирование базы данных завершилось с ошибкой";
      string body = $"Во время работы службы по резервному копированию произошла ошибка. Текст ошибки:{Environment.NewLine}{ErrorMessage}";

      if (dbBackUpIsSuccess && fileMovingIsSuccess)
      {
        subject = "[Done]Резервное копирование базы данных завершилось успешно";
        body = $"Вы можете посмотреть файлы резервных копий по ссылке:{Environment.NewLine}{CurrentAppSets.GDriveFolderLink}";
      }

      using (var smtp = new SmtpClient
      {
        Host = "smtp.gmail.com",
        Port = 587,
        EnableSsl = true,
        DeliveryMethod = SmtpDeliveryMethod.Network,
        UseDefaultCredentials = false,
        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
      })
      {
        using (var message = new MailMessage(fromAddress, toAddress)
        {
          Subject = subject,
          Body = body
        })
        {
          smtp.Send(message);
        }
      }

    }

  }
}
