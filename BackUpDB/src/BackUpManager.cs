using System.Data.SqlClient;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;


namespace BackUpDB
{
  public static class BackUpManager
  {
    private static NLog.Logger _nLogger;
    private static string FileName = "";

    static BackUpManager()
    {
      _nLogger = Program.logger.WithProperty("side", Program.CurrentEnv);
    }
    public static bool BackUpDataBase(AppSets appSets)
    {
      _nLogger.Info("Connect to the data base and run back up query");

      var conStrBuilder = new SqlConnectionStringBuilder(appSets.ConnectionString);
      FileName = $"{appSets.BackUpFolder}/{conStrBuilder.InitialCatalog}-{System.DateTime.Now.Date.ToShortDateString()}.bak";
      appSets.Query = $"USE {conStrBuilder.InitialCatalog}; GO BACKUP DATABASE {conStrBuilder.InitialCatalog} TO DISK = {FileName}; GO";
      bool isSuccess = false;
      using (var sc = new SqlConnection(conStrBuilder.ConnectionString))
      {
        using (var dr = new SqlCommand(appSets.Query, sc))
        {
          sc.Open();
          dr.ExecuteScalar();
          isSuccess = true;
        }
      }
      return isSuccess;
    }

    public static bool UploadFileToGD(GoogleDriveSets gDriveSecrets)
    {
      bool isSuccess = false;
      _nLogger.Info("Checking of existence of bak file");
      if (!System.IO.File.Exists(FileName))
        return false;

      File body = new File();
      body.Name = System.IO.Path.GetFileName(FileName);
      body.Description = "";
      body.MimeType = GetMimeType(FileName);
      body.Parents = new List<string>() { gDriveSecrets.parent };

      string[] scopes = new string[] { DriveService.Scope.Drive, DriveService.Scope.DriveFile };

      var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
      {
        ClientId = gDriveSecrets.client_id,
        ClientSecret = gDriveSecrets.client_secret
      },
        scopes,
        gDriveSecrets.UserName,
        System.Threading.CancellationToken.None,
        new FileDataStore("MyAppsToken")).Result;


      DriveService service = new DriveService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = credential,
        ApplicationName = "MyAppName",
      });

      FilesResource.CreateMediaUpload request;

      using (var stream = new System.IO.FileStream(FileName,
                         System.IO.FileMode.Open))
      {
        request = service.Files.Create(body, stream, GetMimeType(FileName));
        request.Fields = "id";
        request.Upload();
      }

      if (request.ResponseBody.Id.Contains("200"))
        isSuccess = true;

      return isSuccess;

    }

    private static string GetMimeType(string fileName)
    {
      string mimeType = "application/unknown";
      string ext = System.IO.Path.GetExtension(fileName).ToLower();
      Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
      if (regKey != null && regKey.GetValue("Content Type") != null)
        mimeType = regKey.GetValue("Content Type").ToString();
      return mimeType;
    }

    public static void Notify(string email)
    {

    }

  }
}