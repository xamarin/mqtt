namespace System.Net.Mqtt.Sdk.Storage
{
	internal interface IRepositoryProvider
	{
		IRepository<T> GetRepository<T> () where T : IStorageObject;
	}
}
