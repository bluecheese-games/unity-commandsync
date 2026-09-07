using System;

namespace BlueCheese.CommandSync.Core
{
	[Serializable]
	public class SyncResponse : Response
	{
		/// <summary>
		/// Set by the server when the client state diverged from the authoritative state and the
		/// client must recover by re-fetching the full state rather than simply retrying the batch.
		/// </summary>
		public bool RequiresResync { get; set; }

		static public SyncResponse Ok() => new()
		{
			Success = true
		};

		static public SyncResponse Fail(string errorMessage) => new()
		{
			Success = false,
			Message = errorMessage
		};

		static public SyncResponse Desync(string errorMessage) => new()
		{
			Success = false,
			RequiresResync = true,
			Message = errorMessage
		};
	}
}
