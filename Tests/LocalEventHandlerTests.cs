//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.LocalCommands.Core;
using NUnit.Framework;
using System.Reflection;

namespace BlueCheese.LocalCommands.Tests
{
	[TestFixture]
	public class LocalEventHandlerTests
	{
		private LocalCommandManager _manager;
		private DataManager _dataManager;

		[SetUp]
		public void Setup()
		{
			var storage = new FakeDataStorage();
			_dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());
			_manager = new LocalCommandManager(
				_dataManager, new FakeLogger(), Config.Create(),
				new TimeProvider(), storage);
			_manager.RegisterCommands(Assembly.GetExecutingAssembly());
		}

		[Test]
		public void EventHandler_IsCalledAfterCommandRaisesEvent()
		{
			_manager.ExecuteCommand(nameof(EventTestCommands.GainXp), new XpArgs { Amount = 50 });

			// XpGainedEvent should trigger OnXpGained, which increments EventsFired
			Assert.AreEqual(1, _dataManager.Get<EventCounterData>().Count,
				"Event handler must be called once when the command raises one event.");
		}

		[Test]
		public void EventHandler_ReceivesCorrectPayload()
		{
			_manager.ExecuteCommand(nameof(EventTestCommands.GainXp), new XpArgs { Amount = 75 });

			// OnXpGained copies the amount into LastEventAmount
			Assert.AreEqual(75, _dataManager.Get<EventCounterData>().LastEventAmount,
				"Event handler must receive the exact payload raised by the command.");
		}

		[Test]
		public void EventHandler_CanModifyData()
		{
			_manager.ExecuteCommand(nameof(EventTestCommands.GainXp), new XpArgs { Amount = 200 });

			// Reaching 200 xp triggers OnLevelUp via a second event raised inside OnXpGained
			Assert.AreEqual(1, _dataManager.Get<PlayerLevelData>().Level,
				"An event handler that raises another event must trigger the downstream handler.");
		}

		[Test]
		public void EventHandler_CascadingEvents_AreAllProcessed()
		{
			// GainXp(200) → XpGainedEvent → OnXpGained → LevelUpEvent → OnLevelUp
			_manager.ExecuteCommand(nameof(EventTestCommands.GainXp), new XpArgs { Amount = 200 });

			Assert.AreEqual(1, _dataManager.Get<EventCounterData>().Count, "XpGained handler called once.");
			Assert.AreEqual(1, _dataManager.Get<PlayerLevelData>().Level, "LevelUp handler called once.");
		}

		[Test]
		public void EventHandler_CommandIsEnqueuedInHistory_EvenWhenHandlerModifiesData()
		{
			_manager.ExecuteCommand(nameof(EventTestCommands.GainXp), new XpArgs { Amount = 50 });

			// Only the root command should appear in history, not the handler
			Assert.AreEqual(1, _manager.History.Count,
				"Only the root command must be stored in history, not its event handlers.");
			Assert.AreEqual(nameof(EventTestCommands.GainXp), _manager.History[0].CommandName,
				"The history entry must reference the originating command.");
		}

		[Test]
		public void EventHandler_IsDeterministic_SameRngOnReplay()
		{
			// Execute and capture RNG output stored by the handler
			_manager.ExecuteCommand(nameof(EventTestCommands.RollOnEvent));
			int firstRoll = _dataManager.Get<RollData>().LastRoll;

			// Reset data and replay
			_dataManager.Set(new RollData());
			_dataManager.Flush();
			_manager.ReplayCommand(_manager.History[0]);
			int replayRoll = _dataManager.Get<RollData>().LastRoll;

			Assert.AreEqual(firstRoll, replayRoll,
				"Event handler RNG must be deterministic: replay must produce the same values.");
		}

