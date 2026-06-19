//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace BlueCheese.LocalCommands.Core
{
	// Compiled, cached descriptor of a registered command method.
	internal struct CommandDef
	{
		public string Name { get; set; }
		public Type CommandType { get; set; }
		public Type ArgsType { get; set; }
		public MethodInfo ExecuteMethod { get; set; }

		private readonly Action<CommandContext, object> _cachedInvoke;

		public CommandDef(string name, Type commandType, Type argsType, MethodInfo executeMethod)
		{
			Name = name;
			CommandType = commandType;
			ArgsType = argsType;
			ExecuteMethod = executeMethod;

			var contextParam = Expression.Parameter(typeof(CommandContext), "context");
			var argsParam = Expression.Parameter(typeof(object), "args");

			if (argsType == null)
			{
				// Wrapper for: (ctx, _) => TargetMethod(ctx)
				var call = Expression.Call(executeMethod, contextParam);
				_cachedInvoke = Expression.Lambda<Action<CommandContext, object>>(call, contextParam, argsParam).Compile();
			}
			else
			{
				// Wrapper for: (ctx, obj) => TargetMethod(ctx, (TArgs)obj)
				var castArgs = Expression.Convert(argsParam, argsType);
				var call = Expression.Call(executeMethod, contextParam, castArgs);
				_cachedInvoke = Expression.Lambda<Action<CommandContext, object>>(call, contextParam, argsParam).Compile();
			}
		}

		public readonly void Execute(CommandContext context, object args = null)
		{
			if (args == null && ArgsType != null)
			{
				throw new CommandArgumentException($"Command '{Name}' requires an argument of type {ArgsType}, but no arguments were provided.");
			}

			if (args != null && ArgsType == null)
			{
				throw new CommandArgumentException($"Command '{Name}' does not accept any arguments, but arguments were provided.");
			}

			if (args != null && ArgsType != null && !ArgsType.IsInstanceOfType(args))
			{
				throw new CommandArgumentException($"Command '{Name}' requires an argument of type {ArgsType}, but an argument of type {args.GetType()} was provided.");
			}

			try
			{
				_cachedInvoke(context, args);
			}
			catch (Exception e)
			{
				context.State.Fail($"An error occurred while executing command '{Name}': {e.Message}", e);
			}
		}
	}
}
