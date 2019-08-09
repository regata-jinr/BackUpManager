using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

//TODO: figure out https://www.twilio.com/blog/2018/05/user-secrets-in-a-net-core-console-app.html
//TODO: why should I use appsettings.json if I have already added json file via user-secrets?
//TODO: split code to different files.
//TODO: split code to different files.

namespace BackUpDB
{
  public class Program
  {
    public static IConfigurationRoot Configuration { get; set; }

    private static void Main()
    {
      var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

      var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                          devEnvironmentVariable.ToLower() == "development";
      //Determines the working environment as IHostingEnvironment is unavailable in a console app

      var builder = new ConfigurationBuilder();
      // tell the builder to look for the appsettings.json file
      builder
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

      //only add secrets in development
      if (isDevelopment)
      {
        builder.AddUserSecrets<Program>();
      }

      Configuration = builder.Build();

      IServiceCollection services = new ServiceCollection();

      //Map the implementations of your classes here ready for DI
      services
          .Configure<DBSecrets>(Configuration.GetSection(nameof(DBSecrets)))
          // .AddLoggin() //FIXME: why it doesn't work?
          .AddOptions()
          .AddSingleton<ISecretRevealer, SecretRevealer>()
          .BuildServiceProvider();

      var serviceProvider = services.BuildServiceProvider();

      // Get the service you need - DI will handle any dependencies - in this case IOptions<SecretStuff>
      var revealer = serviceProvider.GetService<ISecretRevealer>();

      revealer.Reveal();

      Console.ReadKey();
    }

  }

  public class DBSecrets
  {
    public string ConnectionString { get; set; }
  }

  public interface ISecretRevealer
  {
    void Reveal();
  }
  public class SecretRevealer : ISecretRevealer
  {
    private readonly DBSecrets _secrets;
    // I’ve injected <em>secrets</em> into the constructor as setup in Program.cs
    public SecretRevealer(IOptions<DBSecrets> secrets)
    {
      // We want to know if secrets is null so we throw an exception if it is
      _secrets = secrets.Value ?? throw new ArgumentNullException(nameof(secrets));
    }

    public void Reveal()
    {
      //I can now use my mapped secrets below.
      Console.WriteLine($"Secret One: {_secrets.ConnectionString}");
    }
  }
}
