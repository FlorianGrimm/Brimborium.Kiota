#if false
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Http.HttpResults;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);


        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.Run();
    }
}

#endif
namespace SampleWebApp;

public sealed class Program {
    public static async Task<int> Main(string[] args) {
        try {
            Program program = new();
            var version = program.GetType().Assembly.GetName().Version?.ToString() ?? "?";
            System.Console.Out.WriteLine($"Version: {version}");

            var builder = WebApplication.CreateBuilder(args);

            ApplicationFeatures applicationFeatures = new ApplicationFeatures(
                EnableOpenApi: true
                );
            program.ConfigureServices(builder, applicationFeatures);
            WebApplication app = builder.Build();
            await program.RunAsync(app);
            return 0;
        } catch (AggregateException ex) {
            ex.Handle((error) => {
                WriteException(error);
                return true;
            });
            return 1;
        } catch (Exception ex) {
            WriteException(ex);
            return 1;
        }

        static void WriteException(Exception ex) {
            System.Console.Error.WriteLine(ex.ToString());
        }
    }

    public void ConfigureServices(
        WebApplicationBuilder builder,
        ApplicationFeatures applicationFeatures) {
        // Add services to the container.

        _ = builder.Services.AddSingleton<ApplicationFeatures>(applicationFeatures);

        // services with Injectio Attribute
        _ = builder.Services.AddSampleWebApp();

        builder.ConfigureServices(applicationFeatures);
    }

    public async Task RunAsync(WebApplication app) {
        // Configure the HTTP request pipeline.

        /*
        app.UseHttpsRedirection();
        */

        app.ConfigureWebApplication();


        await app.RunAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Enable Features
/// </summary>
public record class ApplicationFeatures(
    bool EnableOpenApi
    );

public static class ApplicationFeaturesExtension {
    extension(WebApplicationBuilder builder) {
        public void ConfigureServices(ApplicationFeatures applicationFeatures, LogLevel logLevel = LogLevel.Trace) {
            // this creates a second ServiceCollection/ServiceProvider
            var serviceBuilder = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            serviceBuilder.AddLogging((loggingBuilder) => {
                loggingBuilder.AddConsole((consoleLoggerOptions) => {
                });
                loggingBuilder.SetMinimumLevel(logLevel);
            });
            serviceBuilder.AddSingleton<ApplicationFeatures>(applicationFeatures);
            // using the Injectio attributed services.
            serviceBuilder.AddSampleWebApp();
            using (var configureServiceProvider = serviceBuilder.BuildServiceProvider()) {
                configureServiceProvider.ConfigureServices(builder);
            }
            // this seams to run long (100ms),
            // but this thing that eats the time with type loading - which will be loaded anyway
            // but if it is compiled to aot it comes down to under 1ms.
        }

    }
}

public sealed record class ConfigureServicesBuilder(List<IConfigureWebApplication> ListConfigureWebApplication);

public static partial class ConfigureServicesBuilderExtension {
    extension(IServiceProvider serviceProvider) {
        public void ConfigureServices(
            WebApplicationBuilder builder
        ) {
            if (serviceProvider.GetServices<IConfigureServices>() is { } list) {
                List<IConfigureServices> listConfigureService = new(list);
                List<IConfigureWebApplication> listConfigureWebApplication = new(listConfigureService.Count);
                foreach (var item in listConfigureService) {
                    item.ConfigureServices(builder);
                    if (item is IConfigureWebApplication configureWebApplication) {
                        listConfigureWebApplication.Add(configureWebApplication);
                    }
                }
                ConfigureServicesBuilder configureServicesBuilder = new(listConfigureWebApplication);
                builder.Services.AddSingleton<ConfigureServicesBuilder>(configureServicesBuilder);
            } else {
                ConfigureServicesBuilder configureServicesBuilder = new([]);
                builder.Services.AddSingleton<ConfigureServicesBuilder>(configureServicesBuilder);
            }
        }
    }

    extension(WebApplication app) {
        /// <summary>
        /// iterate over the <see cref="IConfigureWebApplication"/>, in order, and call <see cref="IConfigureWebApplication.ConfigureWebApplication(WebApplication)"/>.
        /// </summary>
        /// <param name="app">The app that is about to run.</param>
        public void ConfigureWebApplication() {
            ConfigureServicesBuilder configureServicesBuilder = app.Services.GetRequiredService<ConfigureServicesBuilder>();
            var orderedList = configureServicesBuilder.ListConfigureWebApplication.OrderBy(item => item.GetOrder());
            foreach (var item in orderedList) {
                item.ConfigureWebApplication(app);
            }
        }
    }
}

/// <summary>
/// Configure Services for the WebApplicationBuilder
/// </summary>
public interface IConfigureServices {
    /// <summary>
    /// Configure the services for <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The builder to polish.</param>
    /// <example>
    /// [Injectio.Attributes.RegisterSingleton<IConfigureServices>(Duplicate = DuplicateStrategy.Append)]
    /// public class FeatureAbc(ApplicationFeatures applicationFeatures) : IConfigureServices {
    ///     ...
    /// }
    /// </example>
    void ConfigureServices(WebApplicationBuilder builder);
}

public static partial class IConfigureServicesLogExtension {
    // ConfigureServices
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "ConfigureServices-Feature({feature})"
        )]
    public static partial void LogConfigureServicesFeature(this ILogger logger, string feature);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "ConfigureServices-Feature-disabled({feature})"
        )]
    public static partial void LogConfigureServicesFeatureDisabled(this ILogger logger, string feature);

    // ConfigureWebApplication
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "ConfigureWebApplication-Feature({feature})"
        )]
    public static partial void LogConfigureWebApplicationFeature(this ILogger logger, string feature);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "ConfigureWebApplication-Feature-disabled({feature})"
        )]
    public static partial void LogConfigureWebApplicationFeatureDisabled(this ILogger logger, string feature);

}

