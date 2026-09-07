namespace BlueCheese.CommandSync.Core
{
	public interface IConfig
	{
		string Version { get; }

		T Get<T>(string key, T defaultValue = default);
	}
}
