using System.Text.Json.Serialization;
using LoePowerSchedule.Extensions;
using LoePowerSchedule.Middleware;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsProduction())
{
    builder.Configuration.ConfigureKeyVault();
}

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddAuthentication("StaticTokenScheme")
    .AddScheme<AuthenticationSchemeOptions, StaticTokenAuthenticationHandler>("StaticTokenScheme", null);

builder.Services
    .ConfigureOptions()
    .AddCustomsizedSwaggerGen()
    .AddHttpClient()
    .AddComputerVision(builder.Configuration)
    .AddMongoDb(builder.Configuration)
    .AddRepositories()
    .AddCoreModule()
    .AddBackgroundServices();

var app = builder.Build();

app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(opt =>
{
    opt.DocumentTitle = "LOE Power Schedule";
    opt.InjectStylesheet("/swagger-ui/custom.css");
    opt.InjectJavascript("/swagger-ui/custom.js");
    opt.DefaultModelsExpandDepth(-1);
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
