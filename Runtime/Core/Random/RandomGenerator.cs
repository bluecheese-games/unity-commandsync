//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.LocalCommands.Core
{
	public class RandomGenerator : IRandomGenerator
	{
		private Random _random = new();

		public double Value => _random.NextDouble();

		public void Init(int seed) => _random = new Random(seed);

		public double Next(double min, double max) => _random.NextDouble() * (max - min) + min;

		public int Next(int min, int max) => _random.Next(min, max);
	}
}
