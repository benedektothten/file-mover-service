using FileMoverService;
using Topshelf;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

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
    configurator.SetServiceName("TorrentMoverService");
    configurator.SetDisplayName("Torrent Mover Service");
    configurator.SetDescription("Automatically moves downloaded torrent files");

    // Startup Type
    configurator.StartAutomatically();
});
