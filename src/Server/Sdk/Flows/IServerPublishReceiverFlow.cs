using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Flows
{
    internal interface IServerPublishReceiverFlow : IProtocolFlow
    {
        Task SendWillAsync (string clientId);
    }
}
