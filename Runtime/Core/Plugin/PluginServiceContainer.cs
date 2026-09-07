using System;
using System.Collections.Generic;

namespace BlueCheese.CommandSync.Core
{
	// Backing store for plugin-registered services. CommandManager keeps one instance for its lifetime,
	// exposing it as IPluginServiceRegistry during plugin installation and as the narrower IPluginServices
	// to command authors via CommandContext.GetService.
	internal sealed class PluginServiceContainer : IPluginServiceRegistry
	{
		private readonly Dictionary<Type, object> _services = new();

		public void Add<T>(T instance) where T : class
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}
			if (_services.ContainsKey(typeof(T)))
			{
				throw new PluginRegistrationException($"A service of type '{typeof(T)}' is already registered.");
			}
			_services[typeof(T)] = instance;
		}

		public T Get<T>() where T : class
		{
			if (TryGet<T>(out var service))
			{
				return service;
			}
			throw new PluginServiceNotFoundException(typeof(T));
		}

		public bool TryGet<T>(out T service) where T : class
		{
			if (_services.TryGetValue(typeof(T), out var obj))
			{
				service = (T)obj;
				return true;
			}
			service = null;
			return false;
		}
	}
}
