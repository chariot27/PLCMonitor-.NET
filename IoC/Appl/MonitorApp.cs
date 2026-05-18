using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IoC.Interfaces;
using ModBus.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace IoC.Appl
{
    /// <summary>
    /// Implementação principal da lógica de monitoramento do CLP.
    /// Realiza a varredura periódica de diversos tipos de registradores e detecta mudanças de estado.
    /// </summary>
    public class MonitorApp : IMonitorApp
    {
        private readonly IModBusClient _modbusClient;
        private readonly IRegisterMapper _registerMapper;
        private readonly ILogger<MonitorApp> _logger;
        private readonly IConfiguration _configuration;

        private bool _supportsSpecialRelays = true;
        private bool _supportsSpecialRegisters = true;
        private bool _supportsDiscreteInputs = true;
        private bool _supportsCoils = true;
        private bool _supportsHoldingRegisters = true;
        private bool _supportsInputRegisters = true;

        private readonly Dictionary<string, object> _lastStates = new();
        private readonly HashSet<string> _activeIOs = new();

        /// <summary>
        /// Inicializa uma nova instância de <see cref="MonitorApp"/>.
        /// </summary>
        public MonitorApp(IModBusClient modbusClient, IRegisterMapper registerMapper, ILogger<MonitorApp> logger, IConfiguration configuration)
        {
            _modbusClient = modbusClient;
            _registerMapper = registerMapper;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Monitor Application...");

            var ip = _configuration["Modbus:IpAddress"] ?? "127.0.0.1";
            var port = int.Parse(_configuration["Modbus:Port"] ?? "502");

            try
            {
                await _modbusClient.ConnectAsync(ip, port);
                _logger.LogInformation("Connected to Modbus Server at {Ip}:{Port}", ip, port);

                bool isFirstScan = true;
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_supportsDiscreteInputs) await MonitorDigitalInputs(cancellationToken);
                    if (_supportsCoils) await MonitorCoils(cancellationToken);
                    if (_supportsHoldingRegisters) await MonitorAnalogHolding(cancellationToken);
                    if (_supportsInputRegisters) await MonitorInputRegisters(cancellationToken);
                    if (_supportsSpecialRelays || _supportsSpecialRegisters) await MonitorPlcStatus(cancellationToken);
                    
                    if (isFirstScan)
                    {
                        PrintSymbolicLadder();
                        isFirstScan = false;
                    }

                    await Task.Delay(50, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Monitor Application");
            }
            finally
            {
                await _modbusClient.DisconnectAsync();
                _logger.LogInformation("Disconnected from Modbus Server.");
            }
        }

        private async Task MonitorDigitalInputs(CancellationToken ct)
        {
            try
            {
                // Expanded scan: X0-X63 (64 inputs)
                var inputs = await _modbusClient.ReadInputsAsync(1, 1024, 64);
                for (int i = 0; i < inputs.Length; i++)
                {
                    DetectAndLogChange($"X{i}", inputs[i]);
                }
            }
            catch (Exception ex) when (IsIllegalFunction(ex))
            {
                _logger.LogWarning("Discrete Inputs (X) not supported at this range. Skipping.");
                _supportsDiscreteInputs = false;
            }
            catch (Exception ex)
            {
                _logger.LogTrace("Error reading digital inputs: {Msg}", ex.Message);
            }
        }

        private async Task MonitorCoils(CancellationToken ct)
        {
            try
            {
                // Expanded scan: Y0-Y63 (64 coils)
                var coils = await _modbusClient.ReadCoilsAsync(1, 1280, 64);
                for (int i = 0; i < coils.Length; i++)
                {
                    DetectAndLogChange($"Y{i}", coils[i]);
                }
            }
            catch (Exception ex) when (IsIllegalFunction(ex))
            {
                _logger.LogWarning("Coils (Y) not supported at this range. Skipping.");
                _supportsCoils = false;
            }
            catch (Exception ex)
            {
                _logger.LogTrace("Error reading coils: {Msg}", ex.Message);
            }
        }

        private async Task MonitorAnalogHolding(CancellationToken ct)
        {
            try
            {
                var registers = await _modbusClient.ReadHoldingRegistersAsync(1, 0, 10);
                for (int i = 0; i < registers.Length; i++)
                {
                    DetectAndLogChange($"D{i}", registers[i]);
                }
            }
            catch (Exception ex) when (IsIllegalFunction(ex))
            {
                _logger.LogWarning("Holding Registers (D) not supported by this device. Skipping.");
                _supportsHoldingRegisters = false;
            }
            catch (Exception ex)
            {
                _logger.LogTrace("Error reading holding registers: {Msg}", ex.Message);
            }
        }

        private async Task MonitorInputRegisters(CancellationToken ct)
        {
            try
            {
                var registers = await _modbusClient.ReadInputRegistersAsync(1, 0, 10);
                for (int i = 0; i < registers.Length; i++)
                {
                    DetectAndLogChange($"AI{i}", registers[i]);
                }
            }
            catch (Exception ex) when (IsIllegalFunction(ex))
            {
                _logger.LogWarning("Input Registers (AI) not supported by this device. Skipping.");
                _supportsInputRegisters = false;
            }
            catch (Exception ex)
            {
                _logger.LogTrace("Error reading input registers: {Msg}", ex.Message);
            }
        }

        private async Task MonitorPlcStatus(CancellationToken ct)
        {
            try
            {
                // M1000 is the "Always ON" bit during RUN. 
                // For Delta DVP: M0 starts at 2048 (0x0800), so M1000 is at 3048.
                if (_supportsSpecialRelays)
                {
                    var statusRelays = await _modbusClient.ReadCoilsAsync(1, 3048, 1);
                    bool m1000 = statusRelays.Length > 0 && statusRelays[0];
                    
                    // Fallback: Check address 2048 (M0) which is sometimes used for RUN status in specific modes
                    var runStatus = await _modbusClient.ReadCoilsAsync(1, 2048, 1);
                    bool isRunBitSet = runStatus.Length > 0 && runStatus[0];

                    bool isRunning = m1000 || isRunBitSet;
                    DetectAndLogStatus("PLC_STATE", isRunning ? "RUNNING (Ladder Active)" : "STOPPED (Ladder Idle)");
                }

                // D1010 is address 1010 - Current Scan Time (0.1ms)
                if (_supportsSpecialRegisters)
                {
                    var scanTimeRegs = await _modbusClient.ReadHoldingRegistersAsync(1, 1010, 1);
                    if (scanTimeRegs.Length > 0)
                    {
                        double scanTimeMs = scanTimeRegs[0] / 10.0;
                        DetectAndLogChange("Ladder_ScanTime_MS", scanTimeMs);
                    }
                }
            }
            catch (Exception ex) when (IsIllegalFunction(ex))
            {
                _logger.LogWarning("Special Status Registers/Relays not supported by this device. Skipping.");
                _supportsSpecialRelays = false;
                _supportsSpecialRegisters = false;
            }
            catch (Exception ex)
            {
                _logger.LogTrace("Error reading PLC status: {Msg}", ex.Message);
            }
        }

        private void DetectAndLogStatus(string key, string newStatus)
        {
            if (!_lastStates.TryGetValue(key, out var lastStatus) || (string)lastStatus != newStatus)
            {
                _logger.LogInformation("[{Time}] {Key}: {NewStatus}", 
                    DateTime.Now.ToString("HH:mm:ss.fff"), key, newStatus);
                _lastStates[key] = newStatus;
            }
        }

        private bool IsIllegalFunction(Exception ex)
        {
            // NModbus SlaveException with ExceptionCode 1 means Illegal Function
            return ex.Message.Contains("Function Code: 132") || ex.Message.Contains("Exception Code: 1");
        }

        private void DetectAndLogChange(string key, object newValue)
        {
            if (!_lastStates.TryGetValue(key, out var lastValue) || !EqualityComparer<object>.Default.Equals(lastValue, newValue))
            {
                // Register any X or Y as discovered if it's our first time seeing it
                if (key.StartsWith("X") || key.StartsWith("Y"))
                {
                    _activeIOs.Add(key);
                }

                _logger.LogInformation("[{Time}] {Key} changed: {OldValue} -> {NewValue}", 
                    DateTime.Now.ToString("HH:mm:ss.fff"), key, lastValue ?? "INIT", newValue);
                _lastStates[key] = newValue;

                // Refresh ladder view on any change or periodic pulse
                if (key.StartsWith("X") || key.StartsWith("Y") || key == "PLC_STATE")
                {
                    PrintSymbolicLadder();
                }
            }
        }

        private void PrintSymbolicLadder()
        {
            var plcState = (string)(_lastStates.GetValueOrDefault("PLC_STATE") ?? "UNKNOWN");
            
            // Only show ladder preview if the PLC is actually running the ladder code
            if (!plcState.Contains("RUNNING")) 
            {
                return;
            }

            // Get active ports or default to first 8 if none discovered
            var activeXs = _activeIOs.Where(k => k.StartsWith("X")).OrderBy(k => int.Parse(k.Substring(1))).Take(8).ToList();
            if (!activeXs.Any()) activeXs = Enumerable.Range(0, 8).Select(i => $"X{i}").ToList();

            var activeYs = _activeIOs.Where(k => k.StartsWith("Y")).OrderBy(k => int.Parse(k.Substring(1))).Take(8).ToList();
            if (!activeYs.Any()) activeYs = Enumerable.Range(0, 8).Select(i => $"Y{i}").ToList();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n================= LIVE LADDER PREVIEW =================");
            Console.WriteLine($" PLC STATE: {plcState} | TIME: {DateTime.Now:HH:mm:ss.fff}");
            Console.WriteLine("-------------------------------------------------------");
            
            // Build Inputs Row
            string xRung = " Inputs:  ";
            foreach (var x in activeXs)
            {
                bool state = GetState(x);
                xRung += state ? $"[X] " : $"[ ] ";
            }
            Console.WriteLine(xRung);
            Console.WriteLine("          " + string.Join("  ", activeXs));

            Console.WriteLine("             |");
            Console.WriteLine("             v");

            // Build Outputs Row
            string yRung = " Outputs: ";
            foreach (var y in activeYs)
            {
                bool state = GetState(y);
                yRung += state ? $"(X) " : $"( ) ";
            }
            Console.WriteLine(yRung);
            Console.WriteLine("          " + string.Join("  ", activeYs));
            
            Console.WriteLine("=======================================================\n");
            Console.ResetColor();
        }

        private bool GetState(string key)
        {
            return _lastStates.TryGetValue(key, out var val) && val is bool b && b;
        }
    }
}
