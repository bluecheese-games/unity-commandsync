//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.LocalCommands.Core;
using BlueCheese.LocalCommands.Sample.Data;

namespace BlueCheese.LocalCommands.Sample.Commands
{
    public struct SampleCommandArgs
    {
        public int Value;
		public float FloatValue;

		override public readonly string ToString()
		{
			return $"Value={Value} FloatValue={FloatValue}";
		}
	}

	public struct TextCommandArgs
	{
		public string Text;

		override public readonly string ToString()
		{
			return $"Text=\"{Text}\"";
		}
	}

	public class SampleCommands
	{
		[LocalCommand]
		public static void AddValue(Context context, SampleCommandArgs args)
		{
			context.Data.Update((ref SampleData data) =>
			{
				data.TotalValue += args.Value;
			});
		}

		public const string RESET_COMMAND_NAME = "Reset";

		[LocalCommand(RESET_COMMAND_NAME)]
		public static void ResetValue(Context context)
		{
			context.Data.Update((ref SampleData data) =>
			{
				data.TotalValue = 0;
			});
		}

		[LocalCommand]
		public static void AddFloatValue(Context context, SampleCommandArgs args)
		{
			context.Data.Update((ref SampleData data) =>
			{
				data.TotalFloatValue += args.FloatValue;
			});
		}

		[LocalCommand]
		public static void AddRandomDoubleValue(Context context)
		{
			context.Data.Update((ref SampleData data) =>
			{
				double randomValue = context.RNG.Next(100d, 1_000_000d);
				context.Logger.Log($"Adding random double value: {randomValue}");
				data.TotalDoubleValue += randomValue;
			});
		}

		[LocalCommand]
		public static void LogValue(Context context)
		{
			var data = context.Data.Get<SampleData>();
			context.Logger.Log($"Current total value: {data.TotalValue} (at {context.Time.UtcNow.ToLocalTime()})");
		}

		[LocalCommand]
		public static void ResetAndAddValue(Context context, SampleCommandArgs args)
		{
			ResetValue(context);
			AddValue(context, args);
		}

		[LocalCommand]
		public static void AddValueMultipleTimes(Context context, SampleCommandArgs args)
		{
			for (int i = 0; i < context.Config.Get("mult", 5); i++)
			{
				AddValue(context, args);
			}
		}

		[LocalCommand]
		public static void FailingCommand(Context context)
		{
			// This command simulates a failure by setting the execution result to Failure.
			context.State.Fail("Simulated failure in local command.");
		}

		[LocalCommand]
		public static void FailingCommandWithException(Context context)
		{
			// This command simulates a failure by throwing an exception.
			throw new System.Exception("Simulated exception in local command.");
		}

		[LocalCommand]
		public static void NestedWriteValue(Context context)
		{
			context.Data.Update((ref SampleData data) =>
			{
				data.TotalValue += 10;
				context.Data.Update((ref SampleData nestedData) =>
				{
					nestedData.TotalValue += 20;
				});
			});
		}

		[LocalCommand]
		public static void SetText(Context context, TextCommandArgs args)
		{
			context.Data.Update((ref SampleData2 data) =>
			{
				data.Text = args.Text;
			});
			context.Logger.Log($"Text set to: {args.Text}");
		}
	}
}
