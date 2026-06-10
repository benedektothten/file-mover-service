using FileMoverService;
using Serilog;
using Topshelf;

var logPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "FileMoverService", "logs", "service-.log");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        logPath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<FileMonitorService>();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

HostFactory.Run(configurator =>
{
    configurator.Service<IHost>(s =>
    {
        s.ConstructUsing(() => builder.Build());
        s.WhenStarted(host => Task.Run(() => host.StartAsync()));
        s.WhenStopped(host =>
        {
            host.StopAsync().GetAwaiter().GetResult();
            Log.CloseAndFlush();
        });
    });

    configurator.RunAsLocalSystem();
    configurator.SetServiceName("FileMoverService");
    configurator.SetDisplayName("File Mover Service");
    configurator.SetDescription("Automatically moves files based on configured rules.");
    configurator.StartAutomatically();
});
