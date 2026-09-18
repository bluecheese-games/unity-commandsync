namespace BlueCheese.CommandSync.Sample.Commands
{
	public struct SetPlayerNameArgs
	{
		public string PlayerId;
		public string PlayerName;

		override public readonly string ToString() => $"PlayerId={PlayerId} PlayerName={PlayerName}";
	}
}
