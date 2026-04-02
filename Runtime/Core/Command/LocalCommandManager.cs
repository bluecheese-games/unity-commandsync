//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace BlueCheese.LocalCommands.Core
{

	public class LocalCommandManager
	{
		private readonly IDataManager _dataManager;
		private readonly ILogger _logger;
		private readonly IConfig _config;
		private readonly ITimeProvider _timeProvider;
		private readonly ICommandSyncService _syncService;

		private readonly Dictionary<string, CommandDef> _definitions = new(); // Registered command definitions, keyed by command name
		private readonly CommandHistory _history = new(); // History of executed commands that have updated the user data, to be processed later

		public SyncMode Mode { get; set; } = SyncMode.Online;

		public IReadOnlyList<CommandCall> History => _history.ToArray();

		public IEnumerable<Type> UpdatedDataTypes => _history.UpdatedData;

		public LocalCommandManager(IDataManager dataManager, ILogger logger, IConfig config, ITimeProvider timeProvider, ICommandSyncService syncService = null)
		{
			_dataManager = dataManager;
			_logger = logger ?? new NullLogger();
			_config = config ?? new Config();
			_timeProvider = timeProvider ?? new TimeProvider();
			_syncService = syncService;
			_history = _dataManager.Get(new CommandHistory());
		}

		public void RegisterCommands(Assembly assembly)
		{
			foreach (var method in GetLocalCommandMethods(assembly))
			{
				RegisterCommand(method);
			}
		}

		private IEnumerable<MethodInfo> GetLocalCommandMethods(Assembly assembly)
		{
			// Find all static methods that have the LocalCommandAttribute
			foreach (var type in assembly.GetTypes())
			{
				foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
				{
					if (method.GetCustomAttribute<LocalCommandAttribute>() != null)
					{
						yield return method;
					}
				}
			}
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

		public void ExecuteCommand(string commandName, object args) => ExecuteCommandImpl(Guid.NewGuid(), commandName, args);

		public void ExecuteCommand(string commandName) => ExecuteCommandImpl(Guid.NewGuid(), commandName, null);

		public void ExecuteCommand<T>(Action<Context, T> command, T args) => ExecuteCommandImpl(Guid.NewGuid(), command.Method.Name, args);

		public void ExecuteCommand(Action<Context> command) => ExecuteCommandImpl(Guid.NewGuid(), command.Method.Name, null);

		public void ReplayCommand(CommandCall call) => ExecuteCommandImpl(call.Id, call.CommandName, call.Args);

		public void ExecuteCommandImpl(Guid commandId, string commandName, object args)
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

			// Create the context for this command call
			var state = new ExecutionState();
			var data = new Data(_dataManager, state);
			var rng = new RandomGenerator();
			rng.Init(commandId.GetHashCode()); // Initialize RNG with a seed derived from the command ID to ensure deterministic random values for the same command call
			var context = new Context(_config, data, _logger, state, _timeProvider, rng);

			// Execute the command
			commandDef.Execute(context, args);

			if (state.Result == CommandExecutionResult.Failure)
			{
				_logger.LogWarning($"Command '{commandName}' execution failed: {state.FailureMessage}");
				return;
			}

			if (state.DataHasBeenUpdated)
			{
				// Flush the data storage to ensure that the updated data is saved before the next command call is executed.
				var updatedDataTypes = _dataManager.Flush();

				// If the user data was updated, we need to save this command call so that it can be processed later.
				SaveToHistory(commandId, commandName, args, updatedDataTypes);

				_logger.Log($"Command '{commandName}' executed and data was updated. Call has been enqueued.");
			}
		}
		private void SaveToHistory(Guid commandId, string commandName, object args, IEnumerable<Type> updatedDataTypes)
		{
			if (Mode == SyncMode.Offline)
			{
				_logger.Log($"Command '{commandName}' executed in Offline mode, so it will not be saved to the history for later processing.");
				return;
			}

			// Enqueue the command call with its arguments and a timestamp
			_history.Enqueue(new CommandCall
			{
				Id = commandId,
				CommandName = commandName,
				Args = args,
				Timestamp = _timeProvider.UtcNow.ToUnixTimeMilliseconds()
			});

			// Track the types of data that were updated by this command, so that when processing the history later, we know which data types need to be synced with the server.
			_history.UpdatedData.UnionWith(updatedDataTypes);

			// Save the updated call queue to storage so that it can be processed later
			_dataManager.Set(_history);
		}

		public async Task Sync()
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
			var dataTypes = _history.UpdatedData.Except(new[] { typeof(CommandHistory) }).ToArray();
			var clientStateHash = _dataManager.GetStateHash(dataTypes);

			var request = new SyncRequest
			{
				Commands = commands,
				ClientStateHash = clientStateHash,
			};
			var response = await _syncService.SyncCommandsAsync(request);

			// If the sync is successful, clear the history so that the same commands won't be processed again.
			if (response.Result == SyncResult.Success)
			{
				_logger.Log($"Sync successful, clearing command history.");
				ClearHistory();
			}
			else
			{
				_logger.LogWarning($"Sync failed with result {response.Result}, message: {response.Message}. Command history will be retained for later retry.");
			}
		}

		public void ClearHistory()
		{
			// Clear the call queue
			_history.Clear();

			// Save the updated call queue to storage so that it can be processed later
			_dataManager.Set(_history);

			// Flush the data storage to ensure that the cleared history is saved before the next command call is executed.
			_dataManager.Flush();
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

		[Serializable]
		public record CommandCall
		{
			public Guid Id;
			public long Timestamp;
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

			public void Clear() => Queue.Clear();
		}

		[Serializable]
		public enum SyncMode
		{
			Online, // Commands are executed immediately and saved to the history for later processing (e.g. syncing with server)
			Offline, // Commands are executed immediately but not saved to the history, so they won't be processed later (e.g. for syncing with server)
		}
	}
}
