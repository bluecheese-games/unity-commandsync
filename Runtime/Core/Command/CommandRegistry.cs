using System;
using System.Collections.Generic;
using System.Reflection;

namespace BlueCheese.CommandSync.Core
{
	// Discovers and stores command and signal handler definitions, and provides lookup for the executor.
	internal sealed class CommandRegistry
	{
		private readonly Dictionary<string, CommandDef> _definitions = new();
		private readonly Dictionary<Type, List<SignalHandlerDef>> _signalHandlers = new();

		public void RegisterFromAssembly(Assembly assembly)
		{
			// Find all static methods that have the CommandAttribute or the SignalHandlerAttribute
			foreach (var type in assembly.GetTypes())
			{
				foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
				{
					if (method.GetCustomAttribute<CommandAttribute>() != null)
					{
						RegisterCommand(method);
					}
					else if (method.GetCustomAttribute<SignalHandlerAttribute>() != null)
					{
						RegisterSignalHandler(method);
					}
				}
			}
		}

		public void RegisterCommand(MethodInfo method)
		{
			var attr = method.GetCustomAttribute<CommandAttribute>();
			var commandName = attr.Name ?? method.Name;
			if (_definitions.ContainsKey(commandName))
			{
				throw new CommandRegistrationException($"A command with the name '{commandName}' is already registered.");
			}

			var parameters = method.GetParameters();
			if (parameters[0].ParameterType != typeof(CommandContext))
			{
				throw new CommandRegistrationException($"Method '{method.Name}' must have a CommandContext parameter as the first argument.");
			}
			if (parameters.Length > 2)
			{
				throw new CommandRegistrationException($"Method '{method.Name}' has too many parameters. Only CommandContext and an optional args parameter are allowed.");
			}

			var argsType = parameters.Length == 2 ? parameters[1].ParameterType : null;
			_definitions[commandName] = new CommandDef(commandName, method.DeclaringType, argsType, method);
		}

		public void RegisterSignalHandler(MethodInfo method)
		{
			var parameters = method.GetParameters();
			if (parameters.Length != 2 || parameters[0].ParameterType != typeof(CommandContext))
			{
				throw new CommandRegistrationException(
					$"Signal handler '{method.Name}' must have exactly two parameters: CommandContext and the signal payload struct.");
			}

			var signalType = parameters[1].ParameterType;
			if (!signalType.IsValueType)
			{
				throw new CommandRegistrationException(
					$"Signal handler '{method.Name}': signal payload type '{signalType.Name}' must be a struct.");
			}

			var attr = method.GetCustomAttribute<SignalHandlerAttribute>();
			var handlerDef = new SignalHandlerDef(method.Name, signalType, attr.Priority, method);

			if (!_signalHandlers.TryGetValue(signalType, out var handlers))
			{
				handlers = new List<SignalHandlerDef>();
				_signalHandlers[signalType] = handlers;
			}
			handlers.Add(handlerDef);
			// Keep the list sorted descending so highest-priority handlers execute first
			handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
		}

		public bool TryGetCommand(string commandName, out CommandDef definition) =>
			_definitions.TryGetValue(commandName, out definition);

		public bool TryGetSignalHandlers(Type signalType, out List<SignalHandlerDef> handlers) =>
			_signalHandlers.TryGetValue(signalType, out handlers);
	}
}
