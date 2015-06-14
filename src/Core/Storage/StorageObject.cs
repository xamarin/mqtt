namespace System.Net.Mqtt.Storage
{
	public abstract class StorageObject
	{
		public StorageObject ()
		{
			this.Id = Guid.NewGuid ().ToString ();
		}

		public string Id { get; private set; }
	}
}
