using Microsoft.AspNetCore.Mvc;
using ModBus.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace Gatilho.Controllers
{
    /// <summary>
    /// Controlador responsável por disparar comandos (triggers) no CLP.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TriggerController : ControllerBase
    {
        private readonly IModBusClient _modbusClient;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="TriggerController"/>.
        /// </summary>
        public TriggerController(IModBusClient modbusClient, IConfiguration configuration)
        {
            _modbusClient = modbusClient;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TriggerRequest request)
        {
            if (string.IsNullOrEmpty(request.Out))
                return BadRequest("Output field 'out' is required.");

            // Map string like "y0" to address
            // Assuming Y0 is 0x0500 (1280)
            if (!request.Out.ToLower().StartsWith("y"))
                return BadRequest("Only Y outputs are supported (e.g., 'y0').");

            if (!ushort.TryParse(request.Out.Substring(1), out ushort index))
                return BadRequest("Invalid output index.");

            ushort baseAddress = 1280; // Y0
            ushort targetAddress = (ushort)(baseAddress + index);

            try
            {
                if (!_modbusClient.IsConnected)
                {
                    var ip = _configuration["Modbus:IpAddress"] ?? "192.168.0.10";
                    var port = int.Parse(_configuration["Modbus:Port"] ?? "502");
                    await _modbusClient.ConnectAsync(ip, port);
                }

                await _modbusClient.WriteCoilAsync(1, targetAddress, true);
                
                // Fire and forget: Turn off after 10 seconds
                _ = Task.Run(async () => {
                    await Task.Delay(10000);
                    try {
                        await _modbusClient.WriteCoilAsync(1, targetAddress, false);
                    } catch {
                        // Log failure if necessary
                    }
                });
                
                return Ok(new { Status = "Success", Address = targetAddress, Value = true, Duration = "10s" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Modbus error: {ex.Message}");
            }
        }
    }

    public class TriggerRequest
    {
        [JsonPropertyName("out")]
        public string Out { get; set; } = string.Empty;
    }
}
