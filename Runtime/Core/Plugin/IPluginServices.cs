namespace BlueCheese.CommandSync.Core
{
	/// <summary>
	/// Read-only access to services registered by plugins during installation.
	/// Exposed to command authors via <see cref="CommandContext.GetService{T}"/>.
	/// Plugins typically wrap this behind a named extension method on <see cref="CommandContext"/>
	/// so the service shows up in auto-completion, e.g. `ctx.Leaderboard()` instead of
	/// `ctx.GetService&lt;ILeaderboardService&gt;()`.
	/// </summary>
	public interface IPluginServices
	{
		/// <summary>
		/// Returns the registered service of type T, or throws <see cref="PluginServiceNotFoundException"/> if none was registered.
		/// </summary>
		T Get<T>() where T : class;

		/// <summary>
		/// Attempts to return the registered service of type T.
		/// </summary>
		bool TryGet<T>(out T service) where T : class;
	}
}