/// <summary>
/// Order for the Middleware to call.
/// The order matters.
/// </summary>
public enum ConfigureWebApplicationOrder {
    First,
    OpenApi,
    MinimalApiEndpoint,
    Last
}

/// <summary>
/// Configure the WebApplication middleware
/// </summary>
public interface IConfigureWebApplication : IConfigureServices {
    /// <summary>
    /// The Order to call
    /// </summary>
    /// <returns>The Order to call</returns>
    ConfigureWebApplicationOrder GetOrder();

    /// <summary>
    /// Configure the WebApplication middleware
    /// </summary>
    /// <param name="app">The app that is about to run.</param>
    void ConfigureWebApplication(WebApplication app);
}

/// <summary>
/// Allows to register the minimalapi-endpoint(s).
/// </summary>
public interface IMinimalApiEndpoint {
    /// <summary>
    /// Map the minimalapi
    /// </summary>
    /// <param name="builder">The app that is abount to run.</param>
    void MapMinimalApi(WebApplication builder);
}


[Injectio.Attributes.RegisterSingleton<IConfigureServices>(Duplicate = DuplicateStrategy.Append)]
public sealed class MinimalApiEndpointConfigureWebApplication(
        ILogger<MinimalApiEndpointConfigureWebApplication> logger
    ) : IConfigureWebApplication {
    private readonly ILogger<MinimalApiEndpointConfigureWebApplication> _Logger = logger;

    public void ConfigureServices(WebApplicationBuilder builder) {
    }

    public ConfigureWebApplicationOrder GetOrder() => ConfigureWebApplicationOrder.MinimalApiEndpoint;

    public void ConfigureWebApplication(WebApplication app) {
        // register all MinimalAPI endpoints
        this._Logger.LogConfigureWebApplicationFeature("MinimalAPIEndpoints");
        if (app.Services.GetServices<IMinimalApiEndpoint>() is { } list) {
            foreach (var minimalApiEndpoint in list) {
                minimalApiEndpoint.MapMinimalApi(app);
            }
        }
    }
}
