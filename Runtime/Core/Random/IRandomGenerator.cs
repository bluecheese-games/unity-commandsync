//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.LocalCommands.Core
{
	public interface IRandomGenerator
	{
		void Init(int seed);
		double Next(double min, double max);
		int Next(int min, int max);
		double Value { get; }
	}
}
