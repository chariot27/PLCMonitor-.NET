using System.Threading;
using System.Threading.Tasks;

namespace IoC.Interfaces
{
    /// <summary>
    /// Interface para a aplicação principal de monitoramento.
    /// </summary>
    public interface IMonitorApp
    {
        /// <summary>
        /// Inicia o ciclo de monitoramento do CLP.
        /// </summary>
        /// <param name="cancellationToken">Token para cancelamento da operação.</param>
        Task RunAsync(CancellationToken cancellationToken);
    }
}
