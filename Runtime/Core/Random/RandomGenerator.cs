//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.CommandSync.Core
{
	/// <summary>
	/// Deterministic pseudo-random generator based on the SplitMix64 algorithm.
	/// Unlike System.Random, the sequence produced for a given seed is identical across
	/// runtimes (Mono, IL2CPP, .NET Core/8+), which is required for client/server command replay.
	/// </summary>
	public class RandomGenerator : IRandomGenerator
	{
		private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

		private ulong _state;

		public RandomGenerator()
		{
			Init(0);
		}

		public void Init(int seed)
		{
			unchecked
			{
				_state = (ulong)seed * GoldenGamma + GoldenGamma;
			}
		}

		public double Value => NextDouble();

		public double Next(double min, double max) => NextDouble() * (max - min) + min;

		public int Next(int min, int max)
		{
			if (min >= max)
			{
				return min;
			}

			ulong range = (ulong)((long)max - min);
			return (int)((long)min + (long)(NextUInt64() % range));
		}

		private ulong NextUInt64()
		{
			unchecked
			{
				_state += GoldenGamma;
				ulong z = _state;
				z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
				z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
				return z ^ (z >> 31);
			}
		}

		// 53-bit mantissa mapped to [0, 1)
		private double NextDouble() => (NextUInt64() >> 11) * (1.0 / 9007199254740992.0);
	}
}
