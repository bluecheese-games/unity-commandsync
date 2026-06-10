//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.LocalCommands.Core
{
	public struct Context
	{
		public IConfig Config { get; private set; }
		public Data Data { get; private set; }
		public ILogger Logger { get; private set; }
		public ITimeProvider Time { get; private set; }
		public IRandomGenerator RNG { get; private set; }
		public ExecutionState State { get; private set; }

		/// <summary>
		/// Allows raising local events from within a command or event handler.
		/// Events are dispatched after the current execution step completes.
		/// </summary>
		public IEventContext Events { get; private set; }

		internal Context(IConfig config, Data data, ILogger logger, ExecutionState state, ITimeProvider timeProvider, IRandomGenerator rng, IEventContext events)
		{
			Config = config;
			Data = data;
			Logger = logger;
			State = state;
			RNG = rng;
			Time = timeProvider;
			Events = events;
		}
	}
}
