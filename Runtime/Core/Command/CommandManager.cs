using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BlueCheese.CommandSync.Core
{
	/// <summary>
	/// Public facade over the CommandSync pipeline. It wires together focused collaborators
	/// (<see cref="CommandRegistry"/>, <see cref="CommandExecutor"/>, <see cref="SignalDispatcher"/>,
	/// <see cref="ExternalEventBus"/>, <see cref="CommandHistoryStore"/>, <see cref="SyncCoordinator"/>)
	/// and exposes a single entry point for command authors.
	///
	/// Threading contract: command execution and history mutation (RegisterCommands, ExecuteCommand,
	/// ReplayCommand, ClearHistory, On/Off, LoadData) are NOT thread-safe and must all run on the
	/// same thread (typically the Unity main thread). Only <see cref="Sync"/> may be awaited
	/// concurrently — it is guarded against re-entrancy and must not run while a command executes.
	/// </summary>
	public class CommandManager
	{
		private readonly CommandRegistry _registry;
		private readonly ExternalEventBus _eventBus;
		private readonly CommandHistoryStore _historyStore;
		private readonly CommandExecutor _executor;
		private readonly SyncCoordinator _syncCoordinator;

		private int _ownerThreadId = -1; // Thread that first used the manager; enforces the single-thread contract

		/// <summary>
		/// Creates a manager. Only <paramref name="dataManager"/> is required; every other dependency
		/// falls back to a sensible default when omitted, so callers can supply just the ones they
		/// want to override — ideally via named arguments.
		/// </summary>
		public CommandManager(
			IDataManager dataManager,
			ILogger logger = null,
			IConfig config = null,
			ITimeProvider timeProvider = null,
			IDataStorage commandsDataStorage = null,
			ISyncService syncService = null,
			ISerializer serializer = null)
		{
			var internalDataManager = dataManager as IInternalDataManager
				?? throw new ArgumentException(
					"dataManager must implement IInternalDataManager. Use DataManager or a compatible implementation.",
					nameof(dataManager));
			logger ??= new NullLogger();
			config ??= new Config();
			timeProvider ??= new SystemTimeProvider();
			serializer ??= new NewtonsoftJsonSerializer();
			commandsDataStorage ??= new InMemoryDataStorage();

			if (ReferenceEquals(commandsDataStorage, internalDataManager.Storage))
			{
				logger.LogWarning(
					"CommandManager: commandsDataStorage is the same instance as the DataManager's storage. " +
					"Pass a separate IDataStorage for command history to avoid key collisions.");
			}

			_registry = new CommandRegistry();
			_eventBus = new ExternalEventBus();
			_historyStore = new CommandHistoryStore(commandsDataStorage);
			var signalDispatcher = new SignalDispatcher(_registry, logger, config, timeProvider);
			_executor = new CommandExecutor(_registry, signalDispatcher, _eventBus, _historyStore, internalDataManager, logger, config, timeProvider, serializer);
			_syncCoordinator = new SyncCoordinator(_historyStore, internalDataManager, syncService, logger);
		}

		public SyncMode Mode
		{
			get => _syncCoordinator.Mode;
			set => _syncCoordinator.Mode = value;
		}

		public IReadOnlyList<CommandCall> History => _historyStore.ToArray();

		public IEnumerable<Type> UpdatedDataTypes => _historyStore.UpdatedData;

		public void RegisterCommands(Assembly assembly)
		{
			AssertOwnerThread();
			_registry.RegisterFromAssembly(assembly);
		}

		/// <summary>
		/// Subscribes to external events of type <typeparamref name="T"/> raised by commands via <c>ctx.Events.Raise</c>.
		/// Handlers are invoked after the command and all its signal handlers have completed.
		/// Returns a handle whose <see cref="IDisposable.Dispose"/> unsubscribes the handler — useful for
		/// lambdas that cannot be passed back to <see cref="Off{T}"/>.
		/// </summary>
		public IDisposable On<T>(Action<T> handler) where T : struct
		{
			AssertOwnerThread();
			return _eventBus.On(handler);
		}

		/// <summary>
		/// Unsubscribes a previously registered external event handler.
		/// </summary>
		public void Off<T>(Action<T> handler) where T : struct
		{
			AssertOwnerThread();
			_eventBus.Off(handler);
		}

		public Task LoadData(CancellationToken cancellationToken = default)
		{
			AssertOwnerThread();
			return _syncCoordinator.LoadData(cancellationToken);
		}

		public void ExecuteCommand(string commandName, object args)
		{
			AssertOwnerThread();
			_executor.Execute(Guid.NewGuid(), commandName, args);
		}

		public void ExecuteCommand(string commandName)
		{
			AssertOwnerThread();
			_executor.Execute(Guid.NewGuid(), commandName, null);
		}

		public void ExecuteCommand<T>(Action<CommandContext, T> command, T args)
		{
			AssertOwnerThread();
			_executor.Execute(Guid.NewGuid(), command.Method.Name, args);
		}

		public void ExecuteCommand(Action<CommandContext> command)
		{
			AssertOwnerThread();
			_executor.Execute(Guid.NewGuid(), command.Method.Name, null);
		}

		public void ReplayCommand(CommandCall call)
		{
			AssertOwnerThread();
			_executor.Execute(call.Id, call.CommandName, call.Args, saveCommand: false);
		}

		public Task Sync(CancellationToken cancellationToken = default) => _syncCoordinator.Sync(cancellationToken);

		public void ClearHistory()
		{
			AssertOwnerThread();
			_historyStore.Clear();
		}

		// Enforces the documented single-thread contract in development builds.
		[Conditional("DEBUG")]
		private void AssertOwnerThread()
		{
			int current = Environment.CurrentManagedThreadId;
			if (_ownerThreadId == -1)
			{
				_ownerThreadId = current;
			}
			else if (_ownerThreadId != current)
			{
				throw new InvalidOperationException(
					$"CommandManager is not thread-safe: it was first used on thread {_ownerThreadId} " +
					$"but accessed from thread {current}. Call all command methods from the same thread.");
			}
		}
	}
}
