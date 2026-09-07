using System;
using System.Collections.Generic;

namespace BlueCheese.CommandSync.Core
{
	[Serializable]
	public class FetchResponse : Response
	{
		public Dictionary<string, string> Data { get; set; }

		static public FetchResponse Ok(Dictionary<string, string> data) => new()
		{
			Success = true,
			Data = data,
		};

		static public FetchResponse Fail(string errorMessage) => new()
		{
			Success = false,
			Message = errorMessage,
			Data = null,
		};
	}
}
