namespace System.Net.Mqtt.Storage
{
	internal interface IRepositoryProvider
	{
		IRepository<T> GetRepository<T> () where T : StorageObject;
	}
}
