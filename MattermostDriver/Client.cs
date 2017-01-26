using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WebSocket4Net;

namespace MattermostDriver
{
	public class Client
	{
		internal static ILogger logger;
		private WebSocket socket;
		private int seq;
		private bool awaiting_ok;

		#region Events
		public event EventHandler WebsocketConnected;
		public event HelloEventHandler Hello;
		public event StatusChangeEventHandler StatusChange;
		public event TypingEventHandler Typing;
		public event PostedEventHandler Posted;
		public event NewUserEventHandler NewUser;
		public event ChannelDeletedEventHandler ChannelDeleted;
		public event DirectAddedEventHandler DirectAdded;
		public event UserUpdatedEventHandler UserUpdated;
		public event TeamChangeEventHandler UserAdded;
		public event TeamChangeEventHandler LeaveTeam;
		public event EphemeralMessageEventHandler EphemeralMessage;
		public event PreferenceChangedEventHandler PreferenceChanged;
		public event UserRemovedEventHandler UserRemoved;
		public event PostDeletedEventHandler PostDeleted;
		public event PostEditedEventHandler PostEdited;
		public event ReactionChangedEventHandler ReactionAdded;
		public event ReactionChangedEventHandler ReactionRemoved;
		public event ChannelViewedEventHandler ChannelViewed;
		#endregion

		public Self Connect(string url, string username, string password, ILogger logger)
		{
			//Setup logging
			Client.logger = logger;

			string websocket;

			//Remove last '/' for consistency
			if (url.EndsWith("/"))
				url = url.TrimEnd('/');

			//Generate API base url
			API.ApiBase = url + "/api/v4";

			//Generate websocket url
			if (url.StartsWith("https"))
				websocket = "wss" + url.Substring(5) + "/api/v4/users/websocket";
			else if (url.StartsWith("http"))
				websocket = "ws" + url.Substring(4) + "/api/v4/users/websocket";
			else
			{
				logger.Error($"Invalid server URL in Client.Connect(): {url}");
				throw new Exception("Invalid server URL.");
			}

			API.Initialize();

			//Login and receive Session Token
			Self self = API.PostGetAuth(new { login_id = username, password = password });

			//Connect to Websocket
			socket = new WebSocket(websocket);
			socket.Opened += OnWebsocketOpen;
			socket.MessageReceived += OnWebsocketMessage;
			socket.Closed += OnWebsocketClose;
			socket.Open();
			seq = 1;

			return self;
		}

		#region Websocket Handlers
		private void OnWebsocketOpen(object sender, EventArgs e)
		{
			WebsocketConnected?.Invoke();
			logger.Debug("Websocket-open event thrown. Sending authentication challenge.");

			//Authenticate over Websocket
			var request = new { seq = seq, action = "authentication_challenge", data = new { token = API.token } };
			awaiting_ok = true;
			socket.Send(JsonConvert.SerializeObject(request));
		}

