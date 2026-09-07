namespace BlueCheese.CommandSync.Core
{
	/// <summary>
	/// Entry point for a CommandSync plugin. Implementations typically live in their own assembly
	/// (e.g. a "Leaderboard" package referencing only Core), register the services they expose to
	/// command authors in <see cref="Install"/>, and ship their own [Command]/[SignalHandler] methods,
	/// which <see cref="CommandManager.AddPlugin"/> discovers automatically from the plugin's assembly.
	/// </summary>
	public interface IPlugin
	{
		/// <summary>
		/// Unique name identifying the plugin. Used by <see cref="CommandManager.AddPlugin"/> to detect
		/// duplicate installation.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Called once by <see cref="CommandManager.AddPlugin"/>. Register any services the plugin exposes
		/// to command authors here, e.g. <c>context.Services.Add&lt;ILeaderboardService&gt;(new LeaderboardService(...))</c>.
		/// </summary>
		void Install(PluginInstallContext context);
	}
}
