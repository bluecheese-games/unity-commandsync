//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.LocalCommands.Core;
using NUnit.Framework;
using System;

namespace BlueCheese.LocalCommands.Tests
{
	[TestFixture]
	public class HashUtilityTests
	{
		[Test]
		public void GetDeterministicHashCode_SameString_ReturnsSameHash()
		{
			string input = "{\"Score\":10,\"Name\":\"Player1\"}";
			int hash1 = HashUtility.GetDeterministicHashCode(input);
			int hash2 = HashUtility.GetDeterministicHashCode(input);

			Assert.AreEqual(hash1, hash2, "Identical strings must produce the exact same hash.");
		}

		[Test]
		public void GetDeterministicHashCode_DifferentStrings_ReturnsDifferentHash()
		{
			int hash1 = HashUtility.GetDeterministicHashCode("A");
			int hash2 = HashUtility.GetDeterministicHashCode("B");

			Assert.AreNotEqual(hash1, hash2, "Different strings must produce different hashes.");
		}

		[Test]
		public void FloatDeterminism_SerializedFloats_ProduceConsistentHash()
		{
			// This test ensures that microscopic differences in float memory 
			// don't break our state hash if the serializer handles them properly.
			var serializer = new NewtonsoftJsonSerializer();

			var data1 = new SampleData { TotalFloatValue = 100.5f };
			var data2 = new SampleData { TotalFloatValue = 100.5000000f }; // Same IEEE 754 value

			string json1 = serializer.Serialize(data1);
			string json2 = serializer.Serialize(data2);

			int hash1 = HashUtility.GetDeterministicHashCode(json1);
			int hash2 = HashUtility.GetDeterministicHashCode(json2);

			Assert.AreEqual(hash1, hash2, "Equivalent float values must serialize and hash deterministically.");
		}
	}

	public struct SampleData : IEquatable<SampleData>
	{
		public int TotalValue;

		public float TotalFloatValue;

		public double TotalDoubleValue;

		public readonly bool Equals(SampleData other) =>
			TotalValue == other.TotalValue &&
			TotalFloatValue == other.TotalFloatValue &&
			TotalDoubleValue == other.TotalDoubleValue;
	}
}