using System;
using System.Reflection;
using BlueCheese.CommandSync.Core;
using NUnit.Framework;

namespace BlueCheese.CommandSync.Tests
{
	[TestFixture]
	public class SignalHandlerTests
	{
		private CommandManager _manager;
		private DataManager _dataManager;

		[SetUp]
		public void Setup()
		{
			var storage = new FakeDataStorage();
			_dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());
			_manager = new CommandManager(
				_dataManager, new FakeLogger(), Config.Create(),
				new SystemTimeProvider(), storage);
			_manager.RegisterCommands(Assembly.GetExecutingAssembly());
		}

		[Test]
		public void SignalHandler_IsCalledDuringCommandExecution()
		{
			_manager.ExecuteCommand(nameof(SignalTestCommands.GainXp), new SignalXpArgs { Amount = 50 });

			Assert.AreEqual(1, _dataManager.Get<SignalCounterData>().Count,
				"Signal handler must be called once when the command sends one signal.");
		}

		[Test]
		public void SignalHandler_ReceivesCorrectPayload()
		{
			_manager.ExecuteCommand(nameof(SignalTestCommands.GainXp), new SignalXpArgs { Amount = 75 });

			Assert.AreEqual(75, _dataManager.Get<SignalCounterData>().LastAmount,
				"Signal handler must receive the exact payload sent by the command.");
		}

		[Test]
		public void SignalHandler_CanChainSignals()
		{
			// GainXp(200) → XpGainedSignal → OnXpGained → LevelUpSignal → OnLevelUp
			_manager.ExecuteCommand(nameof(SignalTestCommands.GainXp), new SignalXpArgs { Amount = 200 });

			Assert.AreEqual(1, _dataManager.Get<SignalCounterData>().Count, "XpGained handler called once.");
			Assert.AreEqual(1, _dataManager.Get<SignalLevelData>().Level, "LevelUp handler called once.");
		}

		[Test]
		public void SignalHandler_CommandIsEnqueuedInHistory_EvenWhenHandlerModifiesData()
		{
			_manager.ExecuteCommand(nameof(SignalTestCommands.GainXp), new SignalXpArgs { Amount = 50 });

			// Only the root command should appear in history, not the handler
			Assert.AreEqual(1, _manager.History.Count,
				"Only the root command must be stored in history, not its signal handlers.");
			Assert.AreEqual(nameof(SignalTestCommands.GainXp), _manager.History[0].CommandName,
				"The history entry must reference the originating command.");
		}

		[Test]
		public void SignalHandler_IsDeterministic_SameRngOnReplay()
		{
			_manager.ExecuteCommand(nameof(SignalTestCommands.RollOnSignal));
			int firstRoll = _dataManager.Get<SignalRollData>().LastRoll;

			// Reset data and replay with the same command ID to verify deterministic RNG
			_dataManager.Set(new SignalRollData());
			_dataManager.Flush();
			_manager.ReplayCommand(_manager.History[0]);
			int replayRoll = _dataManager.Get<SignalRollData>().LastRoll;

			Assert.AreEqual(firstRoll, replayRoll,
				"Signal handler RNG must be deterministic: replay must produce the same values.");
		}

		[Test]
		public void SignalHandler_MaxCascadeDepth_IsRespected()
		{
			// InfiniteSignalLoop sends InfiniteLoopSignal whose handler sends the same signal.
			// The cascade must stop at SignalContext.MaxCascadeDepth without throwing or looping forever.
			var storage = new FakeDataStorage();
			var dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());
			var logger = new FakeLogger();
			var manager = new CommandManager(dataManager, logger, Config.Create(), new SystemTimeProvider(), storage);
			manager.RegisterCommands(Assembly.GetExecutingAssembly());

			Assert.DoesNotThrow(() => manager.ExecuteCommand(nameof(SignalTestCommands.TriggerInfiniteSignalLoop)),
				"Exceeding MaxCascadeDepth must not throw — it must log an error and stop gracefully.");

