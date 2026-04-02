//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;

namespace BlueCheese.LocalCommands.Core
{
	public interface ICommandSyncService
	{
		Task<SyncResponse> SyncCommandsAsync(SyncRequest request);
	}
}
