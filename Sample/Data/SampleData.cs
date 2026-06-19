//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.CommandSync.Sample.Data
{
	[Serializable]
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

	[Serializable]
	public struct SampleData2 : IEquatable<SampleData2>
	{
		public string Text;

		public readonly bool Equals(SampleData2 other) => Text == other.Text;
	}
}
