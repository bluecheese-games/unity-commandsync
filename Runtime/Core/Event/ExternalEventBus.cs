//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.LocalCommands.Core
{
	// Holds external event subscribers and dispatches the events queued during a command execution.
	internal sealed class ExternalEventBus
	{
		private readonly Dictionary<Type, Dictionary<Delegate, Action<object>>> _subscribers = new();

		public IDisposable On<T>(Action<T> handler) where T : struct
		{
			var eventType = typeof(T);
			if (!_subscribers.TryGetValue(eventType, out var subscribers))
			{
				subscribers = new Dictionary<Delegate, Action<object>>();
				_subscribers[eventType] = subscribers;
			}
			subscribers[handler] = payload => handler((T)payload);
			return new Subscription(() => Off(handler));
		}

		public void Off<T>(Action<T> handler) where T : struct
		{
			if (_subscribers.TryGetValue(typeof(T), out var subscribers))
			{
				subscribers.Remove(handler);
			}
		}

		public void Dispatch(EventContext eventContext)
		{
			while (eventContext.TryDequeue(out var pendingEvent))
			{
				if (!_subscribers.TryGetValue(pendingEvent.EventType, out var subscribers))
				{
					continue; // No subscribers registered for this event type
				}

				foreach (var subscriber in subscribers.Values)
				{
					subscriber(pendingEvent.Payload);
				}
			}
		}

		// Handle returned by On<T> so a subscription (including a lambda) can be undone via Dispose().
		private sealed class Subscription : IDisposable
		{
			private Action _unsubscribe;

			public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;

			public void Dispose()
			{
				_unsubscribe?.Invoke();
				_unsubscribe = null;
			}
		}
	}
}
