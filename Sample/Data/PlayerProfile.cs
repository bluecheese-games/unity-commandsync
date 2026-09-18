using System;

namespace BlueCheese.CommandSync.Sample.Data
{
	// Deliberately has no "created at" / timestamp field: CommandManager.ReplayCommand replays a command
	// with the executor's live ITimeProvider, NOT the Timestamp recorded on the original CommandCall
	// (that field is display-only, see CommandCall.ToString()). Any wall-clock value written into this
	// struct via context.Time.UtcNow would therefore differ between the client's original execution and
	// the (mock) server's later replay, and desync every single time — this bit us with CreatedAtUtc
	// (removed). Only write values derived from context.RNG (seeded from the command Id, so identical
	// on both sides) or from the command's own args into replicated/hashed data.
	[Serializable]
	public struct PlayerProfile
	{
		public string PlayerId;
		public string PlayerName;
		public long XP;
		public int LoginCount;
	}
}