		[Test]
		public void EventHandler_MaxCascadeDepth_IsRespected()
		{
			// InfiniteLoopCommand raises InfiniteLoopEvent whose handler raises the same event again.
			// The cascade must stop at EventContext.MaxCascadeDepth without throwing or looping forever.
			var storage = new FakeDataStorage();
			var dataManager = new DataManager(storage, new NewtonsoftJsonSerializer());
			var logger = new FakeLogger();
			var manager = new LocalCommandManager(dataManager, logger, Config.Create(), new TimeProvider(), storage);
			manager.RegisterCommands(Assembly.GetExecutingAssembly());

			Assert.DoesNotThrow(() => manager.ExecuteCommand(nameof(EventTestCommands.TriggerInfiniteLoop)),
				"Exceeding MaxCascadeDepth must not throw — it must log an error and stop gracefully.");

			Assert.IsTrue(logger.Logs.Exists(l => l.Contains("cascade depth exceeded") || l.Contains("ERR:")),
				"An error must be logged when the cascade depth limit is reached.");
		}

		[Test]
		public void RegisterCommands_InvalidHandlerSignature_Throws()
		{
			// A method without the required event payload parameter should fail at registration.
			// RegisterEventHandler is private, so we invoke it via reflection — which wraps the
			// exception in a TargetInvocationException. We unwrap it to verify the root cause.
			var manager = new LocalCommandManager(
				_dataManager, new FakeLogger(), Config.Create(),
				new TimeProvider(), new FakeDataStorage());

			var method = typeof(InvalidEventHandlers).GetMethod(
				nameof(InvalidEventHandlers.BadHandler),
				System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

			var registerMethod = typeof(LocalCommandManager).GetMethod(
				"RegisterEventHandler",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			var ex = Assert.Throws<System.Reflection.TargetInvocationException>(
				() => registerMethod.Invoke(manager, new object[] { method }));

			Assert.IsInstanceOf<CommandRegistrationException>(ex.InnerException,
				"The root cause must be a CommandRegistrationException for an invalid handler signature.");
		}
	}

	// ---------------------------------------------------------------------------
	// Data types used by event tests
	// ---------------------------------------------------------------------------

	public struct XpArgs { public int Amount; }

	public struct XpGainedEvent { public int Amount; }

	public struct LevelUpEvent { public int NewLevel; }

	public struct InfiniteLoopEvent { }

	public struct RollEvent { }

	public struct EventCounterData
	{
		public int Count;
		public int LastEventAmount;
	}

	public struct PlayerLevelData { public int Level; }

	public struct RollData { public int LastRoll; }

	// ---------------------------------------------------------------------------
	// Commands and handlers used by event tests
	// ---------------------------------------------------------------------------

	public static class EventTestCommands
	{
		[LocalCommand]
		public static void GainXp(Context ctx, XpArgs args)
		{
			ctx.Events.Raise(new XpGainedEvent { Amount = args.Amount });
		}

		[LocalCommand]
		public static void RollOnEvent(Context ctx)
		{
			ctx.Events.Raise(new RollEvent());
		}

		[LocalCommand]
		public static void TriggerInfiniteLoop(Context ctx)
		{
			ctx.Events.Raise(new InfiniteLoopEvent());
		}
	}

	public static class EventTestHandlers
	{
		[LocalEventHandler]
		public static void OnXpGained(Context ctx, XpGainedEvent evt)
		{
			ctx.Data.Update((ref EventCounterData d) =>
			{
				d.Count++;
				d.LastEventAmount = evt.Amount;
			});

			// Reaching 200 XP triggers a level up
			if (evt.Amount >= 200)
			{
				ctx.Events.Raise(new LevelUpEvent { NewLevel = 1 });
			}
		}

		[LocalEventHandler]
		public static void OnLevelUp(Context ctx, LevelUpEvent evt)
		{
			ctx.Data.Update((ref PlayerLevelData d) => d.Level = evt.NewLevel);
		}

		[LocalEventHandler]
		public static void OnRollEvent(Context ctx, RollEvent evt)
		{
			int roll = ctx.RNG.Next(1, 100);
			ctx.Data.Update((ref RollData d) => d.LastRoll = roll);
		}

		[LocalEventHandler]
		public static void OnInfiniteLoop(Context ctx, InfiniteLoopEvent evt)
		{
			// Intentionally raises the same event to trigger depth protection
			ctx.Events.Raise(new InfiniteLoopEvent());
		}
	}

	// Used to test invalid handler registration.
	// No [LocalEventHandler] attribute here — the test registers it manually via reflection
	// so it doesn't pollute the assembly-wide scan in Setup().
	public static class InvalidEventHandlers
	{
		public static void BadHandler(Context ctx) { } // missing event payload parameter
	}
}
