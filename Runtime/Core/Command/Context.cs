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

		internal Context(IConfig config, Data data, ILogger logger, ExecutionState state, ITimeProvider timeProvider, IRandomGenerator rng)
		{
			Config = config;
			Data = data;
			Logger = logger;
			State = state;
			RNG = rng;
			Time = timeProvider;
		}
	}
}
