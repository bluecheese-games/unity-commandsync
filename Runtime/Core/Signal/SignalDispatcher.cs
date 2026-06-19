//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.CommandSync.Core
{
	// Routes signals synchronously to their registered handlers, sharing the triggering command's
	// data and state, with deterministic per-handler RNG seeds and cascade-depth protection.
	internal sealed class SignalDispatcher
	{
		private readonly CommandRegistry _registry;
		private readonly ILogger _logger;
		private readonly IConfig _config;
		private readonly ITimeProvider _timeProvider;

		public SignalDispatcher(CommandRegistry registry, ILogger logger, IConfig config, ITimeProvider timeProvider)
		{
			_registry = registry;
			_logger = logger;
			_config = config;
			_timeProvider = timeProvider;
		}

		// Builds the SignalContext handed to the root command; sending a signal re-enters Dispatch.
		public SignalContext CreateRootContext(
			Guid rootCommandId,
			DataAccessor data,
			ExecutionState state,
			Dictionary<Type, int> occurrenceCounters,
			EventContext eventContext)
		{
			return new SignalContext((signalType, payload) =>
				Dispatch(rootCommandId, data, state, occurrenceCounters, eventContext, signalType, payload, depth: 0));
		}

		private void Dispatch(
			Guid rootCommandId,
			DataAccessor data,
			ExecutionState state,
			Dictionary<Type, int> occurrenceCounters,
			EventContext eventContext,
			Type signalType,
			object payload,
			int depth)
		{
			if (depth > SignalContext.MaxCascadeDepth)
			{
				_logger.LogError(
					$"Signal cascade depth exceeded the maximum of {SignalContext.MaxCascadeDepth}. " +
					"Check for circular signal handler chains.");
				return;
			}

			if (!_registry.TryGetSignalHandlers(signalType, out var handlers))
			{
				return; // No handlers registered for this signal type
			}

			occurrenceCounters.TryGetValue(signalType, out int occurrenceIndex);
			occurrenceCounters[signalType] = occurrenceIndex + 1;

			foreach (var handler in handlers)
			{
				// Derive a deterministic seed from the root command ID, signal type and occurrence index
				// so that the handler's RNG seed is stable across replays and runtimes.
				var handlerSeed = DeriveHandlerSeed(rootCommandId, signalType, occurrenceIndex);

				var handlerRng = new RandomGenerator();
				handlerRng.Init(handlerSeed);

				// Each handler level gets its own SignalContext so the depth counter advances correctly
				var handlerSignalContext = new SignalContext((nestedSignalType, nestedPayload) =>
					Dispatch(rootCommandId, data, state, occurrenceCounters, eventContext, nestedSignalType, nestedPayload, depth + 1));

				// Handlers share the same state and data as the triggering command
				var handlerContext = new CommandContext(
					_config, data, _logger,
					state, _timeProvider, handlerRng, handlerSignalContext, eventContext);

				handler.Execute(handlerContext, payload);

				if (state.Result == CommandExecutionResult.Failure)
				{
					_logger.LogWarning($"Signal handler '{handler.Name}' failed: {state.FailureMessage}");
					if (state.FailureException != null)
					{
						_logger.LogException(state.FailureException);
					}
					return;
				}
			}
		}

		private static int DeriveHandlerSeed(Guid rootCommandId, Type signalType, int occurrenceIndex)
		{
			// Combine the root command ID, signal type full name and occurrence index into a
			// deterministic seed so replaying the same command always reseeds each handler identically.
			return HashUtility.GetDeterministicHashCode(
				$"{rootCommandId}:{signalType.FullName}:{occurrenceIndex}");
		}
	}
}
