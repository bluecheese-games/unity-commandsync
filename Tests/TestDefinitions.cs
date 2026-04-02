//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using BlueCheese.LocalCommands.Core;

namespace BlueCheese.LocalCommands.Tests
{
	[Serializable]
	public struct TestScoreData : IEquatable<TestScoreData>
	{
		public int Score;

		public readonly bool Equals(TestScoreData other) => Score == other.Score;
	}

	public struct ValueTypeArgs { public int Id; }

	public struct TestArgs { public int Value; }

	public static class TestCommands
	{
		[LocalCommand]
		public static void AddScore(Context context, TestArgs args)
		{
			context.Data.Update((ref TestScoreData data) => data.Score += args.Value);
		}

		[LocalCommand("SecretName")]
		public static void CustomNamedMethod(Context context)
		{
			context.Data.Update((ref TestScoreData data) => data.Score = 99);
		}

		[LocalCommand]
		public static void NoOpCommand(Context context) { }

		[LocalCommand]
		public static void ValueTypeCommand(Context context, ValueTypeArgs args) { }

		[LocalCommand]
		public static void NestedUpdateCommand(Context context)
		{
			context.Data.Update((ref TestScoreData data) =>
			{
				data.Score += 10;
				context.Data.Update((ref TestScoreData inner) => inner.Score += 20);

			});
		}

		[LocalCommand]
		public static void ThrowingCommand(Context context)
		{
			throw new Exception("Intentional crash for testing");
		}
	}
}