using System.Threading;
using System.Threading.Tasks;

namespace BlueCheese.CommandSync.Core
{
	// Owns the online/offline mode and the synchronization lifecycle: initial load, batch sync,
	// desync recovery and history clearing.
	internal sealed class SyncCoordinator
	{
		private readonly CommandHistoryStore _historyStore;
		private readonly IInternalDataManager _dataManager;
		private readonly ISyncService _syncService;
		private readonly ILogger _logger;

		private int _syncInProgress = 0; // Interlocked flag — prevents concurrent Sync() calls

		public SyncCoordinator(CommandHistoryStore historyStore, IInternalDataManager dataManager, ISyncService syncService, ILogger logger)
		{
			_historyStore = historyStore;
			_dataManager = dataManager;
			_syncService = syncService;
			_logger = logger;
		}

		public SyncMode Mode { get; set; } = SyncMode.Online;

		public async Task LoadData(CancellationToken cancellationToken)
		{
			// If there are commands in the history, use the local data as the source of truth
			if (_historyStore.Count > 0)
			{
				_logger.Log($"Loaded command history with {_historyStore.Count} commands.");
				return;
			}

			if (_syncService == null)
			{
				_logger.Log("No command history and no sync service configured; using local data storage as the source of truth and switching to Offline mode.");
				Mode = SyncMode.Offline;
				return;
			}

			_logger.Log($"No command history found, loading full state from sync service.");

			// If not, we need to fetch the full state from the sync service and import it into the data manager
			var response = await _syncService.FetchAsync(cancellationToken);
			if (response.Success)
			{
				_dataManager.ImportState(response.Data);
				_logger.Log($"Full state loaded and imported into data manager.");
			}
			else
			{
				_logger.LogError($"Failed to fetch full state from sync service: {response.Message}");
				_logger.Log("If there is data in the local data storage, it will be used as the source of truth, and we swith to Offline mode.");
				Mode = SyncMode.Offline;
			}
		}

		public async Task Sync(CancellationToken cancellationToken)
		{
			if (Interlocked.CompareExchange(ref _syncInProgress, 1, 0) != 0)
			{
				_logger.Log("Sync already in progress, skipping concurrent call.");
				return;
			}

			try
			{
				await SyncInternal(cancellationToken);
			}
			finally
			{
				Interlocked.Exchange(ref _syncInProgress, 0);
			}
		}

		private async Task SyncInternal(CancellationToken cancellationToken)
		{
			if (Mode == SyncMode.Offline)
			{
				_logger.Log($"Sync called in Offline mode, but commands executed in Offline mode are not saved to the history, so there is nothing to sync.");
				return;
			}

			if (_historyStore.IsEmpty)
			{
				_logger.Log($"Sync called but there are no commands in the history to sync.");
				return;
			}

			if (_syncService == null)
			{
				_logger.LogError($"Sync service is not configured, cannot sync command history.");
				return;
			}

			var commands = _historyStore.ToArray();
			var clientStateHash = _dataManager.GetStateHash(_historyStore.UpdatedData);

			var request = new SyncRequest
			{
				Commands = commands,
				ClientStateHash = clientStateHash,
			};
			var response = await _syncService.SyncAsync(request, cancellationToken);

			if (response.Success)
			{
				// The server accepted the batch: clear the history so the same commands won't be re-sent.
				_logger.Log($"Sync successful, clearing command history.");
				_historyStore.Clear();
			}
			else if (response.RequiresResync)
			{
				// The server detected a state divergence: recover from the authoritative state.
				_logger.LogWarning($"Server reported a desync: {response.Message}. Re-fetching the authoritative state.");
				await ResyncFromServer(cancellationToken);
			}
			else
			{
				_logger.LogWarning($"Sync failed with message: {response.Message}. Command history will be retained for later retry.");
			}
		}

		private async Task ResyncFromServer(CancellationToken cancellationToken)
		{
			var response = await _syncService.FetchAsync(cancellationToken);
			if (response.Success)
			{
				_dataManager.ImportState(response.Data);
				_historyStore.Clear();
				_logger.Log("Authoritative state re-imported after desync; local command history cleared.");
			}
			else
			{
				_logger.LogError($"Resync failed to fetch the authoritative state: {response.Message}. Command history retained.");
			}
		}
	}
}
