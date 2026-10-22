using ApiSdk;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Bundle;

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SampleClient;

public class Program {
    private const string _HostUrl = "http://localhost:5190";
    private const string _TodoUrl = "/api/Todo";
    private const int _LoopCount = 100;

    public static async Task Main(string[] args) {
        try {
            await Run(args);
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
        Console.WriteLine("Done");
    }
    public static async Task Run(string[] args) {
        Microsoft.Extensions.DependencyInjection.ServiceCollection serviceBuilder = new();
        serviceBuilder.AddTransient<ApiClient>((sp) => {
            var authProvider = new AnonymousAuthenticationProvider();
            var requestAdapter = new DefaultRequestAdapter(authProvider);
            var client = new ApiClient(requestAdapter);
            return client;
        });
        serviceBuilder.AddHttpClient("MyClient", client => {
            client.BaseAddress = new Uri(_HostUrl);
        });
        using (var globalServiceProvider = serviceBuilder.BuildServiceProvider()) {

            await RunRequestTyped(globalServiceProvider).ConfigureAwait(false);

            var start = Stopwatch.GetTimestamp();

            List<Task<double>> listTask = new();
            for (int i = 0; i < _LoopCount; i++) {
                listTask.Add(RunRequestTyped(globalServiceProvider));
            }
            int countSuccess = 0;
            double sumElapsedTime = 0.0d;
            foreach (var task in listTask) {
                var taskElapsedTime = await task.ConfigureAwait(false);
                if (0d < taskElapsedTime) {
                    sumElapsedTime += taskElapsedTime;
                    countSuccess++;
                }
            }

            var elapsedTime = Stopwatch.GetElapsedTime(start);
            System.Console.Out.WriteLine($"Done - {elapsedTime.TotalMilliseconds}ms");
            System.Console.Out.WriteLine($"Success - {countSuccess}/{listTask.Count}");
            double averageElapsedTime;
            if (0 < countSuccess) {
                averageElapsedTime = sumElapsedTime / countSuccess;
            } else {
                averageElapsedTime = -1;
            }
            System.Console.Out.WriteLine($"Average Elapsed Time - {averageElapsedTime}sec");
        }
    }

    private static async Task<double> RunRequestGeneric(ServiceProvider globalServiceProvider) {
        var start = Stopwatch.GetTimestamp();
        using (var scope = globalServiceProvider.CreateScope()) {
            var scopeServiceProvider = scope.ServiceProvider;
            var httpClientFactory = scopeServiceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("MyClient");
            for (int i = 0; i < _LoopCount; i++) {
                try {
                    using (var response = await httpClient.GetAsync(_TodoUrl).ConfigureAwait(false)) {
                        response.EnsureSuccessStatusCode();
                    }
                } catch (System.Exception ex) {
                    System.Console.Error.WriteLine($"Error: {ex.Message}");
                    return -1.0d;
                }
            }
        }
        var elapsedTime = Stopwatch.GetElapsedTime(start);
        return elapsedTime.TotalSeconds;
    }
    private static async Task<double> RunRequestTyped(ServiceProvider globalServiceProvider) {
        var start = Stopwatch.GetTimestamp();
        using (var scope = globalServiceProvider.CreateScope()) {
            var scopeServiceProvider = scope.ServiceProvider;
            var apiClient = scopeServiceProvider.GetRequiredService<ApiClient>();
            for (int i = 0; i < _LoopCount; i++) {
                try {
                    var result = await apiClient.Api.Todo.GetAsync((eequestConfiguration) => {
                    }, CancellationToken.None);
                    if (result is null) { throw new Exception("result is null"); }
                } catch (System.Exception ex) {
                    // System.Console.Error.WriteLine($"Error: {ex.Message}");
                    System.Console.Error.WriteLine($"Error: {ex.ToString()}");
                    return -1.0d;
                }
            }
        }
        var elapsedTime = Stopwatch.GetElapsedTime(start);
        return elapsedTime.TotalSeconds;
    }
}
/*
cd C:\github.com\FlorianGrimm\Brimborium.Kiota\SampleClient\
kiota generate --openapi C:\github.com\FlorianGrimm\Brimborium.Kiota\SampleWebApp\openapi.v1.json --output C:\github.com\FlorianGrimm\Brimborium.Kiota\SampleClient\Client --language CSharp

cd C:\github.com\FlorianGrimm\Brimborium.Kiota\
dotnet run --project src\kiota\kiota.csproj -- generate --openapi C:\github.com\FlorianGrimm\Brimborium.Kiota\SampleWebApp\openapi.v1.json --output C:\github.com\FlorianGrimm\Brimborium.Kiota\SampleClient\Client --language CSharp
 
 */