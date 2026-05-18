using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Grcp.Services;
using ModBus.Client;
using ModBus.Interfaces;
using ModBus.MapeamentoDeRegistradores;
using IoC.Appl;
using IoC.Interfaces;
using Monitor.Config;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Observability (Logs & Security)
ObservabilityConfig.ConfigureLogging(builder);
ObservabilityConfig.ConfigureSecurity(builder);

// 2. Configure Dependency Injection
builder.Services.AddSingleton<IModBusClient, ModBusTcpClient>();
builder.Services.AddSingleton<IRegisterMapper, DeltaPlcMapper>();
builder.Services.AddSingleton<IMonitorApp, MonitorApp>();

// 3. Configure gRPC
builder.Services.AddGrpc();
builder.Services.AddControllers();

var app = builder.Build();

// 4. Configure Middleware
app.UseRouting();
app.UseHttpMetrics(); // Prometheus metrics

app.UseAuthentication();
app.UseAuthorization();

// 5. Map Endpoints
app.MapGrpcService<MonitorGrpcService>();
app.MapControllers();
app.MapMetrics(); // Expose /metrics for Prometheus

// 6. Run Application background task
var monitorApp = app.Services.GetRequiredService<IMonitorApp>();
_ = Task.Run(() => monitorApp.RunAsync(app.Lifetime.ApplicationStopping));

app.Run();
