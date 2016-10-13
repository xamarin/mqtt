namespace System.Net.Mqtt.Sdk.Storage
{
	internal abstract class StorageObject
	{
		public StorageObject ()
		{
			Id = Guid.NewGuid ().ToString ();
		}

		public string Id { get; }
	}
}
