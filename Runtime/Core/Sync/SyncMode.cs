using System;

namespace BlueCheese.CommandSync.Core
{
	[Serializable]
	public enum SyncMode
	{
		Online, // Commands are executed immediately and saved to the history for later processing (e.g. syncing with server)
		Offline, // Commands are executed immediately but not saved to the history, so they won't be processed later (e.g. for syncing with server)
	}
}
