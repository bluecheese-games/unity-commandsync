namespace BlueCheese.CommandSync.Core
{
	/// <summary>
	/// Passed to <see cref="IPlugin.Install"/>. Gives the plugin write access to the shared service
	/// container (<see cref="Services"/>), read-only access to the data manager (<see cref="Data"/>) so a
	/// plugin's service can query committed state outside of command execution (e.g. for a UI), plus the
	/// manager's config and logger, so a plugin can validate its own configuration or log its installation
	/// without needing its own copies.
	/// </summary>
	public sealed class PluginInstallContext
	{
		public IPluginServiceRegistry Services { get; }
		public IReadOnlyDataManager Data { get; }
		public IConfig Config { get; }
		public ILogger Logger { get; }

		internal PluginInstallContext(IPluginServiceRegistry services, IReadOnlyDataManager data, IConfig config, ILogger logger)
		{
			Services = services;
			Data = data;
			Config = config;
			Logger = logger;
		}
	}
}
