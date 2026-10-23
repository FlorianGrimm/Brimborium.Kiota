using System.Text.Json.Serialization;

namespace SampleMSWebApp;


[JsonSerializable(typeof(List<Todo>))]
[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(Todo))]
internal partial class AppJsonSerializerContext : JsonSerializerContext {

}

[Injectio.Attributes.RegisterSingleton<IConfigureServices>(Duplicate = DuplicateStrategy.Append)]
public sealed partial class AppJsonSerializerContextCon : IConfigureServices {
    public void ConfigureServices(WebApplicationBuilder builder) {
        builder.Services.ConfigureHttpJsonOptions(options => {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });
    }
}