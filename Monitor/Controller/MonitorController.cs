using Microsoft.AspNetCore.Mvc;
using ModBus.Interfaces;
using System.Threading.Tasks;

namespace Monitor.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitorController : ControllerBase
    {
        private readonly IModBusClient _modbusClient;

        public MonitorController(IModBusClient modbusClient)
        {
            _modbusClient = modbusClient;
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                Status = "Running",
                ModbusConnected = _modbusClient.IsConnected,
                Timestamp = System.DateTime.UtcNow
            });
        }
    }
}
