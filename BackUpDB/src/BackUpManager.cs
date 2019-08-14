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
    private string CurrentEnv;
    private string dbName = "";
    public AppSets CurrentAppSets;
    private readonly bool isDevelopment;

    private string ErrorMessage = "";

    public BackUpManager()
    {
      try
      {
        Debug.WriteLine($"Starting up backup process:");
        CurrentEnv = Environment.GetEnvironmentVariable("NETCORE_ENV");
        isDevelopment = string.IsNullOrEmpty(CurrentEnv) ||
                           CurrentEnv.ToLower() == "development";

        var builder = new ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        if (!isDevelopment) //production
          builder.AddUserSecrets<Program>();

        Configuration = builder.Build();

        CurrentAppSets = new AppSets();
        Configuration.GetSection(nameof(AppSets)).Bind(CurrentAppSets);
        dbName = CurrentAppSets.DBName;

        Debug.WriteLine("Constructor has done");
      }
      catch (Exception ex)
      { ErrorMessage = ex.Message; }
    }

    public bool BackUpDataBase()
    {
      Debug.WriteLine("Connect to the data base and run back up query");

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
          Debug.WriteLine($"Execute query:<{query}>");
          command.ExecuteScalar();
          System.Threading.Thread.Sleep(1000);
        }
        isSuccess = true;
      }
      catch (Exception ex)
      {
        ErrorMessage = ex.Message;
        Debug.WriteLine(ErrorMessage);

      }
      finally
      {
        connection?.Dispose();
        command?.Dispose();
      }
      Debug.WriteLine($"All scripts are done. Status of running is {isSuccess}");

      return isSuccess;
    }

    public bool MoveFileToGDriveFolder()
    {
      Debug.WriteLine($"Start moving file to google drive folder");

      bool isSuccess = false;
      var fileName = $"{CurrentAppSets.BackUpFolder}\\{dbName}-{System.DateTime.Now.Date.ToString("dd.MM.yyyy")}.bak";
      if (!System.IO.File.Exists(fileName))
      {
        Debug.WriteLine($"File not found: {fileName}");
        return false;
      }
      try
      {
        System.IO.File.Copy(fileName, $"{CurrentAppSets.GDriveFolder}//{dbName}-{System.DateTime.Now.Date.ToString("dd.MM.yyyy")}.bak", true);
        isSuccess = true;
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.Message);
      }
      Debug.WriteLine($"Moving has done with status - [{isSuccess}]");

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