using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grcp.Proto;
using ModBus.Interfaces;
using Microsoft.Extensions.Logging;

namespace Grcp.Services
{
    public class MonitorGrpcService : MonitorService.MonitorServiceBase
    {
        private readonly IModBusClient _modbusClient;
        private readonly ILogger<MonitorGrpcService> _logger;

        public MonitorGrpcService(IModBusClient modbusClient, ILogger<MonitorGrpcService> logger)
        {
            _modbusClient = modbusClient;
            _logger = logger;
        }

        public override Task<StatusResponse> GetPlcStatus(StatusRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Received status request for device: {DeviceId}", request.DeviceId);
            
            return Task.FromResult(new StatusResponse
            {
                IsConnected = _modbusClient.IsConnected,
                LastError = _modbusClient.IsConnected ? "" : "Disconnected from PLC",
                Metadata = { { "version", "1.0.0" } }
            });
        }

        public override async Task StreamPlcData(StreamRequest request, IServerStreamWriter<PlcData> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("Started data stream for device: {DeviceId}", request.DeviceId);

            while (!context.CancellationToken.IsCancellationRequested)
            {
                var data = new PlcData
                {
                    DeviceId = request.DeviceId,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Registers = 
                    {
                        new RegisterValue { Address = 0, Value = 123, Type = "Holding" },
                        new RegisterValue { Address = 1, Value = 456, Type = "Holding" }
                    }
                };

                await responseStream.WriteAsync(data);
                await Task.Delay(1000);
            }
        }
    }
}
