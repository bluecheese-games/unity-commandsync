//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Concurrent;

namespace BlueCheese.LocalCommands.Core
{
	/// <summary>
	/// Resolves a <see cref="Type"/> from its full name by scanning loaded assemblies, with caching.
	/// Shared by the serializer's name binder and by state import to avoid repeated O(assemblies) scans.
	/// </summary>
	public static class TypeResolver
	{
		private static readonly ConcurrentDictionary<string, Type> _cache = new();

		public static Type Resolve(string typeFullName)
		{
			if (string.IsNullOrEmpty(typeFullName))
			{
				return null;
			}

			if (_cache.TryGetValue(typeFullName, out var cached))
			{
				return cached;
			}

			var type = ScanLoadedAssemblies(typeFullName);
			if (type != null)
			{
				// Only successful resolutions are cached so a type can still be found after its assembly loads.
				_cache[typeFullName] = type;
			}
			return type;
		}

		private static Type ScanLoadedAssemblies(string typeFullName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var type = assembly.GetType(typeFullName);
				if (type != null)
				{
					return type;
				}
			}
			return null;
		}
	}
}
