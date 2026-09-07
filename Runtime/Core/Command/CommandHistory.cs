using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BlueCheese.CommandSync.Core
{
	[Serializable]
	public class CommandHistory
	{
		public ConcurrentQueue<CommandCall> Queue = new();
		public HashSet<Type> UpdatedData = new();

		public void Enqueue(CommandCall call) => Queue.Enqueue(call);

		public CommandCall[] ToArray() => Queue.ToArray();

		public void Clear()
		{
			Queue.Clear();
			UpdatedData.Clear();
		}
	}
}
