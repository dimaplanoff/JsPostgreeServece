using System.Text;
using UniApp;


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);



IConfiguration config = new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.json"), optional: false, reloadOnChange: true)
        .Build();
Const.Entity.Init(config);




var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();




builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(//Const.tOrigins,
       policy =>
       {
           policy.AllowAnyOrigin()
           .AllowAnyHeader()
           .AllowAnyMethod();
       });
});

var app = builder.Build();

app.UseRouting();
app.UseHsts();
app.UseStaticFiles();
app.MapControllers();
app.UseCors();// (Const.tOrigins);
app.Use(async (context, next) =>
{
    await next.Invoke();
});
app.Run();
