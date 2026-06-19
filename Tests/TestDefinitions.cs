//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using BlueCheese.CommandSync.Core;

namespace BlueCheese.CommandSync.Tests
{
	[Serializable]
	public struct TestScoreData : IEquatable<TestScoreData>
	{
		public int Score;

		public readonly bool Equals(TestScoreData other) => Score == other.Score;
	}

	public struct ValueTypeArgs { public int Id; }

	public struct TestArgs { public int Value; }

	public struct ReadbackData { public int Value; }

	public static class TestCommands
	{
		[Command]
		public static void AddScore(CommandContext context, TestArgs args)
		{
			context.Data.Update((ref TestScoreData data) => data.Score += args.Value);
		}

		[Command("SecretName")]
		public static void CustomNamedMethod(CommandContext context)
		{
			context.Data.Update((ref TestScoreData data) => data.Score = 99);
		}

		[Command]
		public static void NoOpCommand(CommandContext context) { }

		[Command]
		public static void ValueTypeCommand(CommandContext context, ValueTypeArgs args) { }

		[Command]
		public static void NestedUpdateCommand(CommandContext context)
		{
			context.Data.Update((ref TestScoreData data) =>
			{
				data.Score += 10;
				context.Data.Update((ref TestScoreData inner) => inner.Score += 20);

			});
		}

		[Command]
		public static void ThrowingCommand(CommandContext context)
		{
			throw new Exception("Intentional crash for testing");
		}

		[Command]
		public static void MutateThenFail(CommandContext context)
		{
			context.Data.Update((ref TestScoreData data) => data.Score = 50);
			context.State.Fail("Simulated failure after a data mutation.");
		}

		[Command]
		public static void ReadAfterWrite(CommandContext context)
		{
			context.Data.Update((ref TestScoreData data) => data.Score = 5);
			int readBack = context.Data.Get<TestScoreData>().Score;
			context.Data.Update((ref ReadbackData data) => data.Value = readBack);
		}
	}
}
