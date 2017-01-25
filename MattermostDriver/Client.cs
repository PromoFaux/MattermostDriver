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
			string rawdata = API.PostGetAuth(new { login_id = username, password = password });

			//Connect to Websocket
			socket = new WebSocket(websocket);
			socket.Opened += OnWebsocketOpen;
			socket.MessageReceived += OnWebsocketMessage;
			socket.Closed += OnWebsocketClose;
			socket.Open();
			seq = 1;

			//Return Self information
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<Self>(rawdata);
			else
				return null;
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
			string rawdata = API.Post($"/users", obj);
			return rawdata.Deserialize<User>();
		}

		[ApiRoute("/users/{user_id}", RequestType.PUT)]
		public User UpdateUser(User user)
		{
			throw new NotImplementedException();
			string rawdata = API.Put($"/users/{user.id}", user);
			return rawdata.Deserialize<User>();
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
				API.Put($"/users/{user_id}/roles", obj);
			}
			else
			{
				var obj = new { user_id = user_id, new_roles = new_roles };
				API.Put($"/users/{user_id}/roles", obj);
			}
		}

		[ApiRoute("/users/{user_id}/password", RequestType.PUT)]
		public void UpdatePassword(string user_id, string current_password, string new_password)
		{
			throw new NotImplementedException();
			var obj = new { user_id = user_id, current_password = current_password, new_password = new_password };
			API.Put($"/users/{user_id}/password", obj);
		}

		[ApiRoute("/users/{user_id}/password/reset", RequestType.POST)]
		public void ResetPassword(string user_id, string new_password)
		{
			throw new NotImplementedException();
			var obj = new { new_password = new_password };
			API.Post($"/users/{user_id}/password/reset", obj);
		}

		[ApiRoute("/users/{user_id}/password/reset/send", RequestType.POST)]
		public void SendResetPassword(string user_id, string email)
		{
			throw new NotImplementedException();
			var obj = new { email = email };
			API.Post($"/users/{user_id}/password/reset/send", obj);
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

			return API.Get("/users", options).Deserialize<List<User>>();
		}

		[ApiRoute("/users/{user_id}", RequestType.GET)]
		public User GetUserByID(string user_id)
		{
			throw new NotImplementedException();
			return API.Get($"/users/{user_id}").Deserialize<User>();
		}

		[ApiRoute("/users/username/{username}", RequestType.GET)]
		public User GetUserByName(string username)
		{
			throw new NotImplementedException();
			return API.Get($"/users/username/{username}").Deserialize<User>();
		}

		[ApiRoute("/users/email/{email}", RequestType.GET)]
		public User GetUserByEmail(string email)
		{
			throw new NotImplementedException();
			return API.Get($"/users/email/{email}").Deserialize<User>();
		}

		[ApiRoute("/users/ids", RequestType.POST)]
		public Dictionary<string, User> GetUsersByIDs(List<string> ids)
		{
			throw new NotImplementedException();
			return API.Post($"/users/ids", ids).Deserialize<Dictionary<string, User>>();
		}

		[ApiRoute("/users/search", RequestType.POST)]
		public List<User> SearchUsers(string term, string team_id = "", string in_channel_id = "", string not_in_channel_id = "", bool allow_inactive = false)
		{
			throw new NotImplementedException();
			var obj = new { term = term, team_id = team_id, in_channel_id = in_channel_id, not_in_channel_id = not_in_channel_id, allow_inactive = allow_inactive };
			return API.Post($"/users/search", obj).Deserialize<List<User>>();
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

			return API.Get($"/users/autocomplete", options).Deserialize<List<User>>();
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

		[ApiRoute("/users/login", RequestType.POST)]
		public Self Login(string login_id, string password, string token = "", string device_id = "")
		{
			throw new NotImplementedException();
			var obj = new { login_id = login_id, password = password, token = token, device_id = device_id };
			return API.PostGetAuth(obj).Deserialize<Self>();
		}

		[ApiRoute("/users/logout", RequestType.POST)]
		public void Logout()
		{
			throw new NotImplementedException();
			API.Post($"/users/logout", null);
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
			string rawdata = API.Get("/users/{user_id}/audits");
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<List<Audit>>(rawdata);
			else
				return null;
		}

		[ApiRoute("/users/{user_id}/device", RequestType.PUT)]
		public void UpdateDeviceID(string user_id)
		{
			throw new NotImplementedException();
		}

		[ApiRoute("/users/{user_id}/mfa", RequestType.GET)]
		public bool IsMFAActive(string user_id)
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

		//#region User Methods

		//[ApiRoute("/users/me", RequestType.GET)]
		//public Self Me()
		//{
		//	string rawdata = API.Get($"/users/me");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Self>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/users/update_active", RequestType.POST)]
		//public User UpdateActive(string user_id, bool active)
		//{
		//	var obj = new { user_id = user_id, active = active };
		//	string rawdata = API.Post($"/users/update_active", obj);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<User>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/users/update_notify", RequestType.POST)]
		//public void UpdateNotify(string user_id, string comments, string desktop, string desktop_sound, string email, string channel = "", string desktop_duration = "", string first_name = "", string mention_keys = "", string push = "", string push_status = "")
		//{
		//	var obj = new { user_id = user_id, comments = comments, desktop = desktop, desktop_sound = desktop_sound, email = email, channel = channel, desktop_duration = desktop_duration, first_name = first_name, mention_keys = mention_keys, push = push, push_status = push_status };
		//	API.Post($"/users/update_notify", obj);
		//}

		//#region Team Methods
		//[ApiRoute("/teams/create", RequestType.POST)]
		//public Team CreateTeam(string name, string display_name, string type)
		//{
		//	var obj = new { name = name, display_name = display_name, type = type };
		//	string rawdata = API.Post($"/teams/create", obj);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Team>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/all", RequestType.GET)]
		//public Dictionary<string,Team> GetAllTeams()
		//{
		//	string rawdata = API.Get("/teams/all");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Dictionary<string, Team>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/members", RequestType.GET)]
		//public List<TeamMember> GetAllTeamsAsMember()
		//{
		//	string rawdata = API.Get("/teams/members");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<TeamMember>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/unread", RequestType.GET)]
		//public List<MessageCount> GetUnreadsFromTeam(string id)
		//{
		//	string rawdata = API.Get("/teams/unread", new Dictionary<string, string>() { { "id", id } });
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<MessageCount>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/members/{offset}/{limit}", RequestType.GET)]
		//public List<TeamMember> GetTeamMembers(string team_id, int offset, int limit)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/members/{offset}/{limit}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<TeamMember>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/members/{user_id}", RequestType.GET)]
		//public TeamMember GetTeamMember(string team_id, string user_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/members/{user_id}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<TeamMember>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/members/ids", RequestType.POST)]
		//public List<TeamMember> GetTeamMembersByIDs(string team_id, List<string> ids)
		//{
		//	string rawdata = API.Post($"/teams/{team_id}/members/ids", ids);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<TeamMember>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/me", RequestType.GET)]
		//public Team GetTeamByID(string team_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/me");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Team>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/name/{team_name}", RequestType.GET)]
		//public Team GetTeamByName(string team_name)
		//{
		//	string rawdata = API.Get($"/teams/name/{team_name}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Team>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/update", RequestType.POST)]
		//public Team UpdateTeam(string team_id, Team team)
		//{
		//	string rawdata = API.Post($"/teams/{team_id}/update", team);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Team>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/stats", RequestType.GET)]
		//public TeamStats GetTeamStats(string team_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/stats");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<TeamStats>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("teams/{team_id}/add_user_to_team", RequestType.POST)]
		//public void AddUserToTeam(string team_id, string user_id)
		//{
		//	var obj = new { user_id = user_id };
		//	API.Post($"teams/{team_id}/add_user_to_team", obj);
		//}

		//[ApiRoute("/teams/{team_id}/remove_user_from_team", RequestType.POST)]
		//public void RemoveUserFromTeam(string team_id, string user_id)
		//{
		//	var obj = new { user_id = user_id };
		//	API.Post($"/teams/{team_id}/remove_user_from_team", obj);
		//}

		//[ApiRoute("/teams/all_team_listings", RequestType.GET)]
		//public Dictionary<string,Team> GetAllAvailableTeams()
		//{
		//	string rawdata = API.Get($"/teams/all_team_listings");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Dictionary<string, Team>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/update_member_roles", RequestType.POST)]
		//public void UpdateTeamMemberRoles(string team_id, string user_id, string new_roles)
		//{
		//	var obj = new { team_id = team_id, user_id = user_id, new_roles = new_roles };
		//	API.Post($"/teams/{team_id}/update_member_roles", obj);
		//}
		//#endregion

		//#region Channel Methods

		//[ApiRoute("/teams/{team_id}/channels/create", RequestType.POST)]
		//public Channel CreateChannel(string team_id, string name, string display_name, string type, string purpose = "", string header = "")
		//{
		//	var obj = new { team_id = team_id, name = name, display_name = display_name, type = type, purpose = purpose, header = header };
		//	string rawdata = API.Post($"/teams/{team_id}/channels/create", obj);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Channel>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/update", RequestType.POST)]
		//public Channel UpdateChannel(string team_id, Channel channel)
		//{
		//	string rawdata = API.Post($"/teams/{team_id}/channels/update", channel);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Channel>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/view", RequestType.POST)]
		//public void ViewChannel(string team_id, string channel_id, string prev_channel_id = "")
		//{
		//	var obj = new { team_id = team_id, channel_id = channel_id, prev_channel_id = prev_channel_id };
		//	API.Post($"/teams/{team_id}/channels/view", obj);
		//}

		//[ApiRoute("/teams/{team_id}/channels/", RequestType.GET)]
		//public List<Channel> GetJoinedChannels(string team_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<Channel>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/name/{channel_name}", RequestType.GET)]
		//public Channel GetChannelByName(string team_id, string channel_name)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/name/{channel_name}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Channel>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/more/{offset}/{limit}", RequestType.GET)]
		//public List<Channel> GetChannelsNotJOined(string team_id, int offset, int limit)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/more/{offset}/{limit}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<Channel>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/members", RequestType.GET)]
		//public List<ChannelMember> GetChannelMembers(string team_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/members");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<ChannelMember>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/", RequestType.GET)]
		//public ChannelInfo GetChannelByID(string team_id, string channel_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/{channel_id}/");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<ChannelInfo>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/counts", RequestType.GET)]
		//public ChannelCounts GetChannelCounts(string team_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/counts");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<ChannelCounts>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/stats", RequestType.GET)]
		//public ChannelStats GetChannelStats(string team_id, string channel_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/{channel_id}/stats");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<ChannelStats>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/delete", RequestType.POST)]
		//public void DeleteChannel(string team_id, string channel_id)
		//{
		//	API.Post($"/teams/{team_id}/channels/{channel_id}/delete", null);
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/add", RequestType.POST)]
		//public void AddUserToChannel(string team_id, string channel_id, string user_id)
		//{
		//	var obj = new { user_id = user_id };
		//	API.Post($"/teams/{team_id}/channels/{channel_id}/add", obj);
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/members/{user_id}", RequestType.GET)]
		//public ChannelMember GetChannelMember(string team_id, string channel_id, string user_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/{channel_id}/members/{user_id}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<ChannelMember>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/members/ids", RequestType.POST)]
		//public List<ChannelMember> GetChannelMembers(string team_id, string channel_id, List<string> ids)
		//{
		//	string rawdata = API.Post($"/teams/{team_id}/channels/{channel_id}/members/ids", ids);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<ChannelMember>>(rawdata);
		//	else
		//		return null;
		//}

		//// -- Doesn't exist in 3.6.1, will return in 3.7
		////[ApiRoute("/teams/{team_id}/channels/{channel_id}/update_member_roles", RequestType.POST)]
		////public void UpdateChannelMemberRoles(string team_id, string channel_id, string user_id, string new_roles)
		////{
		////	var obj = new { user_id = user_id, new_roles = new_roles };
		////	API.Post($"/teams/{team_id}/channels/{channel_id}/update_member_roles", obj);
		////}

		//[ApiRoute("/teams/{team_id}/channels/autocomplete", RequestType.GET)]
		//public List<Channel> AutoCompleteChannels(string team_id, string term)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/autocomplete", new Dictionary<string, string>() { { "term", term } });
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<Channel>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/more/search", RequestType.POST)]
		//public List<Channel> SearchChannels(string team_id, string term)
		//{
		//	var obj = new { term = term };
		//	string rawdata = API.Post($"/teams/{team_id}/channels/more/search", obj);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<Channel>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/create_direct", RequestType.POST)]
		//public Channel CreateDirectChannel(string team_id, string user_id)
		//{
		//	var obj = new { user_id = user_id };
		//	string rawdata = API.Post($"/teams/{team_id}/channels/create_direct", obj);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Channel>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/update_header", RequestType.POST)]
		//public Channel UpdateChannelHeader(string team_id, string channel_id, string channel_header)
		//{
		//	var obj = new { channel_id = channel_id, channel_header = channel_header };
		//	string rawdata = API.Post($"/teams/{team_id}/channels/update_header", obj);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Channel>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/update_purpose", RequestType.POST)]
		//public Channel UpdateChannelPurpose(string team_id, string channel_id, string channel_purpose)
		//{
		//	var obj = new { channel_id = channel_id, channel_purpose = channel_purpose };
		//	string rawdata = API.Post($"/teams/{team_id}/channels/update_purpose", obj);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Channel>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/update_notify_props", RequestType.POST)]
		//public ChannelMember.ChannelNotifProps UpdateChannelNotifyProps(string team_id, string channel_id, string user_id, string mark_unread, string desktop)
		//{
		//	var obj = new { channel_id = channel_id, user_id = user_id, mark_unread = mark_unread, desktop = desktop };
		//	string rawdata = API.Post($"/teams/{team_id}/channels/update_notify_props", obj);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<ChannelMember.ChannelNotifProps>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/join", RequestType.POST)]
		//public Channel JoinChannelByID(string team_id, string channel_id)
		//{
		//	string rawdata = API.Post($"/teams/{team_id}/channels/{channel_id}/join", null);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Channel>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/name/{channel_name}/join", RequestType.POST)]
		//public Channel JoinChannelByName(string team_id, string channel_name)
		//{
		//	string rawdata = API.Post($"/teams/{team_id}/channels/name/{channel_name}/join", null);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Channel>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/leave", RequestType.POST)]
		//public void LeaveChannel(string team_id, string channel_id)
		//{
		//	var obj = new { channel_id = channel_id };
		//	API.Post($"/teams/{team_id}/channels/{channel_id}/leave", obj);
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/remove", RequestType.POST)]
		//public void RemoveMemberFromChannel(string team_id, string channel_id, string user_id)
		//{
		//	var obj = new { user_id = user_id };
		//	API.Post($"/teams/{team_id}/channels/{channel_id}/remove", obj);
		//}
		//#endregion

		//#region Post Methods

		//[ApiRoute("/teams/{team_id}/posts/search", RequestType.POST)]
		//public SearchResult SearchPosts(string team_id, string terms, bool is_or_search)
		//{
		//	var obj = new { terms = terms, is_or_search = is_or_search };
		//	string rawdata = API.Post($"/teams/{team_id}/posts/search", obj);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<SearchResult>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/posts/flagged/{offset}/{limit}", RequestType.GET)]
		//public SearchResult GetFlaggedPosts(string team_id, int offset, int limit)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/posts/flagged/{offset}/{limit}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<SearchResult>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/create", RequestType.POST)]
		//public Post CreatePost(string team_id, string channel_id, Post newPost)
		//{
		//	string rawdata = API.Post($"/teams/{team_id}/channels/{channel_id}/posts/create", newPost);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Post>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/update", RequestType.POST)]
		//public Post UpdatePost(string team_id, string channel_id, Post post)
		//{
		//	string rawdata = API.Post($"/teams/{team_id}/channels/{channel_id}/posts/update", post);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Post>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/page/{offset}/{limit}", RequestType.GET)]
		//public SearchResult GetPosts(string team_id, string channel_id, int offset, int limit)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/{channel_id}/posts/page/{offset}/{limit}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<SearchResult>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/since/{time}", RequestType.GET)]
		//public SearchResult GetPostsSince(string team_id, string channel_id, long time)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/{channel_id}/posts/since/{time}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<SearchResult>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/posts/{post_id}", RequestType.GET)]
		//public SearchResult GetPost(string team_id, string post_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/posts/{post_id}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<SearchResult>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/delete", RequestType.POST)]
		//public void DeletePost(string team_id, string channel_id, string post_id)
		//{
		//	API.Post($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/delete", null);
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/before/{offset}/{limit}", RequestType.GET)]
		//public SearchResult GetPostsBeforePost(string team_id, string channel_id, string post_id, int offset, int limit)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/before/{offset}/{limit}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<SearchResult>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/after/{offset}/{limit}", RequestType.GET)]
		//public SearchResult GetPostsAfterPost(string team_id, string channel_id, string post_id, int offset, int limit)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/after/{offset}/{limit}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<SearchResult>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions", RequestType.GET)]
		//public List<Reaction> GetPostReactions(string team_id, string channel_id, string post_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<Reaction>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions/save", RequestType.POST)]
		//public Reaction CreateReaction(string team_id, string channel_id, string user_id, string post_id, string emoji_name)
		//{
		//	string rawdata = API.Post($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions/save", new Reaction() { emoji_name = emoji_name, post_id = post_id, user_id = user_id });
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Reaction>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions/delete", RequestType.POST)]
		//public Reaction DeleteReaction(string team_id, string channel_id, string user_id, string post_id, string emoji_name)
		//{
		//	string rawdata = API.Post($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions/delete", new Reaction() { emoji_name = emoji_name, post_id = post_id, user_id = user_id });
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Reaction>(rawdata);
		//	else
		//		return null;
		//}
		//#endregion

		//#region Preference Methods
		//[ApiRoute("/preferences/save", RequestType.POST)]
		//public void UpdatePreferences(List<Preference> preferences)
		//{
		//	API.Post($"/preferences/save", preferences);
		//}

		//[ApiRoute("/preferences/delete", RequestType.POST)]
		//public void DeletePreferences(List<Preference> preferences)
		//{
		//	API.Post($"/preferences/delete", preferences);
		//}

		//[ApiRoute("/preferences/{category}", RequestType.GET)]
		//public List<Preference> GetPreferences(string category)
		//{
		//	string rawdata = API.Get($"/preferences/{category}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<Preference>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/preferences/{category}/{name}", RequestType.GET)]
		//public Preference GetPreference(string category, string name)
		//{
		//	string rawdata = API.Get($"/preferences/{category}/{name}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Preference>(rawdata);
		//	else
		//		return null;
		//}
		//#endregion

		//#region Incoming Webhook Methods
		//[ApiRoute("/teams/{team_id}/hooks/incoming/list", RequestType.GET)]
		//public List<IncomingWebook> GetIncomingWebhooks(string team_id)
		//{
		//	string rawdata = API.Get($"/teams/{team_id}/hooks/incoming/list");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<IncomingWebook>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/hooks/incoming/create", RequestType.POST)]
		//public IncomingWebook CreateIncomingWebhook(string team_id, string channel_id, string display_name = "", string description = "")
		//{
		//	var obj = new { channel_id = channel_id, display_name = display_name, description = description };
		//	string rawdata = API.Post($"/teams/{team_id}/hooks/incoming/create", obj);
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<IncomingWebook>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/teams/{team_id}/hooks/incoming/delete", RequestType.POST)]
		//public void DeleteIncomingWebhook(string team_id, string id)
		//{
		//	var obj = new { id = id };
		//	API.Post($"/teams/{team_id}/hooks/incoming/delete", obj);
		//}
		//#endregion

		//#region Admin Methods
		//[ApiRoute("/admin/logs", RequestType.GET)]
		//public List<string> GetLogs()
		//{
		//	string rawdata = API.Get("/admin/logs");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<string>>(rawdata);
		//	else
		//		return null;
		//}



		//[ApiRoute("/admin/config", RequestType.GET)]
		//public Config GetConfig()
		//{
		//	string rawdata = API.Get("/admin/config");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Config>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/admin/save_config", RequestType.POST)]
		//public void SaveConfig(Config config)
		//{
		//	API.Post("/admin/save_config", config);
		//}

		//[ApiRoute("/admin/reload_config", RequestType.GET)]
		//public void ReloadConfig()
		//{
		//	API.Get("/admin/reload_config");
		//}

		//[ApiRoute("/admin/invalidate_all_caches", RequestType.GET)]
		//public void InvalidateAllCaches()
		//{
		//	API.Get("/admin/invalidate_all_caches");
		//}

		//[ApiRoute("/admin/recycle_db_conn", RequestType.GET)]
		//public void RecycleDBConn()
		//{
		//	API.Get("/admin/recycle_db_conn");
		//}

		//[ApiRoute("/admin/analytics/{id}/{name}", RequestType.GET)]
		//public List<Analytic> GetAnalyticsByTeam(string team_id, string name)
		//{
		//	string rawdata = API.Get($"/admin/analytics/{team_id}/{name}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<Analytic>>(rawdata);
		//	else
		//		return null;
		//}
		//[ApiRoute("/admin/analytics/{name}", RequestType.GET)]
		//public List<Analytic> GetAnalytics(string name)
		//{
		//	string rawdata = API.Get($"/admin/analytics/{name}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<List<Analytic>>(rawdata);
		//	else
		//		return null;
		//}

		//[ApiRoute("/admin/reset_mfa", RequestType.POST)]
		//public void ResetMFA(string user_id)
		//{
		//	var obj = new { user_id = user_id };
		//	API.Post("/admin/reset_mfa", obj);
		//}

		//[ApiRoute("/admin/recently_active_users/{team_id}", RequestType.GET)]
		//public Dictionary<string,User> GetRecentlyActiveUsers(string team_id)
		//{
		//	string rawdata = API.Get($"/admin/recently_active_users/{team_id}");
		//	if (!string.IsNullOrWhiteSpace(rawdata))
		//		return JsonConvert.DeserializeObject<Dictionary<string, User>>(rawdata);
		//	else
		//		return null;
		//}
		//#endregion
	}
}
