using System.Net;
using System.Net.Http;
using Keep2DFilesBot;
using Keep2DFilesBot.Features.DownloadFile;
using Keep2DFilesBot.Infrastructure.Storage;
using Keep2DFilesBot.Shared.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Telegram.Bot;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/bot-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Запуск Keep2DFilesBot");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();

    builder.Services.Configure<BotConfiguration>(
        builder.Configuration.GetSection(BotConfiguration.SectionName));
    builder.Services.Configure<DownloadConfiguration>(
        builder.Configuration.GetSection(DownloadConfiguration.SectionName));
    builder.Services.Configure<StorageConfiguration>(
        builder.Configuration.GetSection(StorageConfiguration.SectionName));

    var botConfig = builder.Configuration
        .GetSection(BotConfiguration.SectionName)
        .Get<BotConfiguration>();

    if (botConfig is null || string.IsNullOrWhiteSpace(botConfig.Token))
    {
        Log.Fatal("Не настроен токен бота в конфигурации");
        return 1;
    }

    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botConfig.Token));

    builder.Services.AddHttpClient("DownloadClient")
        .AddPolicyHandler((sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<DownloadConfiguration>>().Value;
            return CreateRetryPolicy(options);
        })
        .AddPolicyHandler((sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<DownloadConfiguration>>().Value;
            return CreateTimeoutPolicy(options);
        });

    builder.Services.AddSingleton<FileStorage>();

    builder.Services.AddScoped<DownloadFileHandler>();

    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();

    await host.RunAsync();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение завершилось с критической ошибкой");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(DownloadConfiguration config)
{
    var retryCount = Math.Max(0, config.RetryCount);
    var delaySeconds = Math.Max(1, config.RetryDelaySeconds);

    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount,
            attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1) * delaySeconds),
            (outcome, timeSpan, attempt, _) =>
            {
                Log.Warning(
                    "Повтор {RetryAttempt} через {DelaySeconds:F1} секунд. Причина: {Reason}",
                    attempt,
                    timeSpan.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Неизвестно");
            });
}

static IAsyncPolicy<HttpResponseMessage> CreateTimeoutPolicy(DownloadConfiguration config)
{
    var timeout = TimeSpan.FromSeconds(Math.Max(1, config.TimeoutSeconds));
    return Policy.TimeoutAsync<HttpResponseMessage>(timeout);
}
