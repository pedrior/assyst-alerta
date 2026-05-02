using Assyst.Alerta.Ingestion;
using Assyst.Alerta.Notification;
using Assyst.Alerta.Processing;
using Assyst.Alerta.Scheduling;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog(Log.Logger);

builder.Services.AddScheduling(builder.Configuration);
builder.Services.AddIngestion(builder.Configuration);
builder.Services.AddProcessing(builder.Configuration);
builder.Services.AddNotification(builder.Configuration);

try
{
    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "Host terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}