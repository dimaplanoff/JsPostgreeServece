using WorkerService;




var host = Host.CreateDefaultBuilder(args);

host.ConfigureAppConfiguration((hostingContext, configuration) =>
{    
    configuration.Sources.Clear();
    configuration.AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy_settings.json"), optional: false, reloadOnChange: true);
    IConfigurationRoot configurationRoot = configuration.Build();
    Const.Init(configurationRoot);
});

if (Environment.OSVersion.Platform == PlatformID.Win32NT)
    host.UseWindowsService();

host.ConfigureServices((services) =>
{    
    services.AddHostedService<Worker>();
});



await host.Build().RunAsync();
