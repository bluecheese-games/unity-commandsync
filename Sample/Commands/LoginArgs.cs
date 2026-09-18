namespace BlueCheese.CommandSync.Sample.Commands
{
	public struct LoginArgs
	{
		public string PlayerId;

		override public readonly string ToString() => $"PlayerId={PlayerId}";
	}
}
