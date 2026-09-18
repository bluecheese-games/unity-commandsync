namespace BlueCheese.CommandSync.Sample.Commands
{
	public struct AddXPArgs
	{
		public string PlayerId;
		public long Amount;

		override public readonly string ToString() => $"PlayerId={PlayerId} Amount={Amount}";
	}
}
