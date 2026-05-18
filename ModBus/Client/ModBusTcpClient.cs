using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using ModBus.Interfaces;
using NModbus;
using NModbus.Device;

namespace ModBus.Client
{
    public class ModBusTcpClient : IModBusClient, IDisposable
    {
        private TcpClient? _tcpClient;
        private IModbusMaster? _modbusMaster;
        private readonly ModbusFactory _factory;

        public ModBusTcpClient()
        {
            _factory = new ModbusFactory();
        }

        public bool IsConnected => _tcpClient?.Connected ?? false;

        public async Task ConnectAsync(string ipAddress, int port)
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(ipAddress, port);
            _modbusMaster = _factory.CreateMaster(_tcpClient);
        }

        public Task DisconnectAsync()
        {
            _tcpClient?.Close();
            _modbusMaster?.Dispose();
            return Task.CompletedTask;
        }

        public async Task<bool[]> ReadCoilsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (_modbusMaster == null) throw new InvalidOperationException("Not connected");
            return await _modbusMaster.ReadCoilsAsync(slaveAddress, startAddress, numberOfPoints);
        }

        public async Task<bool[]> ReadInputsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (_modbusMaster == null) throw new InvalidOperationException("Not connected");
            return await _modbusMaster.ReadInputsAsync(slaveAddress, startAddress, numberOfPoints);
        }

        public async Task<ushort[]> ReadInputRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (_modbusMaster == null) throw new InvalidOperationException("Not connected");
            return await _modbusMaster.ReadInputRegistersAsync(slaveAddress, startAddress, numberOfPoints);
        }

        public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (_modbusMaster == null) throw new InvalidOperationException("Not connected");
            return await _modbusMaster.ReadHoldingRegistersAsync(slaveAddress, startAddress, numberOfPoints);
        }

        public async Task WriteHoldingRegisterAsync(byte slaveAddress, ushort registerAddress, ushort value)
        {
            if (_modbusMaster == null) throw new InvalidOperationException("Not connected");
            await _modbusMaster.WriteSingleRegisterAsync(slaveAddress, registerAddress, value);
        }

        public async Task WriteCoilAsync(byte slaveAddress, ushort registerAddress, bool value)
        {
            if (_modbusMaster == null) throw new InvalidOperationException("Not connected");
            await _modbusMaster.WriteSingleCoilAsync(slaveAddress, registerAddress, value);
        }

        public void Dispose()
        {
            _tcpClient?.Dispose();
            _modbusMaster?.Dispose();
        }
    }
}
