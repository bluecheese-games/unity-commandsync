//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.LocalCommands.Core
{
	// Orchestrates a single command execution: argument conversion, context creation, signal dispatch,
	// failure rollback, external event dispatch, data flush and history enqueuing.
	internal sealed class CommandExecutor
	{
		private readonly CommandRegistry _registry;
		private readonly SignalDispatcher _signalDispatcher;
		private readonly ExternalEventBus _eventBus;
		private readonly CommandHistoryStore _historyStore;
		private readonly IInternalDataManager _dataManager;
		private readonly ILogger _logger;
		private readonly IConfig _config;
		private readonly ITimeProvider _timeProvider;
		private readonly ISerializer _serializer;

		public CommandExecutor(
			CommandRegistry registry,
			SignalDispatcher signalDispatcher,
			ExternalEventBus eventBus,
			CommandHistoryStore historyStore,
			IInternalDataManager dataManager,
			ILogger logger,
			IConfig config,
			ITimeProvider timeProvider,
			ISerializer serializer)
		{
			_registry = registry;
			_signalDispatcher = signalDispatcher;
			_eventBus = eventBus;
			_historyStore = historyStore;
			_dataManager = dataManager;
			_logger = logger;
			_config = config;
			_timeProvider = timeProvider;
			_serializer = serializer;
		}

		public void Execute(Guid commandId, string commandName, object args, bool saveCommand = true)
		{
			// Look up the command definition
			if (!_registry.TryGetCommand(commandName, out var commandDef))
			{
				throw new CommandNotFoundException(commandName);
			}

			// Argument validation is centralized in CommandDef.Execute (single source of truth).

			// When replaying from persisted history, args may come back as a serializer intermediate
			// (e.g. a JObject) instead of the original struct. Let the serializer reinterpret it,
			// without coupling this layer to any concrete serialization library.
			if (commandDef.ArgsType != null && args != null && !commandDef.ArgsType.IsInstanceOfType(args))
			{
				_serializer.TryConvert(args, commandDef.ArgsType, out args);
			}

			// Create the context for this command call
			var state = new ExecutionState();
			var data = new DataAccessor(_dataManager, state);
			var rng = new RandomGenerator();
			rng.Init(HashUtility.GetDeterministicHashCode(commandId.ToString())); // Deterministic, runtime-independent seed derived from the command ID
			var occurrenceCounters = new Dictionary<Type, int>();
			var eventContext = new EventContext();
			var signalContext = _signalDispatcher.CreateRootContext(commandId, data, state, occurrenceCounters, eventContext);
			var context = new CommandContext(_config, data, _logger, state, _timeProvider, rng, signalContext, eventContext);

			// Execute the command
			commandDef.Execute(context, args);

			if (state.Result == CommandExecutionResult.Failure)
			{
				// Roll back any in-memory mutations so a failed command leaves no partial state behind.
				_dataManager.RevertChanges();
				_logger.LogWarning($"Command '{commandName}' execution failed: {state.FailureMessage}");
				if (state.FailureException != null)
				{
					_logger.LogException(state.FailureException);
				}
				return;
			}

			// Dispatch any external events raised during command execution to registered subscribers
			_eventBus.Dispatch(eventContext);

			if (state.DataHasBeenUpdated)
			{
				// Flush the data storage to ensure that the updated data is saved before the next command call is executed.
				var updatedDataTypes = _dataManager.Flush();
				_historyStore.AddUpdatedTypes(updatedDataTypes);

				// If the user data was updated, we need to save this command call so that it can be processed later.
				if (saveCommand)
				{
					EnqueueCommandCall(commandId, commandName, args);
					_logger.Log($"Command '{commandName}' executed and data was updated. Call has been enqueued.");
				}

				_historyStore.Save();
			}
		}

		private void EnqueueCommandCall(Guid commandId, string commandName, object args)
		{
			_historyStore.Enqueue(new CommandCall
			{
				Id = commandId,
				CommandName = commandName,
				Args = args,
				Timestamp = _timeProvider.UtcNow.ToUnixTimeMilliseconds(),
				ConfigVersion = _config.Version,
			});
		}
	}
}
