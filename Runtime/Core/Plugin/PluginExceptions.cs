using System;

namespace BlueCheese.CommandSync.Core
{
	public class PluginException : Exception
	{
		public PluginException(string message) : base(message) { }
	}

	public class PluginRegistrationException : PluginException
	{
		public PluginRegistrationException(string message) : base(message) { }
	}

	public class PluginServiceNotFoundException : PluginException
	{
		public PluginServiceNotFoundException(Type serviceType)
			: base($"No plugin service of type '{serviceType}' is registered. " +
				  "Make sure the owning plugin has been installed via CommandManager.AddPlugin before this command runs.")
		{ }
	}
}
