using System.Threading.Tasks;

namespace ModBus.Interfaces
{
    /// <summary>
    /// Interface para abstração da comunicação Modbus.
    /// </summary>
    public interface IModBusClient
    {
        /// <summary>
        /// Estabelece conexão com o servidor Modbus TCP.
        /// </summary>
        /// <param name="ipAddress">Endereço IP do servidor.</param>
        /// <param name="port">Porta de conexão (padrão 502).</param>
        Task ConnectAsync(string ipAddress, int port);

        /// <summary>
        /// Encerra a conexão ativa com o servidor.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Indica se o cliente está conectado ao servidor.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Lê o estado de bobinas (Coils - Função 01).
        /// </summary>
        Task<bool[]> ReadCoilsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints);

        /// <summary>
        /// Lê o estado de entradas discretas (Inputs - Função 02).
        /// </summary>
        Task<bool[]> ReadInputsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints);

        /// <summary>
        /// Lê registradores de entrada (Input Registers - Função 04).
        /// </summary>
        Task<ushort[]> ReadInputRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints);

        /// <summary>
        /// Lê registradores de retenção (Holding Registers - Função 03).
        /// </summary>
        Task<ushort[]> ReadHoldingRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints);

        /// <summary>
        /// Escreve em um registrador de retenção (Holding Register - Função 06).
        /// </summary>
        Task WriteHoldingRegisterAsync(byte slaveAddress, ushort registerAddress, ushort value);

        /// <summary>
        /// Escreve em uma bobina (Coil - Função 05).
        /// </summary>
        Task WriteCoilAsync(byte slaveAddress, ushort registerAddress, bool value);
    }
}
