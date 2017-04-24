using Newtonsoft.Json;
using System.Collections.Generic;

namespace MattermostDriver
{
	/// <summary>
	/// The websocket response format. Data is implemented in specific event classes.
	/// </summary>
	public class IResponse
	{
		/// <summary>
		/// The websocket event type.
		/// </summary>
		public string @event;
		/// <summary>
		/// Contains information about who received this event.
		/// </summary>
		public InternalBroadcast broadcast;

		/// <summary>
		/// Contains information about who received this event.
		/// </summary>
		public class InternalBroadcast
		{
			/// <summary>
			/// A Dictionary of users who did not receive this event (often the user that triggered this event).
			/// </summary>
			public Dictionary<string, bool> omit_users;
			/// <summary>
			/// If not empty, this user was sent this event.
			/// </summary>
			public string user_id;
			/// <summary>
			/// If not empty, all users in this channel were sent this event.
			/// </summary>
			public string channel_id;
			/// <summary>
			/// If not empty, all users in this team were sent this event.
			/// </summary>
			public string team_id;
		}
	}

	/// <summary>
	/// The Hello websocket event.
	/// </summary>
	public class HelloEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The server version.
			/// </summary>
			public string server_version;
		}

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Server Version: {data.server_version}";
		}
	}

	/// <summary>
	/// The StatusChange websocket event.
	/// </summary>
	public class StatusChangeEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The user's new status.
			/// </summary>
			public string status;
			/// <summary>
			/// The user's ID.
			/// </summary>
			public string user_id;
		}

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Status: {data.status} | User ID: {data.user_id}";
		}
	}

	/// <summary>
	/// The Typing websocket event.
	/// </summary>
	public class TypingEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			public string parent_id;
			/// <summary>
			/// The user's ID.
			/// </summary>
			public string user_id;
		}

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
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

	/// <summary>
	/// The Posted websocket event.
	/// </summary>
	public class PostedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The channel's display name.
			/// </summary>
			public string channel_display_name;
			/// <summary>
			/// The channel's name.
			/// </summary>
			public string channel_name;
			/// <summary>
			/// The channel's type.
			/// </summary>
			public string channel_type;
			/// <summary>
			/// The Post object.
			/// </summary>
			public Post post;
			/// <summary>
			/// The sender's name.
			/// </summary>
			public string sender_name;
			/// <summary>
			/// The team's ID.
			/// </summary>
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

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Post ID: {data.post.ID} | Sender Name: {data.sender_name} | Message: {data.post.Message}";
		}
	}

	/// <summary>
	/// The NewUser websocket event.
	/// </summary>
	public class NewUserEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The user's ID.
			/// </summary>
			public string user_id;
		}

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"User ID: {data.user_id}";
		}
	}

	/// <summary>
	/// The ChannelDeleted websocket event.
	/// </summary>
	public class ChannelDeletedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The channel's ID.
			/// </summary>
			public string channel_id;
		}

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Channel ID: {data.channel_id}";
		}
	}

	/// <summary>
	/// The DirectAdded websocket event.
	/// </summary>
	public class DirectAddedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The ID of the other user in the Direct channel.
			/// </summary>
			public string teammate_id;
		}

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Teammate ID: {data.teammate_id}";
		}
	}

	/// <summary>
	/// The UserUpdated websocket event.
	/// </summary>
	public class UserUpdatedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The newly-updated User object.
			/// </summary>
			public User user;
		}

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"User ID: {data.user.ID} | Username: {data.user.Username}";
		}
	}

	/// <summary>
	/// The websocket event for UserAdded and LeaveTeam.
	/// </summary>
	public class TeamChangeEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The teams's ID.
			/// </summary>
			public string team_id;
			/// <summary>
			/// The user's ID.
			/// </summary>
			public string user_id;
		}

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Team ID: {data.team_id} | User ID: {data.user_id}";
		}
	}

	internal class PreEphemeralMessageEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		internal class Data
		{
			public string post;
		}
	}

	/// <summary>
	/// The Ephemeral websocket event.
	/// </summary>
	public class EphemeralMessageEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The Post object.
			/// </summary>
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

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Post ID: {data.post.ID} | Message: {data.post.Message}";
		}
	}

	internal class PrePreferenceChangedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		internal class Data
		{
			public string preference;
		}
	}

	/// <summary>
	/// The PreferenceChanged websocket event.
	/// </summary>
	public class PreferenceChangedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The newly-updated Preference object.
			/// </summary>
			public Preference preference;
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

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Preference: User ID: {data.preference.UserID} | Category: {data.preference.Category} | Name: {data.preference.Name} | Value: {data.preference.Value}";
		}
	}

	/// <summary>
	/// The UserRemoved websocket event.
	/// </summary>
	public class UserRemovedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The ID of the user who removed this user.
			/// </summary>
			public string remover_id;
			/// <summary>
			/// The user's ID.
			/// </summary>
			public string user_id;
		}

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Remover ID: {data.remover_id} | User ID: {data.user_id}";
		}
	}

	internal class PrePostDeletedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		internal class Data
		{
			public string post;
		}
	}

	/// <summary>
	/// The PostDeleted websocket event.
	/// </summary>
	public class PostDeletedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The post that was deleted.
			/// </summary>
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

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Post ID: {data.post.ID} | Message: {data.post.Message}";
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

	/// <summary>
	/// The PostEdited websocket event.
	/// </summary>
	public class PostEditedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The newly-updated Post object.
			/// </summary>
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

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Post ID: {data.post.ID} | Message: {data.post.Message}";
		}
	}

	internal class PreReactionChangedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		internal class Data
		{
			public string reaction;
		}
	}

	/// <summary>
	/// The websocket event for ReactionAdded and ReactionRemoved
	/// </summary>
	public class ReactionChangedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The reaction that was added/removed.
			/// </summary>
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

		/// <summary>
		/// Displays a human-readable summary of this event.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"Reaction Name: {data.reaction.EmojiName}";
		}
	}

	/// <summary>
	/// The ChannelViewed websocket event.
	/// </summary>
	public class ChannelViewedEvent : IResponse
	{
		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public Data data;

		/// <summary>
		/// Contains specific event-related information.
		/// </summary>
		public class Data
		{
			/// <summary>
			/// The channel's ID.
			/// </summary>
			public string channel_id;
		}
	}
}
