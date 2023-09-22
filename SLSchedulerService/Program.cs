using SLScheduler;




var host = Host.CreateDefaultBuilder(args);

host.ConfigureAppConfiguration((hostingContext, configuration) =>
{    
    configuration.Sources.Clear();
    configuration.AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scheduler_settings.json"), optional: false, reloadOnChange: true);
    IConfigurationRoot configurationRoot = configuration.Build();
    Const.Init(configurationRoot);
});

if (Environment.OSVersion.Platform == PlatformID.Win32NT)
    host.UseWindowsService();

host.ConfigureServices((services) =>
{
    services.AddSingleton<SLSchedulerWorker>();
    services.AddHostedService<SLSchedulerWorker>();
});



await host.Build().RunAsync();
