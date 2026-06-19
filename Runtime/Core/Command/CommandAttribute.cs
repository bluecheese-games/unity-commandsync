//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.CommandSync.Core
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public class CommandAttribute : Attribute
	{
		public string Name { get; private set; }

		public CommandAttribute()
		{
			Name = null; // Default to null, which means the command name will be derived from the class name
		}

		public CommandAttribute(string name)
		{
			Name = name;
		}
	}
}
