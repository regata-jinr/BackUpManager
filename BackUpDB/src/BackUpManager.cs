using System.Data.SqlClient;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;

namespace BackUpDB
{
  public class BackUpManager
  {
    private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private IConfigurationRoot Configuration { get; set; }
    private string CurrentEnv;
    public AppSets CurrentAppSets;
    public GoogleDriveSets CurrentGDriveSets;
    private readonly bool isDevelopment;

    private string ErrorMessage = "";

    public BackUpManager()
    {
      try
      {
        logger.Info($"Starting up backup process:");
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

        CurrentGDriveSets = new GoogleDriveSets();
        Configuration.GetSection(nameof(GoogleDriveSets)).Bind(CurrentGDriveSets);


      }
      catch (Exception ex)
      { ErrorMessage = ex.Message; }
    }

    public bool BackUpDataBase()
    {
      logger.Info("Connect to the data base and run back up query");

      var conStrBuilder = new SqlConnectionStringBuilder(CurrentAppSets.ConnectionString);

      string script = System.IO.File.ReadAllText(CurrentAppSets.QueryFilePath);
      bool isSuccess = false;
      var connection = new SqlConnection(conStrBuilder.ConnectionString);
      var command = new SqlCommand(script, connection);
      try
      {

        connection.Open();
        command.ExecuteScalar();
        isSuccess = true;

      }
      catch (Exception ex)
      { ErrorMessage = ex.Message; }
      finally
      {
        connection?.Dispose();
        command?.Dispose();
      }
      return isSuccess;
    }

    public bool UploadFileToGD()
    {
      bool isSuccess = false;
      logger.Info("Checking of existence of bak file");

      var dir = new System.IO.DirectoryInfo(CurrentAppSets.BackUpFolder);
      string fileName = dir.GetFiles("*.bak").OrderBy(f => f.CreationTime).Last().Name;

      if (!System.IO.File.Exists(fileName))
      {
        ErrorMessage = "Couldn't find backup file.";
        return false;
      }

      var body = new Google.Apis.Drive.v3.Data.File();
      body.Name = System.IO.Path.GetFileName(fileName);
      body.Description = "";
      body.MimeType = GetMimeType(fileName);
      body.Parents = new List<string>() { CurrentGDriveSets.parent };

      string[] scopes = new string[] { DriveService.Scope.Drive, DriveService.Scope.DriveFile };

      var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
      {
        ClientId = CurrentGDriveSets.client_id,
        ClientSecret = CurrentGDriveSets.client_secret
      },
        scopes,
        CurrentGDriveSets.UserName,
        System.Threading.CancellationToken.None,
        new FileDataStore("MyAppsToken")).Result;


      DriveService service = new DriveService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = credential,
        ApplicationName = CurrentGDriveSets.project_id,
      });


      var stream = new System.IO.FileStream(fileName, System.IO.FileMode.Open);
      FilesResource.CreateMediaUpload request = null;
      try
      {
        request = service.Files.Create(body, stream, GetMimeType(fileName));
        request.Fields = "id";
        request.Upload();
      }
      catch (Exception ex)
      { ErrorMessage = ex.Message; }
      finally
      {
        stream?.Dispose();
      }


      if (request != null && request.ResponseBody.Id.Contains("200"))
        isSuccess = true;

      return isSuccess;

    }

    private string GetMimeType(string fileName)
    {
      string mimeType = "application/unknown";
      string ext = System.IO.Path.GetExtension(fileName).ToLower();
      Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
      if (regKey != null && regKey.GetValue("Content Type") != null)
        mimeType = regKey.GetValue("Content Type").ToString();
      return mimeType;
    }

    public void Notify(bool dbBackUpIsSuccess, bool fileMovingIsSuccess)
    {
      if (string.IsNullOrEmpty(CurrentAppSets.Email)) return;

      var fromAddress = new MailAddress(CurrentAppSets.Email, "DB BackUp service");
      var toAddress = new MailAddress(CurrentAppSets.Email, "");
      string fromPassword = CurrentAppSets.EmailPassword;
      string subject = "[ERROR]Резервное копирование базы данных завершилось с ошибкой";
      string body = $"Во время работы службы по резервному копированию произошла ошибка. Текст ошибки:{Environment.NewLine}{ErrorMessage}";

      if (dbBackUpIsSuccess && fileMovingIsSuccess)
      {
        subject = "[Done]Резервное копирование базы данных завершилось успешно";
        body = $"Вы можете посмотреть файлы резервных копий по ссылке:{Environment.NewLine}{CurrentGDriveSets.FolderLink}";
      }

      var smtp = new SmtpClient
      {
        Host = "smtp.gmail.com",
        Port = 587,
        EnableSsl = true,
        DeliveryMethod = SmtpDeliveryMethod.Network,
        UseDefaultCredentials = false,
        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
      };
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