			Assert.IsTrue(logger.Logs.Exists(l => l.Contains("cascade depth exceeded") || l.Contains("ERR:")),
				"An error must be logged when the cascade depth limit is reached.");
		}

		[Test]
		public void RegisterCommands_InvalidSignalHandlerSignature_Throws()
		{
			// A method without the required signal payload parameter must be rejected at registration.
			// CommandRegistry is internal but visible to tests, so we register the bad handler directly.
			var registry = new CommandRegistry();

			var method = typeof(InvalidSignalHandlers).GetMethod(
				nameof(InvalidSignalHandlers.BadHandler),
				BindingFlags.Public | BindingFlags.Static);

			Assert.Throws<CommandRegistrationException>(
				() => registry.RegisterSignalHandler(method),
				"An invalid signal handler signature must be rejected at registration.");
		}
	}

	[TestFixture]
	public class ExternalEventTests
	{
		private CommandManager _manager;
		private DataManager _dataManager;

		[SetUp]
		public void Setup()
		{
			var storage = new FakeDataStorage();
			_dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());
			_manager = new CommandManager(
				_dataManager, new FakeLogger(), Config.Create(),
				new SystemTimeProvider(), storage);
			_manager.RegisterCommands(Assembly.GetExecutingAssembly());
		}

		[Test]
		public void ExternalEvent_SubscriberIsCalledAfterCommandExecution()
		{
			bool called = false;
			_manager.On<ItemAddedEvent>(_ => called = true);

			_manager.ExecuteCommand(nameof(ExternalEventTestCommands.AddItem));

			Assert.IsTrue(called, "External event subscriber must be called after command execution.");
		}

		[Test]
		public void ExternalEvent_ReceivesCorrectPayload()
		{
			ItemAddedEvent received = default;
			_manager.On<ItemAddedEvent>(evt => received = evt);

			_manager.ExecuteCommand(nameof(ExternalEventTestCommands.AddItem));

			Assert.AreEqual(42, received.ItemId, "Subscriber must receive the exact payload raised by the command.");
		}

		[Test]
		public void ExternalEvent_UnsubscribedHandlerIsNotCalled()
		{
			int callCount = 0;
			Action<ItemAddedEvent> handler = _ => callCount++;

			_manager.On<ItemAddedEvent>(handler);
			_manager.Off<ItemAddedEvent>(handler);

			_manager.ExecuteCommand(nameof(ExternalEventTestCommands.AddItem));

			Assert.AreEqual(0, callCount, "Unsubscribed handler must not be called.");
		}

		[Test]
		public void ExternalEvent_DisposingSubscriptionHandle_StopsDelivery()
		{
			int callCount = 0;
			var subscription = _manager.On<ItemAddedEvent>(_ => callCount++);

			subscription.Dispose();
			_manager.ExecuteCommand(nameof(ExternalEventTestCommands.AddItem));

			Assert.AreEqual(0, callCount, "Disposing the subscription handle must unsubscribe the handler.");
		}

		[Test]
		public void ExternalEvent_MultipleSubscribers_AllCalled()
		{
			int callCount = 0;
			_manager.On<ItemAddedEvent>(_ => callCount++);
			_manager.On<ItemAddedEvent>(_ => callCount++);

			_manager.ExecuteCommand(nameof(ExternalEventTestCommands.AddItem));

			Assert.AreEqual(2, callCount, "All registered subscribers must be called.");
		}

		[Test]
		public void ExternalEvent_IsNotSavedInCommandHistory()
		{
			_manager.On<ItemAddedEvent>(_ => { });
			_manager.ExecuteCommand(nameof(ExternalEventTestCommands.AddItem));

			// The root command is in history; external event dispatch is transparent to the history
			Assert.AreEqual(1, _manager.History.Count,
				"Only the root command must be in history — external event dispatch is not tracked.");
		}
	}

	// ---------------------------------------------------------------------------
	// Signal types
	// ---------------------------------------------------------------------------

	public struct SignalXpArgs { public int Amount; }
	public struct XpGainedSignal { public int Amount; }
	public struct LevelUpSignal { public int NewLevel; }
	public struct InfiniteLoopSignal { }
	public struct RollSignal { }
	public struct PrioritySignal { }

	// ---------------------------------------------------------------------------
	// External event types
	// ---------------------------------------------------------------------------

	public struct ItemAddedEvent { public int ItemId; }

	// ---------------------------------------------------------------------------
	// Data types
	// ---------------------------------------------------------------------------

	public struct SignalCounterData { public int Count; public int LastAmount; }
	public struct SignalLevelData { public int Level; }
	public struct SignalRollData { public int LastRoll; }
	public struct ItemInventoryData { public int Count; }
	public struct PriorityOrderData { public int Value; }

	// ---------------------------------------------------------------------------
	// Commands
	// ---------------------------------------------------------------------------

	public static class SignalTestCommands
	{
		[Command]
		public static void GainXp(CommandContext ctx, SignalXpArgs args)
		{
			ctx.Signals.Send(new XpGainedSignal { Amount = args.Amount });
		}

		[Command]
		public static void RollOnSignal(CommandContext ctx)
		{
			ctx.Signals.Send(new RollSignal());
		}

		[Command]
		public static void TriggerInfiniteSignalLoop(CommandContext ctx)
		{
			ctx.Signals.Send(new InfiniteLoopSignal());
		}

		[Command]
		public static void SendPrioritySignal(CommandContext ctx)
		{
			ctx.Signals.Send(new PrioritySignal());
		}
	}

	public static class ExternalEventTestCommands
	{
		[Command]
		public static void AddItem(CommandContext ctx)
		{
			ctx.Data.Update((ref ItemInventoryData d) => d.Count++);
			ctx.Events.Raise(new ItemAddedEvent { ItemId = 42 });
		}
	}

	// ---------------------------------------------------------------------------
	// Signal handlers
	// ---------------------------------------------------------------------------

	public static class SignalTestHandlers
	{
		[SignalHandler]
		public static void OnXpGained(CommandContext ctx, XpGainedSignal signal)
		{
			ctx.Data.Update((ref SignalCounterData d) =>
			{
				d.Count++;
				d.LastAmount = signal.Amount;
			});

			if (signal.Amount >= 200)
			{
				ctx.Signals.Send(new LevelUpSignal { NewLevel = 1 });
			}
		}

		[SignalHandler]
		public static void OnLevelUp(CommandContext ctx, LevelUpSignal signal)
		{
			ctx.Data.Update((ref SignalLevelData d) => d.Level = signal.NewLevel);
		}

		[SignalHandler]
		public static void OnRollSignal(CommandContext ctx, RollSignal signal)
		{
			int roll = ctx.RNG.Next(1, 100);
			ctx.Data.Update((ref SignalRollData d) => d.LastRoll = roll);
		}

		[SignalHandler]
		public static void OnInfiniteLoopSignal(CommandContext ctx, InfiniteLoopSignal signal)
		{
			// Intentionally sends the same signal to trigger the depth protection
			ctx.Signals.Send(new InfiniteLoopSignal());
		}
	}

	// Used to test invalid handler registration.
	// No [SignalHandler] attribute here — the test registers it manually via reflection
	// so it doesn't pollute the assembly-wide scan in Setup().
	public static class InvalidSignalHandlers
	{
		public static void BadHandler(CommandContext ctx) { } // missing signal payload parameter
	}

	// ---------------------------------------------------------------------------
	// Priority signal handlers — encoding trick: value = value*10 + handlerIndex
	// Execution order (10, 5, 1): 0 → 1 → 12 → 123
	// Wrong order     ( 1, 5,10): 0 → 3 → 32 → 321  (or other permutations)
	// ---------------------------------------------------------------------------

	public static class PrioritySignalHandlers
	{
		[SignalHandler(10)]
		public static void HandlerPrio10(CommandContext ctx, PrioritySignal signal)
		{
			ctx.Data.Update((ref PriorityOrderData d) => d.Value = d.Value * 10 + 1);
		}

		[SignalHandler(5)]
		public static void HandlerPrio5(CommandContext ctx, PrioritySignal signal)
		{
			ctx.Data.Update((ref PriorityOrderData d) => d.Value = d.Value * 10 + 2);
		}

		[SignalHandler(1)]
		public static void HandlerPrio1(CommandContext ctx, PrioritySignal signal)
		{
			ctx.Data.Update((ref PriorityOrderData d) => d.Value = d.Value * 10 + 3);
		}
	}

	// ---------------------------------------------------------------------------
	// Priority tests
	// ---------------------------------------------------------------------------

	[TestFixture]
	public class SignalPriorityTests
	{
		private CommandManager _manager;
		private DataManager _dataManager;

		[SetUp]
		public void Setup()
		{
			var storage = new FakeDataStorage();
			_dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());
			_manager = new CommandManager(
				_dataManager, new FakeLogger(), Config.Create(),
				new SystemTimeProvider(), storage);
			_manager.RegisterCommands(Assembly.GetExecutingAssembly());
		}

		[Test]
		public void SignalHandlers_AreCalledInDescendingPriorityOrder()
		{
			// Three handlers registered for PrioritySignal with priorities 10, 5, 1.
			// Each appends a digit using value = value*10 + N so the result encodes call order:
			//   prio 10 (N=1) then prio 5 (N=2) then prio 1 (N=3) → 0→1→12→123
			_manager.ExecuteCommand(nameof(SignalTestCommands.SendPrioritySignal));

			Assert.AreEqual(123, _dataManager.Get<PriorityOrderData>().Value,
				"Handlers must execute in descending priority order (highest priority first).");
		}

		[Test]
		public void SignalHandlerAttribute_DefaultPriority_IsZero()
		{
			// Verify that the attribute's default priority value is 0 when not specified.
			var method = typeof(SignalTestHandlers).GetMethod(
				nameof(SignalTestHandlers.OnXpGained),
				BindingFlags.Public | BindingFlags.Static);

			var attr = method.GetCustomAttribute<SignalHandlerAttribute>();

			Assert.AreEqual(0, attr.Priority,
				"A handler declared with [SignalHandler] and no argument must have priority 0.");
		}

		[Test]
		public void SignalHandlerAttribute_ExplicitPriority_IsCorrectlyStored()
		{
			// Verify that the integer passed to [SignalHandler(N)] is faithfully stored
			// and retrievable — a prerequisite for the runtime sorting logic.
			Func<string, SignalHandlerAttribute> getAttr = methodName =>
				typeof(PrioritySignalHandlers)
					.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
					.GetCustomAttribute<SignalHandlerAttribute>();

			Assert.AreEqual(10, getAttr(nameof(PrioritySignalHandlers.HandlerPrio10)).Priority);
			Assert.AreEqual(5,  getAttr(nameof(PrioritySignalHandlers.HandlerPrio5)).Priority);
			Assert.AreEqual(1,  getAttr(nameof(PrioritySignalHandlers.HandlerPrio1)).Priority);
		}
	}
}
