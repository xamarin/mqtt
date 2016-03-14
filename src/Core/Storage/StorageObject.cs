namespace System.Net.Mqtt.Storage
{
	internal abstract class StorageObject
	{
		public StorageObject ()
		{
			Id = Guid.NewGuid ().ToString ();
		}

		public string Id { get; private set; }
	}
}