		private void OnWebsocketMessage(object sender, MessageReceivedEventArgs e)
		{
			string rawdata = e.Message;

			//Specially handle Auth 'OK' message
			if (awaiting_ok)
			{
				var res = JsonConvert.DeserializeAnonymousType(rawdata, new { status = "", seq_reply = "" });

				if (res.status != "OK")
					logger.Warn("OK not received via websocket. Full message: " + rawdata);
				else
					logger.Debug("Authentication challenge successful. Awaiting hello event.");
				awaiting_ok = false;
				return;
			}

			//Websocket Event Handling
			IResponse response = JsonConvert.DeserializeObject<IResponse>(rawdata);

			switch (response.@event)
			{
				case "channel_deleted":
					ChannelDeletedEvent cdevent = JsonConvert.DeserializeObject<ChannelDeletedEvent>(rawdata);
					logger.Debug("Channel deleted event received: " + cdevent.ToString());
					ChannelDeleted?.Invoke(cdevent);
					break;
				case "channel_viewed":
					ChannelViewedEvent cvevent = JsonConvert.DeserializeObject<ChannelViewedEvent>(rawdata);
					logger.Debug("Channel viewed event received: " + cvevent.ToString());
					ChannelViewed?.Invoke(cvevent);
					break;
				case "direct_added":
					DirectAddedEvent daevent = JsonConvert.DeserializeObject<DirectAddedEvent>(rawdata);
					logger.Debug("Direct added event received: " + daevent.ToString());
					DirectAdded?.Invoke(daevent);
					break;
				case "ephemeral_message":
					EphemeralMessageEvent emevent = JsonConvert.DeserializeObject<EphemeralMessageEvent>(rawdata);
					logger.Debug("Ephemeral message event received: " + emevent.ToString());
					EphemeralMessage?.Invoke(emevent);
					break;
				case "hello":
					logger.Debug("Hello event received.");
					Hello?.Invoke(JsonConvert.DeserializeObject<HelloEvent>(rawdata));
					break;
				case "leave_team":
					TeamChangeEvent ltevent = JsonConvert.DeserializeObject<TeamChangeEvent>(rawdata);
					logger.Debug("Leave team event received: " + ltevent.ToString());
					LeaveTeam?.Invoke(ltevent);
					break;
				case "new_user":
					NewUserEvent nuevent = JsonConvert.DeserializeObject<NewUserEvent>(rawdata);
					logger.Debug("New user event received: " + nuevent.ToString());
					NewUser?.Invoke(nuevent);
					break;
				case "post_deleted":
					PrePostDeletedEvent ppdevent = JsonConvert.DeserializeObject<PrePostDeletedEvent>(rawdata);
					PostDeletedEvent pdevent = new PostDeletedEvent();
					pdevent.Import(ppdevent);
					logger.Debug("Post deleted event received: " + pdevent.ToString());
					PostDeleted?.Invoke(pdevent);
					break;
				case "post_edited":
					PrePostEditedEvent ppeevent = JsonConvert.DeserializeObject<PrePostEditedEvent>(rawdata);
					PostEditedEvent peevent = new PostEditedEvent();
					peevent.Import(ppeevent);
					logger.Debug("Post edited event received: " + peevent.ToString());
					PostEdited?.Invoke(peevent);
					break;
				case "posted":
					PrePostedEvent ppevent = JsonConvert.DeserializeObject<PrePostedEvent>(rawdata);
					PostedEvent pevent = new PostedEvent();
					pevent.Import(ppevent);
					logger.Debug("Posted event received: " + pevent.ToString());
					Posted?.Invoke(pevent);
					break;
				case "preference_changed":
					PrePreferenceChangedEvent ppcevent = JsonConvert.DeserializeObject<PrePreferenceChangedEvent>(rawdata);
					PreferenceChangedEvent pcevent = new PreferenceChangedEvent();
					pcevent.Import(ppcevent);
					logger.Debug("Preference changed event received: " + pcevent.ToString());
					PreferenceChanged?.Invoke(pcevent);
					break;
				case "reaction_added":
					PreReactionChangedEvent praevent = JsonConvert.DeserializeObject<PreReactionChangedEvent>(rawdata);
					ReactionChangedEvent raevent = new ReactionChangedEvent();
					raevent.Import(praevent);
					logger.Debug("Reaction added event received: " + raevent.ToString());
					ReactionAdded?.Invoke(raevent);
					break;
				case "reaction_removed":
					PreReactionChangedEvent prrevent = JsonConvert.DeserializeObject<PreReactionChangedEvent>(rawdata);
					ReactionChangedEvent rrevent = new ReactionChangedEvent();
					rrevent.Import(prrevent);
					logger.Debug("Reaction removed event received: " + rrevent.ToString());
					ReactionRemoved?.Invoke(rrevent);
					break;
				case "status_change":
					StatusChangeEvent scevent = JsonConvert.DeserializeObject<StatusChangeEvent>(rawdata);
					logger.Debug("Status change event received: " + scevent.ToString());
					StatusChange?.Invoke(scevent);
					break;
				case "typing":
					TypingEvent tevent = JsonConvert.DeserializeObject<TypingEvent>(rawdata);
					logger.Debug($"Typing event received: " + tevent.ToString());
					Typing?.Invoke(tevent);
					break;
				case "user_added":
					TeamChangeEvent uaevent = JsonConvert.DeserializeObject<TeamChangeEvent>(rawdata);
					logger.Debug("User added event received: " + uaevent.ToString());
					UserAdded?.Invoke(uaevent);
					break;
				case "user_removed":
					UserRemovedEvent urevent = JsonConvert.DeserializeObject<UserRemovedEvent>(rawdata);
					logger.Debug("User removed event received: " + urevent.ToString());
					UserRemoved?.Invoke(urevent);
					break;
				case "user_updated":
					UserUpdatedEvent uuevent = JsonConvert.DeserializeObject<UserUpdatedEvent>(rawdata);
					logger.Debug($"User updated event received: " + uuevent.ToString());
					UserUpdated?.Invoke(uuevent);
					break;
				default:
					logger.Warn("Unhandled event type received: " + rawdata);
					break;
			}

		}

