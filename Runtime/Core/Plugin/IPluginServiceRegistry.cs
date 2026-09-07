namespace BlueCheese.CommandSync.Core
{
	/// <summary>
	/// Write access to the plugin service container. Only available during <see cref="IPlugin.Install"/>,
	/// via <see cref="PluginInstallContext.Services"/>, so services can only be registered at install time.
	/// </summary>
	public interface IPluginServiceRegistry : IPluginServices
	{
		/// <summary>
		/// Registers a service instance under type T. Throws <see cref="PluginRegistrationException"/>
		/// if a service of that type is already registered.
		/// </summary>
		void Add<T>(T instance) where T : class;
	}
}
