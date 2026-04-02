//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.LocalCommands.Core
{
	public static class HashUtility
	{
		/// <summary>
		/// Generates a deterministic hash code for a given string.
		/// This is useful for scenarios where you need a consistent hash code across different runs of the application, such as for caching or identifying commands.
		/// </summary>
		public static int GetDeterministicHashCode(string str)
		{
			if (string.IsNullOrEmpty(str)) return 0;

			unchecked
			{
				int hash = 23;
				foreach (char c in str)
				{
					hash = hash * 31 + c;
				}
				return hash;
			}
		}
	}
}