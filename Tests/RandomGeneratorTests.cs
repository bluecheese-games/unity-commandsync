//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using NUnit.Framework;
using BlueCheese.LocalCommands.Core;
using System;

namespace BlueCheese.LocalCommands.Tests
{
	[TestFixture]
	public class RandomGeneratorTests
	{
		[Test]
		public void Init_WithSameSeed_ProducesExactSameSequence()
		{
			int seed = Guid.NewGuid().GetHashCode();

			// Simulate Client execution
			var clientRng = new RandomGenerator();
			clientRng.Init(seed);
			double clientVal1 = clientRng.Next(0d, 100d);
			int clientVal2 = clientRng.Next(1, 1000);

			// Simulate Server replay
			var serverRng = new RandomGenerator();
			serverRng.Init(seed);
			double serverVal1 = serverRng.Next(0d, 100d);
			int serverVal2 = serverRng.Next(1, 1000);

			Assert.AreEqual(clientVal1, serverVal1, "Double RNG must be identical for the same seed.");
			Assert.AreEqual(clientVal2, serverVal2, "Int RNG must be identical for the same seed.");
		}

		[Test]
		public void Init_WithDifferentSeeds_ProducesDifferentSequences()
		{
			var rng1 = new RandomGenerator();
			rng1.Init(12345);

			var rng2 = new RandomGenerator();
			rng2.Init(98765);

			Assert.AreNotEqual(rng1.Value, rng2.Value, "Different seeds should produce different random values.");
		}
	}
}