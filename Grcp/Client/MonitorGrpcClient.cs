using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grcp.Proto;
using Grpc.Core;

namespace Grcp.Client
{
    public class MonitorGrpcClient
    {
        private readonly MonitorService.MonitorServiceClient _client;

        public MonitorGrpcClient(string address)
        {
            var channel = GrpcChannel.ForAddress(address);
            _client = new MonitorService.MonitorServiceClient(channel);
        }

        public async Task GetStatusAsync(string deviceId)
        {
            var response = await _client.GetPlcStatusAsync(new StatusRequest { DeviceId = deviceId });
            Console.WriteLine($"Status: {response.IsConnected}, Error: {response.LastError}");
        }

        public async Task StartStreamingAsync(string deviceId, CancellationToken ct)
        {
            using var streamingCall = _client.StreamPlcData(new StreamRequest { DeviceId = deviceId });

            try
            {
                await foreach (var data in streamingCall.ResponseStream.ReadAllAsync(ct))
                {
                    Console.WriteLine($"Data received for {data.DeviceId} at {data.Timestamp}");
                    foreach (var reg in data.Registers)
                    {
                        Console.WriteLine($"  Reg {reg.Address}: {reg.Value} ({reg.Type})");
                    }
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                Console.WriteLine("Stream cancelled.");
            }
        }
    }
}