		private void OnWebsocketClose(object sender, EventArgs args)
		{
			logger.Warn("Websocket closed.");
		}
		#endregion

		#region User Methods
		[ApiRoute("/users", RequestType.POST)]
		public User CreateUser(string email, string username, string password, string first_name = "", string last_name = "", string nickname = "", string locale = "")
		{
			throw new NotImplementedException();
			var obj = new { email = email, username = username, password = password, first_name = first_name, last_name = last_name, nickname = nickname, locale = locale };
			return API.Post<User>($"/users", obj);
		}

		[ApiRoute("/users/{user_id}", RequestType.PUT)]
		public User UpdateUser(User user)
		{
			throw new NotImplementedException();
			return API.Put<User>($"/users/{user.id}", user);
		}

		[ApiRoute("/users/{user_id}/patch", RequestType.PUT)]
		public User UpdateUser(string user_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/roles", RequestType.PUT)]
		public void UpdateUserRoles(string user_id, string new_roles, string team_id = "")
		{
			throw new NotImplementedException();
			if (!string.IsNullOrWhiteSpace(team_id))
			{
				var obj = new { user_id = user_id, team_id = team_id, new_roles = new_roles };
				API.Put<string>($"/users/{user_id}/roles", obj);
			}
			else
			{
				var obj = new { user_id = user_id, new_roles = new_roles };
				API.Put<string>($"/users/{user_id}/roles", obj);
			}
		}

		[ApiRoute("/users/{user_id}/password", RequestType.PUT)]
		public void UpdatePassword(string user_id, string current_password, string new_password)
		{
			throw new NotImplementedException();
			var obj = new { user_id = user_id, current_password = current_password, new_password = new_password };
			API.Put<string>($"/users/{user_id}/password", obj);
		}

		[ApiRoute("/users/{user_id}/password/reset", RequestType.POST)]
		public void ResetMyPassword(string user_id, string new_password)
		{
			throw new NotImplementedException();
			var obj = new { new_password = new_password };
			API.Post<string>($"/users/{user_id}/password/reset", obj);
		}

		[ApiRoute("/users/{user_id}/password/reset/send", RequestType.POST)]
		public void SendResetPassword(string user_id, string email)
		{
			throw new NotImplementedException();
			var obj = new { email = email };
			API.Post<string>($"/users/{user_id}/password/reset/send", obj);
		}

		[ApiRoute("/users", RequestType.GET)]
		public List<User> GetUsers(int page, int per_page, string in_team = "", string in_channel = "", string not_in_channel = "")
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
			if (!string.IsNullOrWhiteSpace(in_team))
				options.Add("in_team", in_team);
			if (!string.IsNullOrWhiteSpace(in_channel))
				options.Add("in_channel", in_channel);
			if (!string.IsNullOrWhiteSpace(not_in_channel))
				options.Add("not_in_channel", not_in_channel);

			return API.Get<List<User>>("/users", options);
		}

		[ApiRoute("/users/{user_id}", RequestType.GET)]
		public User GetUserByID(string user_id)
		{
			throw new NotImplementedException();
			return API.Get<User>($"/users/{user_id}");
		}

		[ApiRoute("/users/username/{username}", RequestType.GET)]
		public User GetUserByName(string username)
		{
			throw new NotImplementedException();
			return API.Get<User>($"/users/username/{username}");
		}

		[ApiRoute("/users/email/{email}", RequestType.GET)]
		public User GetUserByEmail(string email)
		{
			throw new NotImplementedException();
			return API.Get<User>($"/users/email/{email}");
		}

		[ApiRoute("/users/ids", RequestType.POST)]
		public Dictionary<string, User> GetUsersByIDs(List<string> ids)
		{
			throw new NotImplementedException();
			return API.Post<Dictionary<string, User>>($"/users/ids", ids);
		}

		[ApiRoute("/users/search", RequestType.POST)]
		public List<User> SearchUsers(string term, string team_id = "", string in_channel_id = "", string not_in_channel_id = "", bool allow_inactive = false)
		{
			throw new NotImplementedException();
			var obj = new { term = term, team_id = team_id, in_channel_id = in_channel_id, not_in_channel_id = not_in_channel_id, allow_inactive = allow_inactive };
			return API.Post<List<User>>($"/users/search", obj);
		}

