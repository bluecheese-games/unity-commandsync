namespace BlueCheese.CommandSync.Core
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

		/// <summary>
		/// 64-bit deterministic hash (FNV-1a) for a given string.
		/// Wider than <see cref="GetDeterministicHashCode"/> to reduce collisions when hashing
		/// the full data state for desync detection.
		/// </summary>
		public static long GetDeterministicHashCode64(string str)
		{
			if (string.IsNullOrEmpty(str)) return 0;

			unchecked
			{
				const ulong offsetBasis = 14695981039346656037UL;
				const ulong prime = 1099511628211UL;
				ulong hash = offsetBasis;
				foreach (char c in str)
				{
					hash ^= c;
					hash *= prime;
				}
				return (long)hash;
			}
		}
	}
}
