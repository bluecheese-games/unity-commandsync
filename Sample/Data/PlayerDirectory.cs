namespace BlueCheese.CommandSync.Sample.Data
{
	// Small read-only lookup helper shared by commands and by the sample UI, so both look up a
	// profile by Id the same way instead of duplicating the linear search everywhere.
	public static class PlayerDirectory
	{
		public static bool TryFind(this PlayersData data, string playerId, out PlayerProfile profile)
		{
			if (data.Players != null)
			{
				foreach (var candidate in data.Players)
				{
					if (candidate.PlayerId == playerId)
					{
						profile = candidate;
						return true;
					}
				}
			}
			profile = default;
			return false;
		}
	}
}
