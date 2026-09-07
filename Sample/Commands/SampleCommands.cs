using BlueCheese.CommandSync.Core;
using BlueCheese.CommandSync.Sample.Data;

namespace BlueCheese.CommandSync.Sample.Commands
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
		[Command]
		public static void AddValue(CommandContext context, SampleCommandArgs args)
		{
			context.Data.Update((ref SampleData data) =>
			{
				data.TotalValue += args.Value;
			});
		}

		public const string RESET_COMMAND_NAME = "Reset";

		[Command(RESET_COMMAND_NAME)]
		public static void ResetValue(CommandContext context)
		{
			context.Data.Update((ref SampleData data) =>
			{
				data.TotalValue = 0;
			});
		}

		[Command]
		public static void AddFloatValue(CommandContext context, SampleCommandArgs args)
		{
			context.Data.Update((ref SampleData data) =>
			{
				data.TotalFloatValue += args.FloatValue;
			});
		}

		[Command]
		public static void AddRandomDoubleValue(CommandContext context)
		{
			context.Data.Update((ref SampleData data) =>
			{
				double randomValue = context.RNG.Next(100d, 1_000_000d);
				context.Logger.Log($"Adding random double value: {randomValue}");
				data.TotalDoubleValue += randomValue;
			});
		}

		[Command]
		public static void LogValue(CommandContext context)
		{
			var data = context.Data.Get<SampleData>();
			context.Logger.Log($"Current total value: {data.TotalValue} (at {context.Time.UtcNow.ToLocalTime()})");
		}

		[Command]
		public static void ResetAndAddValue(CommandContext context, SampleCommandArgs args)
		{
			ResetValue(context);
			AddValue(context, args);
		}

		[Command]
		public static void AddValueMultipleTimes(CommandContext context, SampleCommandArgs args)
		{
			for (int i = 0; i < context.Config.Get("mult", 5); i++)
			{
				AddValue(context, args);
			}
		}

		[Command]
		public static void FailingCommand(CommandContext context)
		{
			// This command simulates a failure by setting the execution result to Failure.
			context.State.Fail("Simulated failure in local command.");
		}

		[Command]
		public static void FailingCommandWithException(CommandContext context)
		{
			// This command simulates a failure by throwing an exception.
			throw new System.Exception("Simulated exception in local command.");
		}

		[Command]
		public static void NestedWriteValue(CommandContext context)
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

		[Command]
		public static void SetText(CommandContext context, TextCommandArgs args)
		{
			context.Data.Update((ref SampleData2 data) =>
			{
				data.Text = args.Text;
			});
			context.Logger.Log($"Text set to: {args.Text}");
		}
	}
}
