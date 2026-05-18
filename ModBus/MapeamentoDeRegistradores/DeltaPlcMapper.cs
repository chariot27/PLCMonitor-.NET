using System.Collections.Generic;
using System.Linq;
using ModBus.Interfaces;

namespace ModBus.MapeamentoDeRegistradores
{
    /// <summary>
    /// Mapeador de registradores específico para CLPs Delta DVP-SE.
    /// Traduz nomes simbólicos em endereços físicos Modbus.
    /// </summary>
    public class DeltaPlcMapper : IRegisterMapper
    {
        private readonly Dictionary<string, ushort> _mappings = new()
        {
            { "X0", 0x0000 },      // Endereço base para entradas discretas no mapeamento interno
            { "X1", 0x0001 },
            { "Y0", 0x0500 },      // Endereço Modbus para Y0 em alguns modelos (Hex)
            { "C251", 0x0E00 },    // Contador de alta velocidade
            { "RTC_SEC", 0x0F00 }, // Registrador RTC: Segundos
            { "RTC_MIN", 0x0F01 }, // Registrador RTC: Minutos
            { "RTC_HOUR", 0x0F02 } // Registrador RTC: Horas
        };

        /// <summary>
        /// Obtém o dicionário completo de mapeamentos.
        /// </summary>
        public IDictionary<string, ushort> GetMappings() => _mappings;

        /// <summary>
        /// Converte um endereço físico em um nome simbólico.
        /// </summary>
        public string MapAddressToName(ushort address)
        {
            return _mappings.FirstOrDefault(x => x.Value == address).Key ?? address.ToString();
        }

        /// <summary>
        /// Converte um nome simbólico em um endereço físico Modbus.
        /// </summary>
        public ushort MapNameToAddress(string name)
        {
            if (_mappings.TryGetValue(name, out var address))
                return address;
            return 0;
        }
    }
}
