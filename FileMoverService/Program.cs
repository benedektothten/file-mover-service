using FileMoverService;
using Topshelf;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

// Load config from %ProgramData%\FileMoverService\appsettings.json so both the
// service and the UI read/write the same file without needing elevated rights.
var sharedConfigPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "FileMoverService", "appsettings.json");
builder.Configuration.AddJsonFile(sharedConfigPath, optional: true, reloadOnChange: true);

// Configure services
builder.Services.AddSingleton<IHostedService, FileMonitorService>();
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection(nameof(AppSettings)));

// Configure Topshelf
HostFactory.Run(configurator =>
{
    configurator.Service<IHost>(serviceConfigurator =>
    {
        serviceConfigurator.ConstructUsing(() => 
            builder.Build());
        
        serviceConfigurator.WhenStarted(host => 
        {
            Task.Run(() => host.StartAsync()); // Start in a background task
        });

        serviceConfigurator.WhenStopped(host =>
        {
            host.StopAsync().GetAwaiter().GetResult();
        });

    });

    // Service Configuration
    configurator.RunAsLocalSystem();
    configurator.SetServiceName("FileMoverService");
    configurator.SetDisplayName("File Mover Service");
    configurator.SetDescription("Automatically moves files based on configured rules.");

    // Startup Type
    configurator.StartAutomatically();
});
