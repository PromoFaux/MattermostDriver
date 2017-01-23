using Newtonsoft.Json;
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
		public Data data;

		public class Data
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
		public Data data;

		public class Data
		{
			public string status;
			public string user_id;
		}

		public override string ToString()
		{
			return $"Status: {data.status} | User ID: {data.user_id}";
		}
	}

	public class TypingEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public string parent_id;
			public string user_id;
		}

		public override string ToString()
		{
			return $"Parent ID: {data.parent_id} | User ID: {data.user_id}";
		}
	}

	internal class PrePostedEvent : IResponse
	{
		public Data data;

		internal class Data
		{
			public string channel_display_name;
			public string channel_name;
			public string channel_type;
			public string post;
			public string sender_name;
			public string team_id;
		}
	}

	public class PostedEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public string channel_display_name;
			public string channel_name;
			public string channel_type;
			public Post post;
			public string sender_name;
			public string team_id;
		}

		public override string ToString()
		{
			return $"Channel Display Name: {data.channel_display_name} | Channel Name: {data.channel_name} | Channel Type: {data.channel_type} | Sender Name: {data.sender_name} | Team ID: {data.team_id}";
		}
	}

	public class NewUserEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public string user_id;
		}

		public override string ToString()
		{
			return $"User ID: {data.user_id}";
		}
	}
}
