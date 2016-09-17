using System.Threading.Tasks;

namespace System.Net.Mqtt.Flows
{
    internal interface IServerPublishReceiverFlow : IProtocolFlow
    {
        Task SendWillAsync (string clientId);
    }
}