		[ApiRoute("/users/autocomplete", RequestType.GET)]
		public List<User> AutoCompleteUsers(string term, string in_channel = "", string in_team = "")
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "term", term }
			};
			if (!string.IsNullOrWhiteSpace(in_channel))
				options.Add("in_channel", in_channel);
			if (!string.IsNullOrWhiteSpace(in_team))
				options.Add("in_team", in_team);

			return API.Get<List<User>>($"/users/autocomplete", options);
		}

		[ApiRoute("/users/{user_id}/email/verify", RequestType.POST)]
		public void VerifyEmail(string user_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/email/verify/send", RequestType.POST)]
		public void SendEmailVerification(string user_id)
		{
			throw new NotImplementedException();
		}

		// Not implemented
		// [ApiRoute("/users/{user_id}/image"), RequestType.POST)]
		// [ApiRoute("/users/{user_id}/image"), RequestType.GET)]

		[ApiRoute("/users/login", RequestType.POST)]
		public Self Login(string login_id, string password, string token = "", string device_id = "")
		{
			throw new NotImplementedException();
			var obj = new { login_id = login_id, password = password, token = token, device_id = device_id };
			return API.PostGetAuth(obj);
		}

		[ApiRoute("/users/logout", RequestType.POST)]
		public void Logout()
		{
			throw new NotImplementedException();
			API.Post<string>($"/users/logout", null);
		}

		[ApiRoute("/users/login/switch", RequestType.POST)]
		public void SwitchLoginType()
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/sessions", RequestType.GET)]
		public void GetSessions(string user_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/sessions/revoke", RequestType.POST)]
		public void RevokeSessions(string user_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/audits", RequestType.GET)]
		public List<Audit> GetAudits(string user_id)
		{
			throw new NotImplementedException();
			return API.Get<List<Audit>>("/users/{user_id}/audits");
		}

		[ApiRoute("/users/{user_id}/device", RequestType.PUT)]
		public void UpdateDeviceID(string user_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/mfa", RequestType.GET)]
		public bool CheckMFAActive(string user_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/mfa", RequestType.PUT)]
		public void UpdateMFA(string user_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/mfa/generate", RequestType.POST)]
		public void GenerateMFA(string user_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/delete", RequestType.DELETE)]
		public void DeactivateUser(string user_id)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Team Methods
		[ApiRoute("/teams", RequestType.POST)]
		public Team CreateTeam(string name, string display_name, string type)
		{
			throw new NotImplementedException();
			var obj = new { name = name, display_name = display_name, type = type };
			return API.Post<Team>($"/teams", obj);
		}

		[ApiRoute("/teams", RequestType.GET)]
		public List<Team> GetTeams(int page, int per_page)
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};

			return API.Get<List<Team>>("/teams", options);
		}

		[ApiRoute("/teams/{team_id}", RequestType.PUT)]
		public Team UpdateTeam(Team team)
		{
			throw new NotImplementedException();
			return API.Put<Team>($"/teams/{team.id}", team);
		}

		[ApiRoute("/teams/{team_id}/patch", RequestType.PUT)]
		public Team UpdateTeam(string team_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/teams/name/{name}", RequestType.GET)]
		public Team GetTeamByName(string name)
		{
			throw new NotImplementedException();
			return API.Get<Team>($"/teams/name/{name}");
		}

		[ApiRoute("/teams/search", RequestType.POST)]
		public List<Team> SearchTeams()
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/teams/{team_id}/unread", RequestType.GET)]
		public List<MessageCount> GetUnreadsFromTeam(string team_id)
		{
			throw new NotImplementedException();
			return API.Get<List<MessageCount>>($"/teams/{team_id}/unread");
		}

		[ApiRoute("/users/{user_id}/teams/unread", RequestType.GET)]
		public List<MessageCount> GetUnreadsFromAllTeams(string user_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/teams/{team_id}/invite", RequestType.POST)]
		public void InviteUserToTeam(string team_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/teams/{team_id}/stats", RequestType.GET)]
		public TeamStats GetTeamStats(string team_id)
		{
			throw new NotImplementedException();
			return API.Get<TeamStats>($"/teams/{team_id}/stats");
		}

		[ApiRoute("/teams/{team_id}/members", RequestType.GET)]
		public List<TeamMember> GetTeamMembers(string team_id, int page, int per_page)
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};

			return API.Get<List<TeamMember>>($"/teams/{team_id}/members");
		}

		[ApiRoute("/teams/{team_id}/members/ids", RequestType.POST)]
		public List<TeamMember> GetTeamMembersByIDs(string team_id, List<string> ids)
		{
			throw new NotImplementedException();
			return API.Post<List<TeamMember>>($"/teams/{team_id}/members/ids", ids);
		}

		[ApiRoute("/teams/{team_id}/members", RequestType.POST)]
		public void CreateTeamMember(string team_id, string invite_id = "", string hash = "", string data = "")
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/teams/{team_id}/members/{user_id}/roles", RequestType.POST)]
		public void UpdateTeamMemberRoles(string team_id, string user_id, string new_roles)
		{
			throw new NotImplementedException();
			var obj = new { team_id = team_id, user_id = user_id, new_roles = new_roles };
			API.Post<string>($"/teams/{team_id}/members/{user_id}/roles", obj);
		}

		[ApiRoute("/teams/name/{name}/exists", RequestType.GET)]
		public bool CheckTeamExists(string name)
		{
			throw new NotImplementedException();
		}

		// Not implemented
		// [ApiRoute("/teams/{team_id}/import", RequestType.POST)]
		#endregion

		#region Channel Methods
		[ApiRoute("/channels", RequestType.POST)]
		public Channel CreateChannel(string team_id, string name, string display_name, string type, string purpose = "", string header = "")
		{
			throw new NotImplementedException();
			var obj = new { team_id = team_id, name = name, display_name = display_name, type = type, purpose = purpose, header = header };
			return API.Post<Channel>($"/teams/{team_id}/channels/create", obj);
		}

		[ApiRoute("/teams/{team_id}/channels", RequestType.GET)]
		public List<Channel> GetChannels(string team_id, int page, int per_page)
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};

			return API.Get<List<Channel>>($"/teams/{team_id}/channels", options);
		}

		[ApiRoute("/channels/{channel_id}", RequestType.GET)]
		public ChannelInfo GetChannelByID(string channel_id)
		{
			throw new NotImplementedException();
			return API.Get<ChannelInfo>($"/channels/{channel_id}");
		}

		[ApiRoute("/teams/{team_id}/channels/name/{name}", RequestType.GET)]
		public ChannelInfo GetChannelByNameWithTeamID(string team_id, string name)
		{
			throw new NotImplementedException();
			return API.Get<ChannelInfo>($"/teams/{team_id}/channels/name/{name}");
		}

		[ApiRoute("/teams/name/{team_name}/channels/name/{channel_name}", RequestType.GET)]
		public ChannelInfo GetChannelByNameWithTeamName(string team_name, string channel_name)
		{
			throw new NotImplementedException();
			return API.Get<ChannelInfo>($"/teams/name/{team_name}/channels/name/{channel_name}");
		}

		[ApiRoute("/channels/ids", RequestType.POST)]
		public List<Channel> GetChannelsByIDs(List<string> channels)
		{
			throw new NotImplementedException();
			return API.Post<List<Channel>>($"/channels/ids", channels);
		}

		[ApiRoute("/channels/{channel_id}", RequestType.PUT)]
		public Channel UpdateChannel(Channel channel)
		{
			throw new NotImplementedException();
			return API.Post<Channel>($"/channels/{channel.id}", channel);
		}

		[ApiRoute("/channels/{channel_id}/patch", RequestType.PUT)]
		public Channel UpdateChannel(string channel_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/channels/{channel_id}", RequestType.DELETE)]
		public void DeleteChannel(string channel_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/channels/{channel_id}/unread", RequestType.GET)]
		public void GetChannelUnreads(string channel_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/channels/{channel_id}/stats", RequestType.GET)]
		public ChannelStats GetChannelStats(string channel_id)
		{
			throw new NotImplementedException();
			return API.Get<ChannelStats>($"/channels/{channel_id}/stats");
		}

		[ApiRoute("/channels/{channel_id}/members", RequestType.GET)]
		public List<ChannelMember> GetChannelMembers(string channel_id, int page, int per_page)
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};

			return API.Get<List<ChannelMember>>($"/channels/{channel_id}/members", options);
		}

		[ApiRoute("/channels/{channel_id}/members", RequestType.POST)]
		public ChannelMember CreateChannelMember(string channel_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/channels/{channel_id}/members/ids", RequestType.POST)]
		public List<ChannelMember> GetChannelMembersByIDs(string channel_id, List<string> ids)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/teams/{team_id}/channels/autocomplete", RequestType.GET)]
		public List<Channel> AutoCompleteChannels(string team_id, string term)
		{
			throw new NotImplementedException();
			return API.Get<List<Channel>>($"/teams/{team_id}/channels/autocomplete", new Dictionary<string, string>() { { "term", term } });
		}

		[ApiRoute("/channels/{channel_id}/members/{user_id}/view", RequestType.POST)]
		public void ViewChannel(string channel_id, string user_id, string prev_channel_id = "")
		{
			throw new NotImplementedException();
			var obj = new { channel_id = channel_id, prev_channel_id = prev_channel_id };
			API.Post<string>($"/channels/{channel_id}/members/{user_id}/view", obj);
		}

		[ApiRoute("/channels/{channel_id}/members/{user_id}/roles", RequestType.PUT)]
		public void UpdateChannelMemberRoles(string channel_id, string user_id, string new_roles)
		{
			throw new NotImplementedException();
			var obj = new { user_id = user_id, new_roles = new_roles };
			API.Put<string>($"/channels/{channel_id}/members/{user_id}/roles", obj);
		}

		[ApiRoute("/channels/{channel_id}/members/{user_id}", RequestType.DELETE)]
		public void DeleteChannelMember(string channel_id, string user_id)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Post Methods
		[ApiRoute("/posts", RequestType.POST)]
		public Post CreatePost(Post newPost)
		{
			throw new NotImplementedException();
			return API.Post<Post>($"/posts/", newPost);
		}

		[ApiRoute("/posts/{post_id}", RequestType.PUT)]
		public Post UpdatePost(string team_id, string channel_id, Post post)
		{
			throw new NotImplementedException();
			return API.Put<Post>($"/teams/{team_id}/channels/{channel_id}/posts/update", post);
		}

		[ApiRoute("/posts/{post_id}/patch", RequestType.PUT)]
		public Post UpdatePost(string team_id, string channel_id, string post_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/channels/{channel_id}/posts", RequestType.GET)]
		public List<Post> GetPosts(int page, int per_page, long since = 0, string before = "", string after = "")
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
			if (since != 0)
				options.Add("since", since.ToString());
			if (!string.IsNullOrWhiteSpace(before))
				options.Add("before", before);
			if (!string.IsNullOrWhiteSpace(after))
				options.Add("after", after);
		}

		[ApiRoute("/posts/{post_id}", RequestType.DELETE)]
		public void DeletePost(string post_id)
		{
			throw new NotImplementedException();
			API.Delete<string>($"/posts/{post_id}", null);
		}

		[ApiRoute("/posts/search", RequestType.POST)]
		public SearchResult SearchPosts(string terms, bool is_or_search)
		{
			throw new NotImplementedException();
			var obj = new { terms = terms, is_or_search = is_or_search };
			return API.Post<SearchResult>($"/posts/search", obj);
		}

		[ApiRoute("/posts/flagged", RequestType.GET)]
		public SearchResult GetFlaggedPosts(string in_team = "", string in_channel = "")
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>();
			if (!string.IsNullOrWhiteSpace(in_team))
				options.Add("in_team", in_team);
			if (!string.IsNullOrWhiteSpace(in_channel))
				options.Add("in_channel", in_channel);

			if (options.Count == 0)
				return API.Get<SearchResult>($"/posts/flagged");
			else
				return API.Get<SearchResult>($"/posts/flagged", options);
		}

		//Not implemented
		//[ApiRoute("/posts/{post_id}/files/info", RequestType.GET)]

		[ApiRoute("/posts/{post_id}", RequestType.GET)]
		public SearchResult GetPost(string post_id)
		{
			throw new NotImplementedException();
		}
		#endregion

		//Not implemented
		//[ApiRoute("/files", RequestType.POST)]
		//[ApiRoute("/files/{file_id}", RequestType.GET)]
		//[ApiRoute("/files/{file_id}/thumbnail", RequestType.GET)]
		//[ApiRoute("/files/{file_id}/preview", RequestType.GET)]
		//[ApiRoute("/files/{file_id}/info", RequestType.GET)]
		//[ApiRoute("/files/{file_id}/link", RequestType.GET)]

		#region Preference Methods
		[ApiRoute("/users/{user_id}/preferences/save", RequestType.POST)]
		public void UpdatePreferences(string user_id, List<Preference> preferences)
		{
			throw new NotImplementedException();
			API.Post<string>($"/users/{user_id}/preferences/save", preferences);
		}

		[ApiRoute("/users/{user_id}/preferences", RequestType.GET)]
		public List<Preference> GetPreferences(string user_id, int page, int per_page)
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
		}

		[ApiRoute("/users/{user_id}/preferences/{category}", RequestType.GET)]
		public List<Preference> GetPreferences(string user_id, string category)
		{
			throw new NotImplementedException();
			return API.Get<List<Preference>>($"/users/{user_id}/preferences/{category}");
		}

		[ApiRoute("/users/{user_id}/preferences/{category}/{name}", RequestType.GET)]
		public Preference GetPreference(string user_id, string category, string name)
		{
			throw new NotImplementedException();
			return API.Get<Preference>($"/users/{user_id}/preferences/{category}/{name}");
		}

		[ApiRoute("/users/{user_id}/preferences/{category}/{name}", RequestType.DELETE)]
		public void DeletePreference(string user_id, string category, string name)
		{
			throw new NotImplementedException();
			API.Delete<string>($"/users/{user_id}/preferences/{category}/{name}", null);
		}
		#endregion

		#region System Methods
		[ApiRoute("/system/client/config", RequestType.GET)]
		public void GetConfig()
		{
			throw new NotImplementedException();
			API.Get<string>("/system/client/config");
		}

		[ApiRoute("/system/log", RequestType.POST)]
		public void CreateSystemLogEntry(string log)
		{
			throw new NotImplementedException();
			API.Post<string>("/system/log", null);
		}

		[ApiRoute("/system/ping", RequestType.GET)]
		public void Ping()
		{
			throw new NotImplementedException();
			API.Get<string>("/system/ping");
		}
		#endregion

		#region Admin Methods
		[ApiRoute("/admin/logs", RequestType.GET)]
		public List<string> GetLogs(int page, int per_page)
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
			return API.Get<List<string>>("/admin/logs", options);
		}

		[ApiRoute("/admin/audits", RequestType.GET)]
		public List<Audit> GetAudits(int page, int per_page)
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
			return API.Get<List<Audit>>("/admin/audits", options);
		}

		[ApiRoute("/admin/config", RequestType.GET)]
		public Config GetAdminConfig()
		{
			throw new NotImplementedException();
			return API.Get<Config>("/admin/config");
		}

		[ApiRoute("/admin/config", RequestType.PUT)]
		public Config UpdateConfig(Config config)
		{
			throw new NotImplementedException();
			return API.Put<Config>("/admin/config", config);
		}

		[ApiRoute("/admin/config/reload", RequestType.POST)]
		public void ReloadConfig()
		{
			throw new NotImplementedException();
			API.Post<string>("/admin/config/reload", null);
		}

		[ApiRoute("/admin/caches/invalidate", RequestType.GET)]
		public void InvalidateCaches()
		{
			throw new NotImplementedException();
			API.Get<string>("/admin/caches/invalidate");
		}

		//Not implemented
		//[ApiRoute("/admin/email/test", RequestType.POST)]

		[ApiRoute("/admin/database/recycle", RequestType.POST)]
		public void RecycleDBConn()
		{
			throw new NotImplementedException();
			API.Post<string>("/admin/database/recycle", null);
		}

		[ApiRoute("/admin/analytics/{type}", RequestType.GET)]
		public List<Analytic> GetAnalytics(string type, string team_id = "")
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>();
			if (!string.IsNullOrWhiteSpace(team_id))
				options.Add("team_id", team_id);

			if (options.Count == 0)
				return API.Get<List<Analytic>>($"/admin/analytics/{type}");
			else
				return API.Get<List<Analytic>>($"/admin/analytics/{type}", options);
		}

		//Not implemented
		//[ApiRoute("/admin/compliance/reports", RequestType.POST)]
		//[ApiRoute("/admin/compliance/reports/{report_id}", RequestType.GET)]
		//[ApiRoute("/admin/compliance/reports", RequestType.GET)]
		//[ApiRoute("/admin/compliance/reports/{report_id}/download", RequestType.GET)]
		//[ApiRoute("/admin/brand/image", RequestType.POST)]
		//[ApiRoute("/admin/brand/image", RequestType.GET)]

		[ApiRoute("/admin/users/{user_id}/mfa/reset", RequestType.POST)]
		public void ResetUserMFA(string user_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/admin/users/{user_id}/password/reset", RequestType.POST)]
		public void ResetPassword(string user_id, string new_password)
		{
			throw new NotImplementedException();
		}

		//Not Implemented
		//[ApiRoute("/admin/ldap/sync", RequestType.POST)]
		//[ApiRoute("/admin/ldap/test", RequestType.POST)]
		//[ApiRoute("/admin/saml/metadata", RequestType.GET)]
		//[ApiRoute("/admin/saml/certificate", RequestType.POST)]
		//[ApiRoute("/admin/saml/certificate", RequestType.DELETE)]
		//[ApiRoute("/admin/saml/certificate/status", RequestType.GET)]
		//[ApiRoute("/admin/cluster/status", RequestType.GET)]
		//[ApiRoute("/admin/users/recent", RequestType.GET)]
		#endregion

		#region Command methods
		[ApiRoute("/commands", RequestType.POST)]
		public void CreateCommand()
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/commands/{command_id}", RequestType.PUT)]
		public void UpdateCommand(string command_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/teams/{team_id}/commands", RequestType.GET)]
		public void GetCommands(string team_id, bool custom_only = false)
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "custom_only", custom_only.ToString() }
			};
		}

		[ApiRoute("/commands/{command_id}/regen_token", RequestType.POST)]
		public void RegerateCommandToken(string command_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/commands/{command_id}", RequestType.DELETE)]
		public void DeleteCommand(string command_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/commands/execute", RequestType.POST)]
		public void ExecuteCommand()
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Webhook Methods
		[ApiRoute("/hooks/incoming", RequestType.POST)]
		public void CreateIncomingWebhook()
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/hooks/incoming", RequestType.GET)]
		public void GetIncomingWebhooks(int page, int per_page)
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
		}

		[ApiRoute("/hooks/incoming/{hook_id}", RequestType.PUT)]
		public void UpdateIncomingWebhook(string hook_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/hooks/incoming/{hook_id}", RequestType.DELETE)]
		public void DeleteIncomingWebhook(string hook_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/hooks/outgoing", RequestType.POST)]
		public void CreateOutgoingWebhook()
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/hooks/outgoing", RequestType.GET)]
		public void GetOutgoingWebhooks(int page, int per_page)
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
		}

		[ApiRoute("/hooks/outgoing/{hook_id}", RequestType.PUT)]
		public void UpdateOutgoingWebhook(string hook_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/hooks/outgoing/{hook_id}", RequestType.DELETE)]
		public void DeleteOutgoingWebhook(string hook_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/hooks/outgoing/{hook_id}/regen_token", RequestType.POST)]
		public void RegenerateOutgoingWebhookToken(string hook_id)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Status Methods
		[ApiRoute("/users/status/ids", RequestType.POST)]
		public void GetUserStatuses(List<string> ids)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/status", RequestType.GET)]
		public void GetUserStatus(string user_id)
		{
			throw new NotImplementedException();
		}
		
		[ApiRoute("/users/{user_id}/status", RequestType.PUT)]
		public void UpdateUserStatus(string user_id)
		{
			throw new NotImplementedException();
		}
		#endregion

		//Not implemented
		//[ApiRoute("/emoji", RequestType.POST)]
		//[ApiRoute("/emoji", RequestType.GET)]
		//[ApiRoute("/emoji/{emoji_id}/image", RequestType.GET)]
		//[ApiRoute("/emoji/{emoji_id}", RequestType.DELETE)]

		#region Reaction Methods
		[ApiRoute("/reactions", RequestType.POST)]
		public void CreateReaction()
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/posts/{post_id}/reactions/{name}", RequestType.DELETE)]
		public void DeleteReaction(string user_id, string post_id, string name)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/posts/{post_id}/reactions", RequestType.GET)]
		public void GetReactions(string post_id)
		{
			throw new NotImplementedException();
		}
		#endregion

		//Not implemented
		//[ApiRoute("/oauth/apps", RequestType.POST)]
		//[ApiRoute("/oauth/apps", RequestType.GET)]
		//[ApiRoute("/oauth/apps/{client_id}", RequestType.GET)]
		//[ApiRoute("/oauth/apps/{client_id}", RequestType.DELETE)]
		//[ApiRoute("/oauth/apps/{client_id}/regen_secret", RequestType.POST)]
		//[ApiRoute("/users/{user_id}/oauth/apps/{client_id}/authorize", RequestType.POST)]
		//[ApiRoute("/users/{user_id}/oauth/apps/authorized", RequestType.GET)]
		//[ApiRoute("/users/{user_id}/oauth/apps/{client_id}/deauthorize", RequestType.POST)]
		//Use Connect() instead: [ApiRoute("/websocket"), RequestType.GET)]
		//[ApiRoute("/webrtc/token", RequestType.GET)]
		//[ApiRoute("/license", RequestType.POST)]
		//[ApiRoute("/license", RequestType.DELETE)]
		//[ApiRoute("/license/settings", RequestType.GET)]
	}
}
