using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    public interface IFactory<T> where T : class
    {
        Task<T> CreateAsync (ProtocolConfiguration configuration);
    }
}
