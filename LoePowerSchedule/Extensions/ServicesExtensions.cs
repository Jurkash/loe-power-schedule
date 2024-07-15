using Azure;
using Azure.AI.Vision.ImageAnalysis;
using LoePowerSchedule.DAL;
using LoePowerSchedule.Services;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using TimeProvider = LoePowerSchedule.Services.TimeProvider;

namespace LoePowerSchedule.Extensions;

public static class ServicesExtensions
{
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        var mongoDbSection = configuration.GetSection(nameof(MongoDbOptions));
        services.Configure<MongoDbOptions>(mongoDbSection);
        var mongoOptions = mongoDbSection.Get<MongoDbOptions>();
        
        var mongoDbContext = new MongoDbContext(mongoOptions.ConnectionString, mongoOptions.DatabaseName);
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new IgnoreIfNullConvention(true),
            new EnumRepresentationConvention(BsonType.String)
        };
        ConventionRegistry.Register("DefaultConvention", conventionPack, t => true);

        return services.AddSingleton(mongoDbContext);
    }

    public static IServiceCollection AddComputerVision(this IServiceCollection services, IConfiguration configuration)
    {
        var visionOptionsSection = configuration.GetSection(nameof(AzureVisionOptions));
        services.Configure<AzureVisionOptions>(visionOptionsSection);
        var visionOptions = visionOptionsSection.Get<AzureVisionOptions>();
       
        var visionClient = new ImageAnalysisClient(
            new Uri(visionOptions.Endpoint),
            new AzureKeyCredential(visionOptions.Key));

        return services
            .AddScoped<VisionService>()
            .AddSingleton(visionClient);
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        return services
            .AddScoped<ScheduleRepository>()
            .AddScoped<OcrRepository>();
    }

    public static IServiceCollection AddCoreModule(this IServiceCollection services)
    {
        return services
            .AddTransient<TimeProvider>()
            .AddScoped<ImageScraperService>()
            .AddScoped<ImportService>()
            .AddScoped<ColorRecognitionService>()
            .AddScoped<ScheduleParserService>();
    }

    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        return services
           .AddHostedService<ImportHostedService>();
    }

    public static IServiceCollection AddCustomsizedSwaggerGen(this IServiceCollection services)
    {
        return services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LOE Power Schedule API", 
                Description = "The Lviv Region Electricity Schedule API is designed to provide precise " +
                              "daily schedules for power on/off periods in the Lviv region. The general " +
                              "schedules are often not reliable due to optional time periods where power " +
                              "may or may not be available. This API addresses this issue by scraping the" +
                              " Lviv region's official website daily, utilizing computer vision AI to recognize " +
                              "and extract schedule information from posted images, and storing the data in a " +
                              "structured format in the database. This enables users to access accurate and" +
                              " detailed schedules for each group, allowing for better planning and management " +
                              "of electricity usage.",
                Version = "v1"
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description =
                    "Static token authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    public static IServiceCollection ConfigureOptions(this IServiceCollection services)
    {
         services
            .AddOptions<BrowserOptions>()
            .BindConfiguration(nameof(BrowserOptions))
            .ValidateDataAnnotations();

        services.AddOptions<ScrapeOptions>()
            .BindConfiguration(nameof(ScrapeOptions))
            .ValidateDataAnnotations();
        
        services.AddOptions<ImportOptions>()
            .BindConfiguration(nameof(ImportOptions))
            .ValidateDataAnnotations();

        return services;
    }
}