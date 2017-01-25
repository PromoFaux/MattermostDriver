using Newtonsoft.Json;

namespace MattermostDriver
{
	public static class Extensions
	{
		public static T Deserialize<T>(this string rawdata)
		{
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<T>(rawdata);
			else
				return default(T);
		}
	}
}
