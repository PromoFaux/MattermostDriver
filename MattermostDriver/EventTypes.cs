using System.Collections.Generic;

namespace MattermostDriver
{
	public class IResponse
	{
		public string @event;
		//We're letting individual response types handle their own Data implementations
		public InternalBroadcast broadcast;

		public class InternalBroadcast
		{
			public Dictionary<string, bool> omit_users;
			public string user_id;
			public string channel_id;
			public string team_id;
		}
	}

	public class HelloEvent : IResponse
	{
		public HData data;

		public class HData
		{
			public string server_version;
		}

		public override string ToString()
		{
			return $"Server Version: {data.server_version}";
		}
	}

	public class StatusChangeEvent : IResponse
	{
		public SCData data;

		public class SCData
		{
			public string status;
			public string user_id;
		}

		public override string ToString()
		{
			return $"Status: {data.status} | User_ID: {data.user_id}";
		}
	}
}
