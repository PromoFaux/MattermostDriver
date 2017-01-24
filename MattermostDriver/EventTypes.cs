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

		internal void Import(PrePostedEvent e)
		{
			broadcast = e.broadcast;
			@event = e.@event;
			data = new Data()
			{
				channel_display_name = e.data.channel_display_name,
				channel_name = e.data.channel_name,
				channel_type = e.data.channel_type,
				post = JsonConvert.DeserializeObject<Post>(e.data.post),
				sender_name = e.data.sender_name,
				team_id = e.data.team_id
			};
		}

		public override string ToString()
		{
			return $"Post ID: {data.post.id} | Sender Name: {data.sender_name} | Message: {data.post.message}";
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

	public class ChannelDeletedEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public string channel_id;
		}

		public override string ToString()
		{
			return $"Channel ID: {data.channel_id}";
		}
	}

	public class DirectAddedEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public string teammate_id;
		}

		public override string ToString()
		{
			return $"Teammate ID: {data.teammate_id}";
		}
	}

	public class UserUpdatedEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public User user;
		}

		public override string ToString()
		{
			return $"User ID: {data.user.id} | Username: {data.user.username}";
		}
	}

	//UserAdded - LeaveTeam
	public class TeamChangeEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public string team_id;
			public string user_id;
		}

		public override string ToString()
		{
			return $"Team ID: {data.team_id} | User ID: {data.user_id}";
		}
	}

	internal class PreEphemeralMessageEvent : IResponse
	{
		public Data data;

		internal class Data
		{
			public string post;
		}
	}

	public class EphemeralMessageEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public Post post;
		}

		internal void Import(PreEphemeralMessageEvent e)
		{
			broadcast = e.broadcast;
			@event = e.@event;
			data = new Data()
			{
				post = JsonConvert.DeserializeObject<Post>(e.data.post)
			};
		}

		public override string ToString()
		{
			return $"Post ID: {data.post.id} | Message: {data.post.message}";
		}
	}

	internal class PrePreferenceChangedEvent : IResponse
	{
		public Data data;

		internal class Data
		{
			public string preference;
		}
	}

	public class PreferenceChangedEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public Preference preference;
		}

		public class Preference
		{
			public string user_id;
			public string category;
			public string name;
			public string value;
		}

		internal void Import(PrePreferenceChangedEvent e)
		{
			broadcast = e.broadcast;
			@event = e.@event;
			data = new Data()
			{
				preference = JsonConvert.DeserializeObject<Preference>(e.data.preference)
			};
		}

		public override string ToString()
		{
			return $"Preference: User ID: {data.preference.user_id} | Category: {data.preference.category} | Name: {data.preference.name} | Value: {data.preference.value}";
		}
	}

	public class UserRemovedEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public string remover_id;
			public string user_id;
		}

		public override string ToString()
		{
			return $"Remover ID: {data.remover_id} | User ID: {data.user_id}";
		}
	}

	internal class PrePostDeletedEvent : IResponse
	{
		public Data data;

		internal class Data
		{
			public string post;
		}
	}

	public class PostDeletedEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public Post post;
		}

		internal void Import(PrePostDeletedEvent e)
		{
			broadcast = e.broadcast;
			@event = e.@event;
			data = new Data()
			{
				post = JsonConvert.DeserializeObject<Post>(e.data.post)
			};
		}

		public override string ToString()
		{
			return $"Post ID: {data.post.id} | Message: {data.post.message}";
		}
	}

	internal class PrePostEditedEvent : IResponse
	{
		public Data data;

		internal class Data
		{
			public string post;
		}
	}

	public class PostEditedEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public Post post;
		}

		internal void Import(PrePostEditedEvent e)
		{
			broadcast = e.broadcast;
			@event = e.@event;
			data = new Data()
			{
				post = JsonConvert.DeserializeObject<Post>(e.data.post)
			};
		}

		public override string ToString()
		{
			return $"Post ID: {data.post.id} | Message: {data.post.message}";
		}
	}

	//ReactionAdded - ReactionRemoved
	internal class PreReactionChangedEvent : IResponse
	{
		public Data data;

		internal class Data
		{
			public string reaction;
		}
	}

	public class ReactionChangedEvent : IResponse
	{
		public Data data;

		public class Data
		{
			public Reaction reaction;
		}

		internal void Import(PreReactionChangedEvent e)
		{
			broadcast = e.broadcast;
			@event = e.@event;
			data = new Data()
			{
				reaction = JsonConvert.DeserializeObject<Reaction>(e.data.reaction)
			};
		}

		public override string ToString()
		{
			return $"Reaction Name: {data.reaction.emoji_name}";
		}
	}
}
