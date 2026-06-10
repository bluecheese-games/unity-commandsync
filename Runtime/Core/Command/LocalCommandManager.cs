//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BlueCheese.LocalCommands.Core
{

	public class LocalCommandManager
	{
		private const string CommandHistoryPrefKey = "LocalCommand_History";

		private readonly IInternalDataManager _dataManager;
		private readonly ILogger _logger;
		private readonly IConfig _config;
		private readonly ITimeProvider _timeProvider;
		private readonly ISyncService _syncService;
		private readonly IDataStorage _commandsDataStorage; // Separate data storage for the command history, so that it can be managed independently from the user data storage

		private readonly Dictionary<string, CommandDef> _definitions = new(); // Registered command definitions, keyed by command name
		private readonly Dictionary<Type, List<EventHandlerDef>> _eventHandlers = new(); // Event handler definitions, keyed by event payload type
		private readonly CommandHistory _history = new(); // History of executed commands that have updated the user data, to be processed later
		private int _syncInProgress = 0; // Interlocked flag — prevents concurrent Sync() calls

		public SyncMode Mode { get; set; } = SyncMode.Online;

		public IReadOnlyList<CommandCall> History => _history.ToArray();

		public IEnumerable<Type> UpdatedDataTypes => _history.UpdatedData;

		public LocalCommandManager(IDataManager dataManager, ILogger logger, IConfig config, ITimeProvider timeProvider, IDataStorage commandsDataStorage, ISyncService syncService = null)
		{
			_dataManager = dataManager as IInternalDataManager
				?? throw new ArgumentException(
					"dataManager must implement IInternalDataManager. Use DataManager or a compatible implementation.",
					nameof(dataManager));
			_logger = logger ?? new NullLogger();
			_config = config ?? new Config();
			_timeProvider = timeProvider ?? new TimeProvider();
			_syncService = syncService;
			_commandsDataStorage = commandsDataStorage ?? new InMemoryDataStorage();

			if (ReferenceEquals(_commandsDataStorage, _dataManager.Storage))
			{
				_logger.LogWarning(
					"LocalCommandManager: commandsDataStorage is the same instance as the DataManager's storage. " +
					"Pass a separate IDataStorage for command history to avoid key collisions.");
			}

			_history = LoadHistory();
		}

		public void RegisterCommands(Assembly assembly)
		{
			// Find all static methods that have the LocalCommandAttribute or the LocalEventHandlerAttribute
			foreach (var type in assembly.GetTypes())
			{
				foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
				{
					if (method.GetCustomAttribute<LocalCommandAttribute>() != null)
					{
						RegisterCommand(method);
					}
					else if (method.GetCustomAttribute<LocalEventHandlerAttribute>() != null)
					{
						RegisterEventHandler(method);
					}
				}
			}
		}

		private void RegisterEventHandler(MethodInfo method)
		{
			var parameters = method.GetParameters();
			if (parameters.Length != 2 || parameters[0].ParameterType != typeof(Context))
			{
				throw new CommandRegistrationException(
					$"Event handler '{method.Name}' must have exactly two parameters: Context and the event payload struct.");
			}

			var eventType = parameters[1].ParameterType;
			if (!eventType.IsValueType)
			{
				throw new CommandRegistrationException(
					$"Event handler '{method.Name}': event payload type '{eventType.Name}' must be a struct.");
			}

			var handlerDef = new EventHandlerDef(method.Name, eventType, method);

			if (!_eventHandlers.TryGetValue(eventType, out var handlers))
			{
				handlers = new List<EventHandlerDef>();
				_eventHandlers[eventType] = handlers;
			}
			handlers.Add(handlerDef);
		}

		private void RegisterCommand(MethodInfo method)
		{
			var attr = method.GetCustomAttribute<LocalCommandAttribute>();
			var commandName = attr.Name ?? method.Name;
			if (_definitions.ContainsKey(commandName))
			{
				throw new CommandRegistrationException($"A command with the name '{commandName}' is already registered.");
			}

			var parameters = method.GetParameters();
			if (parameters[0].ParameterType != typeof(Context))
			{
				throw new CommandRegistrationException($"Method '{method.Name}' must have a Context parameter as the first argument.");
			}
			if (parameters.Length > 2)
			{
				throw new CommandRegistrationException($"Method '{method.Name}' has too many parameters. Only Context and an optional args parameter are allowed.");
			}

			var argsType = parameters.Length == 2 ? parameters[1].ParameterType : null;
			var commandInfo = new CommandDef(commandName, method.DeclaringType, argsType, method);
			_definitions[commandName] = commandInfo;
		}

		public async Task LoadData()
		{
			// If there are commands in the history, use the local data as the source of truth
			if (_history.Queue.Count > 0)
			{
				_logger.Log($"Loaded command history with {_history.Queue.Count} commands.");
				return;
			}

			_logger.Log($"No command history found, loading full state from sync service.");

			// If not, we need to fetch the full state from the sync service and import it into the data manager
			var response = await _syncService.FetchAsync();
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

		public void ExecuteCommand(string commandName, object args) => ExecuteCommandImpl(Guid.NewGuid(), commandName, args);

		public void ExecuteCommand(string commandName) => ExecuteCommandImpl(Guid.NewGuid(), commandName, null);

		public void ExecuteCommand<T>(Action<Context, T> command, T args) => ExecuteCommandImpl(Guid.NewGuid(), command.Method.Name, args);

		public void ExecuteCommand(Action<Context> command) => ExecuteCommandImpl(Guid.NewGuid(), command.Method.Name, null);

		public void ReplayCommand(CommandCall call) => ExecuteCommandImpl(call.Id, call.CommandName, call.Args, saveCommand: false);

		public void ExecuteCommandImpl(Guid commandId, string commandName, object args, bool saveCommand = true)
		{
			// Look up the command definition
			if (!_definitions.TryGetValue(commandName, out var commandDef))
			{
				throw new CommandNotFoundException(commandName);
			}

			// Validate arguments
			if (commandDef.ArgsType == null && args != null)
			{
				throw new CommandArgumentException($"Command '{commandName}' does not accept any arguments, but arguments were provided.");
			}
			if (commandDef.ArgsType != null && args == null)
			{
				throw new CommandArgumentException($"Command '{commandName}' requires an argument of type {commandDef.ArgsType}, but no arguments were provided.");
			}

			// Deserialize JObject args back to the expected type when replaying from persisted history.
			// Kept here in the infrastructure layer to avoid coupling CommandDef to Newtonsoft.
			if (args is Newtonsoft.Json.Linq.JObject jObj && commandDef.ArgsType != null)
			{
				args = jObj.ToObject(commandDef.ArgsType);
			}

			// Create the context for this command call
			var state = new ExecutionState();
			var data = new Data(_dataManager, state);
			var rng = new RandomGenerator();
			rng.Init(commandId.GetHashCode()); // Initialize RNG with a seed derived from the command ID to ensure deterministic random values for the same command call
			var eventContext = new EventContext();
			var context = new Context(_config, data, _logger, state, _timeProvider, rng, eventContext);

			// Execute the command
			commandDef.Execute(context, args);

			if (state.Result == CommandExecutionResult.Failure)
			{
				_logger.LogWarning($"Command '{commandName}' execution failed: {state.FailureMessage}");
				return;
			}

			// Dispatch any events raised during command execution, cascading until the queue is empty
			ProcessPendingEvents(commandId, context, eventContext, depth: 0);

			if (state.DataHasBeenUpdated)
			{
				// Flush the data storage to ensure that the updated data is saved before the next command call is executed.
				var updatedDataTypes = _dataManager.Flush();
				SaveUpdatedDataTypes(updatedDataTypes);

				// If the user data was updated, we need to save this command call so that it can be processed later.
				if (saveCommand)
				{
					EnqueueCommandCall(commandId, commandName, args);
					_logger.Log($"Command '{commandName}' executed and data was updated. Call has been enqueued.");
				}

				SaveHistory(_history);
			}
		}

		private void ProcessPendingEvents(Guid rootCommandId, Context context, EventContext eventContext, int depth)
		{
			if (depth > EventContext.MaxCascadeDepth)
			{
				_logger.LogError(
					$"Event cascade depth exceeded the maximum of {EventContext.MaxCascadeDepth}. " +
					"Check for circular event handler chains. Remaining events will be discarded.");
				// Drain the queue entirely so that all parent while-loops also terminate.
				// Without this, the parent loop would keep dequeueing events added by the
				// handler that triggered this depth-exceeded call, causing an infinite loop.
				while (eventContext.TryDequeue(out _)) { }
				return;
			}

			// Track occurrence index per event type to keep handler IDs deterministic
			// across multiple raises of the same event type within one command execution.
			var occurrenceCounters = new Dictionary<Type, int>();

			while (eventContext.TryDequeue(out var pendingEvent))
			{
				if (!_eventHandlers.TryGetValue(pendingEvent.EventType, out var handlers))
				{
					continue; // No handlers registered for this event type
				}

				occurrenceCounters.TryGetValue(pendingEvent.EventType, out int occurrenceIndex);
				occurrenceCounters[pendingEvent.EventType] = occurrenceIndex + 1;

				foreach (var handler in handlers)
				{
					// Derive a deterministic ID from the root command ID, event type and occurrence index
					// so that the handler's RNG seed is stable across replays.
					var handlerId = DeriveHandlerId(rootCommandId, pendingEvent.EventType, occurrenceIndex);

					var handlerRng = new RandomGenerator();
					handlerRng.Init(handlerId.GetHashCode());

					// Handlers share the same state and data as the triggering command
					var handlerContext = new Context(
						context.Config, context.Data, context.Logger,
						context.State, context.Time, handlerRng, eventContext);

					handler.Execute(handlerContext, pendingEvent.Payload);

					if (context.State.Result == CommandExecutionResult.Failure)
					{
						_logger.LogWarning($"Event handler '{handler.Name}' failed: {context.State.FailureMessage}");
						return;
					}

					// If the handler raised new events, process them at the next depth level
					if (eventContext.HasPendingEvents)
					{
						ProcessPendingEvents(rootCommandId, handlerContext, eventContext, depth + 1);
					}
				}
			}
		}

		private static Guid DeriveHandlerId(Guid rootCommandId, Type eventType, int occurrenceIndex)
		{
			// Combine the root command ID, event type full name and occurrence index into a
			// deterministic hash so that replaying the same command always produces the same handler IDs.
			var seed = HashUtility.GetDeterministicHashCode(
				$"{rootCommandId}:{eventType.FullName}:{occurrenceIndex}");
			var bytes = new byte[16];
			BitConverter.GetBytes(seed).CopyTo(bytes, 0);
			return new Guid(bytes);
		}

		private void EnqueueCommandCall(Guid commandId, string commandName, object args)
		{
			var call = new CommandCall
			{
				Id = commandId,
				CommandName = commandName,
				Args = args,
				Timestamp = _timeProvider.UtcNow.ToUnixTimeMilliseconds(),
				ConfigVersion = _config.Version,
			};

			_history.Enqueue(call);
		}

		private void SaveUpdatedDataTypes(IEnumerable<Type> updatedDataTypes)
		{
			foreach (var type in updatedDataTypes)
			{
				_history.UpdatedData.Add(type);
			}
		}

		public async Task Sync()
		{
			if (Interlocked.CompareExchange(ref _syncInProgress, 1, 0) != 0)
			{
				_logger.Log("Sync already in progress, skipping concurrent call.");
				return;
			}

			try
			{
				await SyncInternal();
			}
			finally
			{
				Interlocked.Exchange(ref _syncInProgress, 0);
			}
		}

		private async Task SyncInternal()
		{
			if (Mode == SyncMode.Offline)
			{
				_logger.Log($"Sync called in Offline mode, but commands executed in Offline mode are not saved to the history, so there is nothing to sync.");
				return;
			}

			if (_history.Queue.IsEmpty)
			{
				_logger.Log($"Sync called but there are no commands in the history to sync.");
				return;
			}

			if (_syncService == null)
			{
				_logger.LogError($"Sync service is not configured, cannot sync command history.");
				return;
			}

			var commands = _history.ToArray();
			var clientStateHash = _dataManager.GetStateHash(_history.UpdatedData);

			var request = new SyncRequest
			{
				Commands = commands,
				ClientStateHash = clientStateHash,
			};
			var response = await _syncService.SyncAsync(request);

			// If the sync is successful, clear the history so that the same commands won't be processed again.
			if (response.Success)
			{
				_logger.Log($"Sync successful, clearing command history.");
				ClearHistory();
			}
			else
			{
				_logger.LogWarning($"Sync failed with message: {response.Message}. Command history will be retained for later retry.");
			}
		}

		public void ClearHistory()
		{
			// Clear the call queue
			_history.Clear();

			// Save the updated call queue to storage so that it can be processed later
			SaveHistory(_history);
		}

		private CommandHistory LoadHistory()
		{
			if (!_commandsDataStorage.TryLoad(CommandHistoryPrefKey, typeof(CommandHistory), out var historyObj) || historyObj == null)
			{
				return new CommandHistory();
			}
			else
			{
				return (CommandHistory)historyObj;
			}
		}

		private void SaveHistory(CommandHistory history)
		{
			_commandsDataStorage.Save(CommandHistoryPrefKey, history, typeof(CommandHistory));
		}

		private struct CommandDef
		{
			public string Name { get; set; }
			public Type CommandType { get; set; }
			public Type ArgsType { get; set; }
			public MethodInfo ExecuteMethod { get; set; }

			private readonly Action<Context, object> _cachedInvoke;

			public CommandDef(string name, Type commandType, Type argsType, MethodInfo executeMethod)
			{
				Name = name;
				CommandType = commandType;
				ArgsType = argsType;
				ExecuteMethod = executeMethod;

				var contextParam = Expression.Parameter(typeof(Context), "context");
				var argsParam = Expression.Parameter(typeof(object), "args");

				if (argsType == null)
				{
					// Wrapper for: (ctx, _) => TargetMethod(ctx)
					var call = Expression.Call(executeMethod, contextParam);
					_cachedInvoke = Expression.Lambda<Action<Context, object>>(call, contextParam, argsParam).Compile();
				}
				else
				{
					// Wrapper for: (ctx, obj) => TargetMethod(ctx, (TArgs)obj)
					var castArgs = Expression.Convert(argsParam, argsType);
					var call = Expression.Call(executeMethod, contextParam, castArgs);
					_cachedInvoke = Expression.Lambda<Action<Context, object>>(call, contextParam, argsParam).Compile();
				}
			}

			public readonly void Execute(Context context, object args = null)
			{
				if (args == null && ArgsType != null)
				{
					throw new CommandArgumentException($"Command '{Name}' requires an argument of type {ArgsType}, but no arguments were provided.");
				}

				if (args != null && ArgsType == null)
				{
					throw new CommandArgumentException($"Command '{Name}' does not accept any arguments, but arguments were provided.");
				}

				if (args != null && ArgsType != null && !ArgsType.IsInstanceOfType(args))
				{
					throw new CommandArgumentException($"Command '{Name}' requires an argument of type {ArgsType}, but an argument of type {args.GetType()} was provided.");
				}

				try
				{
					_cachedInvoke(context, args);
				}
				catch (Exception e)
				{
					context.State.Fail(message: $"An error occurred while executing command '{Name}': {e.Message}");
				}
			}
		}

		private struct EventHandlerDef
		{
			public string Name { get; }
			public Type EventType { get; }

			private readonly Action<Context, object> _cachedInvoke;

			public EventHandlerDef(string name, Type eventType, MethodInfo executeMethod)
			{
				Name = name;
				EventType = eventType;

				// Compile an expression tree for fast invocation: (ctx, obj) => Handler(ctx, (TEvent)obj)
				var contextParam = Expression.Parameter(typeof(Context), "context");
				var payloadParam = Expression.Parameter(typeof(object), "payload");
				var castPayload = Expression.Convert(payloadParam, eventType);
				var call = Expression.Call(executeMethod, contextParam, castPayload);
				_cachedInvoke = Expression.Lambda<Action<Context, object>>(call, contextParam, payloadParam).Compile();
			}

			public readonly void Execute(Context context, object payload)
			{
				try
				{
					_cachedInvoke(context, payload);
				}
				catch (Exception e)
				{
					context.State.Fail(message: $"An error occurred in event handler '{Name}': {e.Message}");
				}
			}
		}

		[Serializable]
		public record CommandCall
		{
			public Guid Id;
			public long Timestamp;
			public string ConfigVersion;
			public string CommandName;
			public object Args;

			public override string ToString()
			{
				var dt = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp);
				string argsString = Args != null ? Args.ToString() : "null";
				return $"[{dt:yyyy-MM-dd HH:mm:ss}] {CommandName} ({argsString}) - Id: {Id}";
			}
		}

		[Serializable]
		public class CommandHistory
		{
			public ConcurrentQueue<CommandCall> Queue = new();
			public HashSet<Type> UpdatedData = new();

			public void Enqueue(CommandCall call) => Queue.Enqueue(call);

			public CommandCall[] ToArray() => Queue.ToArray();

			public void Clear()
			{
				Queue.Clear();
				UpdatedData.Clear();
			}
		}

		[Serializable]
		public enum SyncMode
		{
			Online, // Commands are executed immediately and saved to the history for later processing (e.g. syncing with server)
			Offline, // Commands are executed immediately but not saved to the history, so they won't be processed later (e.g. for syncing with server)
		}
	}
}
