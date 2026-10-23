namespace SampleWebApp.Feature;

[Injectio.Attributes.RegisterSingleton<IConfigureServices>(Duplicate = DuplicateStrategy.Append)]
public sealed partial class OpenApiFeature : IConfigureServices, IConfigureWebApplication {
    private readonly ApplicationFeatures _ApplicationFeatures;
    private readonly ILogger<OpenApiFeature> _Logger;

    public OpenApiFeature(ApplicationFeatures applicationFeatures, ILogger<OpenApiFeature> logger
        ) {
        this._ApplicationFeatures = applicationFeatures;
        this._Logger = logger;
    }

    public void ConfigureServices(WebApplicationBuilder builder) {
        if (!this._ApplicationFeatures.EnableOpenApi) {
            this._Logger.LogConfigureServicesFeatureDisabled("OpenApiFeature");
            return;
        }

        this._Logger.LogConfigureServicesFeature("OpenApiFeature");

        _ = builder.Services.AddOpenApi();
    }

    public ConfigureWebApplicationOrder GetOrder() => ConfigureWebApplicationOrder.OpenApi;

    public void ConfigureWebApplication(WebApplication app) {
        if (!this._ApplicationFeatures.EnableOpenApi) {
            this._Logger.LogConfigureWebApplicationFeatureDisabled("OpenApiFeature");
            return;
        }

        // if (app.Environment.IsDevelopment()) { }

        // 
        this._Logger.LogConfigureWebApplicationFeature("OpenApiFeature");

        _ = app.MapOpenApi();
    }

}