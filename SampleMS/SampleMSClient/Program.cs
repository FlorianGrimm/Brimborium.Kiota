using ApiSdk;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Bundle;
using Microsoft.Kiota.Http.HttpClientLibrary;

using System.Diagnostics;

namespace SampleMSClient;

public class Program {
    private const string _HostUrl = "http://localhost:5191";
    private const string _TodoUrl = "/api/Todo";

    public static async Task Main(string[] args) {
        try {
            int[] listLoopCout = [10, 20, 30, 40, 50, 100, 200, 300, 500, 1000];
            foreach (var loopcount in listLoopCout) {
                await Run(loopcount).ConfigureAwait(false);
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
        Console.WriteLine("Done");
    }
    public static async Task Run(int loopCount) {
        Microsoft.Extensions.DependencyInjection.ServiceCollection serviceBuilder = new();
        serviceBuilder.AddHttpClient("MyClient", client => {
            client.BaseAddress = new Uri(_HostUrl);
        });
        var authProvider = new AnonymousAuthenticationProvider();
        serviceBuilder.AddTransient<ApiClient>((sp) => {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(ApiClient));
            var requestAdapter = new HttpClientRequestAdapter(
                authenticationProvider: authProvider,
                httpClient: httpClient) {
                BaseUrl = _HostUrl
            };
            var client = new ApiClient(requestAdapter);
            return client;
        });
        using (var globalServiceProvider = serviceBuilder.BuildServiceProvider()) {

            await RunRequestTyped(globalServiceProvider, 1).ConfigureAwait(false);

            var start = Stopwatch.GetTimestamp();

            List<Task<double>> listTask = new();
            for (int i = 0; i < loopCount; i++) {
                listTask.Add(RunRequestTyped(globalServiceProvider, loopCount));
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


    private static async Task<double> RunRequestTyped(ServiceProvider globalServiceProvider, int loopCount) {
        var start = Stopwatch.GetTimestamp();
        using (var scope = globalServiceProvider.CreateScope()) {
            var scopeServiceProvider = scope.ServiceProvider;
            var apiClient = scopeServiceProvider.GetRequiredService<ApiClient>();
            for (int i = 0; i < loopCount; i++) {
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
