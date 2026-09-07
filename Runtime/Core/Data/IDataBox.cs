namespace BlueCheese.CommandSync.Core
{
	public interface IDataBox
	{
		bool IsDirty { get; set; }
		object UntypedValue { get; }
	}
}
