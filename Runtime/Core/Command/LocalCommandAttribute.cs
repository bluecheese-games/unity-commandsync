//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public class LocalCommandAttribute : Attribute
	{
		public string Name { get; private set; }

		public LocalCommandAttribute()
		{
			Name = null; // Default to null, which means the command name will be derived from the class name
		}

		public LocalCommandAttribute(string name)
		{
			Name = name;
		}
	}
}
