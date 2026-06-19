//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace BlueCheese.LocalCommands.Core
{
	// Compiled, cached descriptor of a registered signal handler method.
	internal struct SignalHandlerDef
	{
		public string Name { get; }
		public Type SignalType { get; }
		public int Priority { get; }

		private readonly Action<CommandContext, object> _cachedInvoke;

		public SignalHandlerDef(string name, Type signalType, int priority, MethodInfo executeMethod)
		{
			Name = name;
			SignalType = signalType;
			Priority = priority;

			// Compile an expression tree for fast invocation: (ctx, obj) => Handler(ctx, (TSignal)obj)
			var contextParam = Expression.Parameter(typeof(CommandContext), "context");
			var payloadParam = Expression.Parameter(typeof(object), "payload");
			var castPayload = Expression.Convert(payloadParam, signalType);
			var call = Expression.Call(executeMethod, contextParam, castPayload);
			_cachedInvoke = Expression.Lambda<Action<CommandContext, object>>(call, contextParam, payloadParam).Compile();
		}

		public readonly void Execute(CommandContext context, object payload)
		{
			try
			{
				_cachedInvoke(context, payload);
			}
			catch (Exception e)
			{
				context.State.Fail($"An error occurred in signal handler '{Name}': {e.Message}", e);
			}
		}
	}
}
