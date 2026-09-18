using System;
using UnityEngine;

// Fully qualifies ILogger: UnityEngine also declares an ILogger (Debug.unityLogger), which would
// otherwise be ambiguous with BlueCheese.CommandSync.Core.ILogger in this file's using scope.
public class UnityLogger : BlueCheese.CommandSync.Core.ILogger
{
	public void Log(string message) => Debug.Log(message);

	public void LogWarning(string message) => Debug.LogWarning(message);

	public void LogError(string message) => Debug.LogError(message);

	public void LogException(Exception exception) => Debug.LogException(exception);
}
