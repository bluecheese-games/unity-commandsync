namespace BlueCheese.CommandSync.Core
{
	public struct CommandContext
	{
		public IConfig Config { get; private set; }
		public DataAccessor Data { get; private set; }
		public ILogger Logger { get; private set; }
		public ITimeProvider Time { get; private set; }
		public IRandomGenerator RNG { get; private set; }
		public ExecutionState State { get; private set; }

		/// <summary>
		/// Sends a signal synchronously to other commands within the CommandSync module.
		/// The signal handler executes immediately before this call returns.
		/// </summary>
		public ISignalContext Signals { get; private set; }

		/// <summary>
		/// Queues an external event to be dispatched to subscribers outside the module
		/// after the current command and all its signal handlers have finished.
		/// </summary>
		public IEventContext Events { get; private set; }

		private IPluginServices _pluginServices;

		internal CommandContext(IConfig config, DataAccessor data, ILogger logger, ExecutionState state, ITimeProvider timeProvider, IRandomGenerator rng, ISignalContext signals, IEventContext events, IPluginServices pluginServices)
		{
			Config = config;
			Data = data;
			Logger = logger;
			State = state;
			RNG = rng;
			Time = timeProvider;
			Signals = signals;
			Events = events;
			_pluginServices = pluginServices;
		}

		/// <summary>
		/// Returns a service registered by a plugin via <c>PluginInstallContext.Services.Add</c>.
		/// Plugins typically wrap this in a named extension method on <see cref="CommandContext"/> for
		/// discoverability, e.g. `ctx.Leaderboard()` instead of `ctx.GetService&lt;ILeaderboardService&gt;()`.
		/// </summary>
		public T GetService<T>() where T : class => _pluginServices.Get<T>();

		/// <summary>
		/// Attempts to return a service registered by a plugin, without throwing if none was registered.
		/// </summary>
		public bool TryGetService<T>(out T service) where T : class => _pluginServices.TryGet(out service);
	}
}
