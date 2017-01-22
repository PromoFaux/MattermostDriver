namespace MattermostDriver
{
	public interface ILogger
	{
		void Debug(string message);
		void Info(string message);
		void Warn(string message);
		void Error(string message);
	}

	public enum LogLevel
	{
		Debug,
		Info,
		Warn,
		Error
	}
}
