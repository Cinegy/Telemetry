using System.Threading.Tasks;

namespace Cinegy.Telemetry
{
    public interface ILogClient
    {
        Task SendMessage(string message);
    }
}