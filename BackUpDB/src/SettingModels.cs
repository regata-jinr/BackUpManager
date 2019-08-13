namespace BackUpDB
{
  public class AppSets
  {
    public string ConnectionString { get; set; }
    public string QueryFilePath { get; set; }
    public string Email { get; set; }
    public string EmailPassword { get; set; }
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
    public string FolderLink { get; set; }


  }
}