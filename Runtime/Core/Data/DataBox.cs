namespace BlueCheese.CommandSync.Core
{
	public class DataBox<T> : IDataBox
	{
		public T Value;

		public bool IsDirty { get; set; }
		object IDataBox.UntypedValue => Value;
	}
}
