using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using WebSocket4Net;

namespace MattermostDriver
{
	public class Client
	{
		private ILogger logger { get; set; }
		private WebSocket socket { get; set; }
		private int seq { get; set; }
		private bool awaiting_ok { get; set; }
		private string ApiBase { get; set; }
		private string WebsocketUrl { get; set; }
		private RestClient client { get; set; }
		private string token { get; set; }

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

		public Client(string url, ILogger logger)
		{
			//Setup logging
			this.logger = logger;

			//Remove last '/' for consistency
			if (url.EndsWith("/"))
				url = url.TrimEnd('/');

			//Generate API base url
			ApiBase = url + "/api/v4";

			//Generate websocket url
			if (url.StartsWith("https"))
				WebsocketUrl = "wss" + url.Substring(5) + "/api/v4/users/websocket";
			else if (url.StartsWith("http"))
				WebsocketUrl = "ws" + url.Substring(4) + "/api/v4/users/websocket";
			else
			{
				logger.Error($"Invalid server URL in Client.ctor(): {url}");
				throw new Exception("Invalid server URL.");
			}
			socket = new WebSocket(WebsocketUrl);
			socket.Opened += OnWebsocketOpen;
			socket.MessageReceived += OnWebsocketMessage;
			socket.Closed += OnWebsocketClose;

			//Setup REST client
			client = new RestClient(ApiBase);
		}

		#region API
		private Self APIPostGetAuth(object jsonbody)
		{
			RestRequest request = new RestRequest("/users/login", Method.POST);
			request.AddHeader("Content-Type", "application/json");
			request.AddJsonBody(jsonbody);
			var result = client.Execute(request);

			logger.Debug($"Executed Post at endpoint '/users/login'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.status_code != 0)
				{
					logger.Error("Error received from API: " + error.ToString());
					return null;
				}
			}
			catch { }

			logger.Debug($"Result: " + result.Content);

			token = result.Headers[1].Value.ToString();

			return JsonConvert.DeserializeObject<Self>(result.Content);
		}

		private T APIPost<T>(string endpoint, object jsonbody)
		{
			RestRequest request = new RestRequest(endpoint, Method.POST);
			request.AddHeader("Content-Type", "application/json");
			if (!string.IsNullOrWhiteSpace(token))
				request.AddHeader("Authorization", "Bearer " + token);
			if (jsonbody != null)
				request.AddJsonBody(jsonbody);
			var result = client.Execute(request);

			logger.Debug($"Executed Post at endpoint '{endpoint}'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.status_code != 0)
				{
					logger.Error("Error received from API: " + error.ToString());
					return default(T);
				}
			}
			catch { }

			logger.Debug($"Result: " + result.Content);

			try
			{
				return JsonConvert.DeserializeObject<T>(result.Content);
			}
			catch
			{
				logger.Error("Error deserializing result.");
				return default(T);
			}
		}

		private T APIGet<T>(string endpoint, Dictionary<string, string> parameters = null)
		{
			RestRequest request = new RestRequest(endpoint, Method.GET);
			if (!string.IsNullOrWhiteSpace(token))
				request.AddHeader("Authorization", "Bearer " + token);
			if (parameters != null)
			{
				foreach (KeyValuePair<string, string> kvp in parameters)
				{
					request.AddParameter(kvp.Key, kvp.Value);
				}
			}
			var result = client.Execute(request);

			logger.Debug($"Executed Get at endpoint '{endpoint}'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.status_code != 0)
				{
					logger.Error("Error received from API: " + error.ToString());
					return default(T);
				}
			}
			catch { }

			logger.Debug($"Result: " + result.Content);

			try
			{
				return JsonConvert.DeserializeObject<T>(result.Content);
			}
			catch
			{
				logger.Error("Error deserializing result.");
				return default(T);
			}
		}

		private T APIPut<T>(string endpoint, object jsonbody)
		{
			RestRequest request = new RestRequest(endpoint, Method.PUT);

			request.AddHeader("Content-Type", "application/json");
			if (!string.IsNullOrWhiteSpace(token))
				request.AddHeader("Authorization", "Bearer " + token);
			if (jsonbody != null)
				request.AddJsonBody(jsonbody);
			var result = client.Execute(request);

			logger.Debug($"Executed Put at endpoint '{endpoint}'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.status_code != 0)
				{
					logger.Error("Error received from API: " + error.ToString());
					return default(T);
				}
			}
			catch { }

			logger.Debug($"Result: " + result.Content);

			try
			{
				return JsonConvert.DeserializeObject<T>(result.Content);
			}
			catch
			{
				logger.Error("Error deserializing result.");
				return default(T);
			}
		}

		private void APIDelete(string endpoint)
		{
			RestRequest request = new RestRequest(endpoint, Method.DELETE);

			request.AddHeader("Content-Type", "application/json");
			if (!string.IsNullOrWhiteSpace(token))
				request.AddHeader("Authorization", "Bearer " + token);
			var result = client.Execute(request);

			logger.Debug($"Executed Delete at endpoint '{endpoint}'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.status_code != 0)
				{
					logger.Error("Error received from API: " + error.ToString());
					return;
				}
			}
			catch { }

			logger.Debug($"Result: " + result.Content);

			return;
		}
		#endregion

		public void Connect()
		{
			if (socket.State == WebSocketState.Open)
			{
				logger.Error("Cannot connect to websocket while a connection is already made!");
				return;
			}
			if (string.IsNullOrWhiteSpace(token))
			{
				logger.Error("Cannot connect to websocket without authenticating!");
				return;
			}
			socket.Open();
			seq = 1;
		}

		#region Websocket Handlers
		private void OnWebsocketOpen(object sender, EventArgs e)
		{
			WebsocketConnected?.Invoke();
			logger.Debug("Websocket-open event thrown. Sending authentication challenge.");

			//Authenticate over Websocket
			var request = new { seq = ++seq, action = "authentication_challenge", data = new { token = token } };
			awaiting_ok = true;
			socket.Send(JsonConvert.SerializeObject(request));
		}

		private void OnWebsocketMessage(object sender, MessageReceivedEventArgs e)
		{
			string rawdata = e.Message;

			//Specially handle Auth 'OK' message
			if (awaiting_ok)
			{
				var res = JsonConvert.DeserializeAnonymousType(rawdata, new { status = "", seq_reply = 0 });

				if (res.status != "OK")
					logger.Warn("OK not received via websocket. Full message: " + rawdata);
				else if (res.seq_reply == 2)
					logger.Debug("Authentication challenge successful. Awaiting hello event.");
				else
					logger.Debug($"[{res.seq_reply.ToString()}] Reply OK.");

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

		public void SendUserTyping(string channel_id, string parent_id = "")
		{
			var request = new { seq = ++seq, action = "user_typing", data = new { channel_id = channel_id, parent_id = parent_id } };
			awaiting_ok = true;
			socket.Send(JsonConvert.SerializeObject(request));
		}
		#endregion

		#region User Methods
		// CreateUser creates a user in the system based on the provided user struct.
		//TODO: add hash/inviteID options?
		[ApiRoute("/users", RequestType.POST)]
		public User CreateUser(string email, string username, string password, string first_name = "", string last_name = "", string nickname = "", string locale = "")
		{
			var obj = new { email = email, username = username, password = password, first_name = first_name, last_name = last_name, nickname = nickname, locale = locale };
			return APIPost<User>($"/users", obj);
		}

		// UpdateUser updates a user in the system based on the provided user struct.
		[ApiRoute("/users/{user_id}", RequestType.PUT)]
		public User UpdateUser(User user)
		{
			return APIPut<User>($"/users/{user.id}", user);
		}

		// PatchUser partially updates a user in the system. Any missing fields are not updated.
		[ApiRoute("/users/{user_id}/patch", RequestType.PUT)]
		public User UpdateUser(string user_id, string username = "", string nickname = "", string first_name = "", string last_name = "", string position = "", string email = "", string locale = "")
		{
			var obj = new { username = username, nickname = nickname, first_name = first_name, last_name = last_name, position = position, email = email, locale = locale };
			return APIPut<User>($"/users/{user_id}/patch", obj);
		}

		// UpdateUserRoles updates a user's roles in the system. A user can have "system_user" and "system_admin" roles.
		[ApiRoute("/users/{user_id}/roles", RequestType.PUT)]
		public void UpdateUserRoles(string user_id, string roles)
		{
			var obj = new { user_id = user_id, roles = roles };
			APIPut<StatusOK>($"/users/{user_id}/roles", obj);
		}

		// UpdateUserPassword updates a user's password. Must be logged in as the user or be a system administrator.
		[ApiRoute("/users/{user_id}/password", RequestType.PUT)]
		public void UpdatePassword(string user_id, string new_password, string current_password = "")
		{
			var obj = new { user_id = user_id, current_password = current_password, new_password = new_password };
			APIPut<StatusOK>($"/users/{user_id}/password", obj);
		}

		// ResetPassword uses a recovery code to update reset a user's password.
		[ApiRoute("/users/{user_id}/password/reset", RequestType.POST)]
		public void ResetMyPassword(string user_id, string new_password, string code)
		{
			var obj = new { new_password = new_password, code = code };
			APIPost<StatusOK>($"/users/{user_id}/password/reset", obj);
		}

		// SendPasswordResetEmail will send a link for password resetting to a user with the
		// provided email.
		[ApiRoute("/users/{user_id}/password/reset/send", RequestType.POST)]
		public void SendResetPassword(string user_id, string email)
		{
			var obj = new { email = email };
			APIPost<StatusOK>($"/users/{user_id}/password/reset/send", obj);
		}

		// GetUsers returns a page of users on the system. Page counting starts at 0.
		[ApiRoute("/users", RequestType.GET)]
		public List<User> GetUsers(int page, int per_page, string in_team = "", string in_channel = "", string not_in_channel = "")
		{
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};

			if (!string.IsNullOrWhiteSpace(not_in_channel))
				options.Add("not_in_channel", not_in_channel);
			else if (!string.IsNullOrWhiteSpace(in_team))
				options.Add("in_team", in_team);
			else if (!string.IsNullOrWhiteSpace(in_channel))
				options.Add("in_channel", in_channel);

			return APIGet<List<User>>("/users", options);
		}

		// GetUser returns a user based on the provided user id string.
		[ApiRoute("/users/{user_id}", RequestType.GET)]
		public User GetUser(string user_id)
		{
			return APIGet<User>($"/users/{user_id}");
		}

		// GetUserByUsername returns a user based on the provided user name string.
		[ApiRoute("/users/username/{username}", RequestType.GET)]
		public User GetUserByUsername(string username)
		{
			return APIGet<User>($"/users/username/{username}");
		}

		// GetUserByEmail returns a user based on the provided user email string.
		[ApiRoute("/users/email/{email}", RequestType.GET)]
		public User GetUserByEmail(string email)
		{
			return APIGet<User>($"/users/email/{email}");
		}

		// GetUsersByIds returns a list of users based on the provided user ids.
		[ApiRoute("/users/ids", RequestType.POST)]
		public List<User> GetUsersByIDs(List<string> ids)
		{
			return APIPost<List<User>>($"/users/ids", ids);
		}

		[ApiRoute("/users/search", RequestType.POST)]
		public List<User> SearchUsers(string term, string team_id = "", string in_channel_id = "", string not_in_channel_id = "", bool allow_inactive = false)
		{
			throw new NotImplementedException();
			var obj = new { term = term, team_id = team_id, in_channel_id = in_channel_id, not_in_channel_id = not_in_channel_id, allow_inactive = allow_inactive };
			return APIPost<List<User>>($"/users/search", obj);
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

			return APIGet<List<User>>($"/users/autocomplete", options);
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

		// Login authenticates a user by login id, which can be username, email or some sort
		// of SSO identifier based on server configuration, and a password.
		[ApiRoute("/users/login", RequestType.POST)]
		public Self Login(string login_id, string password)
		{
			var obj = new { login_id = login_id, password = password};
			return APIPostGetAuth(obj);
		}

		// LoginById authenticates a user by user id and password.
		[ApiRoute("/users/login", RequestType.POST)]
		public Self LoginByID(string id, string password)
		{
			var obj = new { id = id, password = password };
			return APIPostGetAuth(obj);
		}

		// LoginByLdap authenticates a user by LDAP id and password.
		[ApiRoute("/users/login", RequestType.POST)]
		public Self LoginByLdap(string login_id, string password)
		{
			var obj = new { login_id = login_id, password = password, ldap_only = true };
			return APIPostGetAuth(obj);
		}

		// LoginWithDevice authenticates a user by login id (username, email or some sort
		// of SSO identifier based on configuration), password and attaches a device id to
		// the session.
		[ApiRoute("/users/login", RequestType.POST)]
		public Self LoginWithDevice(string login_id, string password, string device_id = "")
		{
			var obj = new { login_id = login_id, password = password, device_id = device_id};
			return APIPostGetAuth(obj);
		}

		// Logout terminates the current user's session.
		[ApiRoute("/users/logout", RequestType.POST)]
		public void Logout()
		{
			token = "";
			APIPost<StatusOK>($"/users/logout", null);
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
			return APIGet<List<Audit>>("/users/{user_id}/audits");
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

		// DeleteUser deactivates a user in the system based on the provided user id string.
		[ApiRoute("/users/{user_id}", RequestType.DELETE)]
		public void DeleteUser(string user_id)
		{
			APIDelete($"/users/{user_id}");
		}
		#endregion

		#region Team Methods
		// CreateTeam creates a team in the system based on the provided team struct.
		[ApiRoute("/teams", RequestType.POST)]
        public Team CreateTeam(Team team)
        {
            return APIPost<Team>($"/teams", team);
        }
		//public Team CreateTeam(string name, string display_name, string type)
		//{
		//	throw new NotImplementedException();
		//	var obj = new { name = name, display_name = display_name, type = type };
		//	return Post<Team>($"/teams", obj);
		//}

		[ApiRoute("/teams", RequestType.GET)]
		public List<Team> GetTeams(int page, int per_page)
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};

			return APIGet<List<Team>>("/teams", options);
		}

		// GetTeamsForUser returns a list of teams a user is on. Must be logged in as the user
		// or be a system administrator.
		[ApiRoute("/users/{user_id}/teams", RequestType.GET)]
		public List<Team> GetTeamsForUser(string user_id)
		{
			return APIGet<List<Team>>($"/users/{user_id}/teams");
		}

		[ApiRoute("/teams/{team_id}", RequestType.PUT)]
		public Team UpdateTeam(Team team)
		{
			throw new NotImplementedException();
			return APIPut<Team>($"/teams/{team.id}", team);
		}

		[ApiRoute("/teams/{team_id}/patch", RequestType.PUT)]
		public Team UpdateTeam(string team_id)
		{
			throw new NotImplementedException();
		}

		// GetTeam returns a team based on the provided team id string.
		[ApiRoute("/teams/{team_id}", RequestType.GET)]
		public Team GetTeam(string team_id)
		{
			return APIGet<Team>($"/teams/{team_id}");
		}

		[ApiRoute("/teams/name/{name}", RequestType.GET)]
		public Team GetTeamByName(string name)
		{
			throw new NotImplementedException();
			return APIGet<Team>($"/teams/name/{name}");
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
			return APIGet<List<MessageCount>>($"/teams/{team_id}/unread");
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
			return APIGet<TeamStats>($"/teams/{team_id}/stats");
		}

		// GetTeamMember returns a team member based on the provided team and user id strings.
		[ApiRoute("/teams/{team_id}/members/{user_id}", RequestType.GET)]
		public TeamMember GetTeamMember(string team_id, string user_id)
		{
			return APIGet<TeamMember>($"/teams/{team_id}/members/{user_id}");
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

			return APIGet<List<TeamMember>>($"/teams/{team_id}/members");
		}

		[ApiRoute("/teams/{team_id}/members/ids", RequestType.POST)]
		public List<TeamMember> GetTeamMembersByIDs(string team_id, List<string> ids)
		{
			throw new NotImplementedException();
			return APIPost<List<TeamMember>>($"/teams/{team_id}/members/ids", ids);
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
			APIPost<StatusOK>($"/teams/{team_id}/members/{user_id}/roles", obj);
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
		// CreateChannel creates a channel based on the provided channel struct.
		[ApiRoute("/channels", RequestType.POST)]
		public Channel CreateChannel(Channel channel)
		{
			return APIPost<Channel>($"/channels", channel);
		}

		// CreateDirectChannel creates a direct message channel based on the two user
		// ids provided.
		[ApiRoute("/channels/direct", RequestType.POST)]
		public Channel CreateDirectChannel(string userOne, string userTwo)
		{
			List<string> userIDs = new List<string>() { userOne, userTwo };
			return APIPost<Channel>($"/channels/direct", userIDs);
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

			return APIGet<List<Channel>>($"/teams/{team_id}/channels", options);
		}

		// GetChannel returns a channel based on the provided channel id string.
		[ApiRoute("/channels/{channel_id}", RequestType.GET)]
		public Channel GetChannel(string channel_id)
		{
			return APIGet<Channel>($"/channels/{channel_id}");
		}

		// GetChannelByName returns a channel based on the provided channel name and team id strings.
		[ApiRoute("/teams/{team_id}/channels/name/{channel_name}", RequestType.GET)]
		public Channel GetChannelByName(string team_id, string channel_name)
		{
			return APIGet<Channel>($"/teams/{team_id}/channels/name/{channel_name}");
		}

		// GetChannelByNameForTeamName returns a channel based on the provided channel name and team name strings.
		[ApiRoute("/teams/name/{team_name}/channels/name/{channel_name}", RequestType.GET)]
		public Channel GetChannelByNameForTeamName(string team_name, string channel_name)
		{
			return APIGet<Channel>($"/teams/name/{team_name}/channels/name/{channel_name}");
		}

		[ApiRoute("/channels/ids", RequestType.POST)]
		public List<Channel> GetChannelsByIDs(List<string> channels)
		{
			throw new NotImplementedException();
			return APIPost<List<Channel>>($"/channels/ids", channels);
		}

		[ApiRoute("/channels/{channel_id}", RequestType.PUT)]
		public Channel UpdateChannel(Channel channel)
		{
			throw new NotImplementedException();
			return APIPost<Channel>($"/channels/{channel.id}", channel);
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
			return APIGet<ChannelStats>($"/channels/{channel_id}/stats");
		}

		// GetChannelMember gets a channel member.
		[ApiRoute("/channels/{channel_id}/members/{user_id}", RequestType.GET)]
		public ChannelMember GetChannelMember(string channel_id, string user_id)
		{
			return APIGet<ChannelMember>($"/channels/{channel_id}/members/{user_id}");
		}

		// GetChannelMembers gets a page of channel members.
		[ApiRoute("/channels/{channel_id}/members", RequestType.GET)]
		public List<ChannelMember> GetChannelMembers(string channel_id, int page, int per_page)
		{
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};

			return APIGet<List<ChannelMember>>($"/channels/{channel_id}/members", options);
		}

		// GetChannelMembersForUser gets all the channel members for a user on a team.
		[ApiRoute("/users/{user_id}/teams/{team_id}/channels/members", RequestType.GET)]
		public List<ChannelMember> GetChannelMembersForUser(string user_id, string team_id)
		{
			return APIGet<List<ChannelMember>>($"/users/{user_id}/teams/{team_id}/channels/members");
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
			return APIGet<List<Channel>>($"/teams/{team_id}/channels/autocomplete", new Dictionary<string, string>() { { "term", term } });
		}

		[ApiRoute("/channels/{channel_id}/members/{user_id}/view", RequestType.POST)]
		public void ViewChannel(string channel_id, string user_id, string prev_channel_id = "")
		{
			throw new NotImplementedException();
			var obj = new { channel_id = channel_id, prev_channel_id = prev_channel_id };
			APIPost<StatusOK>($"/channels/{channel_id}/members/{user_id}/view", obj);
		}

		[ApiRoute("/channels/{channel_id}/members/{user_id}/roles", RequestType.PUT)]
		public void UpdateChannelMemberRoles(string channel_id, string user_id, string new_roles)
		{
			throw new NotImplementedException();
			var obj = new { user_id = user_id, new_roles = new_roles };
			APIPut<StatusOK>($"/channels/{channel_id}/members/{user_id}/roles", obj);
		}

		[ApiRoute("/channels/{channel_id}/members/{user_id}", RequestType.DELETE)]
		public void DeleteChannelMember(string channel_id, string user_id)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Post Methods
		// CreatePost creates a post based on the provided post struct.
		[ApiRoute("/posts", RequestType.POST)]
		public Post CreatePost(Post newPost)
		{
			return APIPost<Post>($"/posts", newPost);
		}

		[ApiRoute("/posts/{post_id}", RequestType.PUT)]
		public Post UpdatePost(string team_id, string channel_id, Post post)
		{
			throw new NotImplementedException();
			return APIPut<Post>($"/teams/{team_id}/channels/{channel_id}/posts/update", post);
		}

		[ApiRoute("/posts/{post_id}/patch", RequestType.PUT)]
		public Post UpdatePost(string team_id, string channel_id, string post_id)
		{
			throw new NotImplementedException();
		}

		// GetPostsForChannel gets a page of posts with an array for ordering for a channel.
		[ApiRoute("/channels/{channel_id}/posts", RequestType.GET)]
		public SearchResult GetPosts(string channel_id, int page, int per_page)//, long since = 0, string before = "", string after = "")
		{
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
			//if (since != 0)
			//	options.Add("since", since.ToString());
			//if (!string.IsNullOrWhiteSpace(before))
			//	options.Add("before", before);
			//if (!string.IsNullOrWhiteSpace(after))
			//	options.Add("after", after);
			return APIGet<SearchResult>($"/channels/{channel_id}/posts");
		}

		[ApiRoute("/posts/{post_id}", RequestType.DELETE)]
		public void DeletePost(string post_id)
		{
			throw new NotImplementedException();
			APIDelete($"/posts/{post_id}");
		}

		[ApiRoute("/posts/search", RequestType.POST)]
		public SearchResult SearchPosts(string terms, bool is_or_search)
		{
			throw new NotImplementedException();
			var obj = new { terms = terms, is_or_search = is_or_search };
			return APIPost<SearchResult>($"/posts/search", obj);
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
				return APIGet<SearchResult>($"/posts/flagged");
			else
				return APIGet<SearchResult>($"/posts/flagged", options);
		}

		//Not implemented
		//[ApiRoute("/posts/{post_id}/files/info", RequestType.GET)]

		// GetPost gets a single post.
		[ApiRoute("/posts/{post_id}", RequestType.GET)]
		public Post GetPost(string post_id)
		{
			return APIGet<Post>($"/posts/{post_id}");
		}

		// GetPostThread gets a post with all the other posts in the same thread.
		[ApiRoute("/posts/{post_id}/thread", RequestType.GET)]
		public SearchResult GetPostThread(string post_id)
		{
			return APIGet<SearchResult>($"/posts/{post_id}/thread");
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
			APIPost<StatusOK>($"/users/{user_id}/preferences/save", preferences);
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
			return APIGet<List<Preference>>($"/users/{user_id}/preferences/{category}");
		}

		[ApiRoute("/users/{user_id}/preferences/{category}/{name}", RequestType.GET)]
		public Preference GetPreference(string user_id, string category, string name)
		{
			throw new NotImplementedException();
			return APIGet<Preference>($"/users/{user_id}/preferences/{category}/{name}");
		}

		[ApiRoute("/users/{user_id}/preferences/{category}/{name}", RequestType.DELETE)]
		public void DeletePreference(string user_id, string category, string name)
		{
			throw new NotImplementedException();
			APIDelete($"/users/{user_id}/preferences/{category}/{name}");
		}
		#endregion

		#region System Methods
		[ApiRoute("/system/client/config", RequestType.GET)]
		public void GetConfig()
		{
			throw new NotImplementedException();
			APIGet<StatusOK>("/system/client/config");
		}

		[ApiRoute("/system/log", RequestType.POST)]
		public void CreateSystemLogEntry(string log)
		{
			throw new NotImplementedException();
			APIPost<StatusOK>("/system/log", null);
		}

		[ApiRoute("/system/ping", RequestType.GET)]
		public void Ping()
		{
			throw new NotImplementedException();
			APIGet<StatusOK>("/system/ping");
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
			return APIGet<List<string>>("/admin/logs", options);
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
			return APIGet<List<Audit>>("/admin/audits", options);
		}

		[ApiRoute("/admin/config", RequestType.GET)]
		public Config GetAdminConfig()
		{
			throw new NotImplementedException();
			return APIGet<Config>("/admin/config");
		}

		[ApiRoute("/admin/config", RequestType.PUT)]
		public Config UpdateConfig(Config config)
		{
			throw new NotImplementedException();
			return APIPut<Config>("/admin/config", config);
		}

		[ApiRoute("/admin/config/reload", RequestType.POST)]
		public void ReloadConfig()
		{
			throw new NotImplementedException();
			APIPost<StatusOK>("/admin/config/reload", null);
		}

		[ApiRoute("/admin/caches/invalidate", RequestType.GET)]
		public void InvalidateCaches()
		{
			throw new NotImplementedException();
			APIGet<StatusOK>("/admin/caches/invalidate");
		}

		//Not implemented
		//[ApiRoute("/admin/email/test", RequestType.POST)]

		[ApiRoute("/admin/database/recycle", RequestType.POST)]
		public void RecycleDBConn()
		{
			throw new NotImplementedException();
			APIPost<StatusOK>("/admin/database/recycle", null);
		}

		[ApiRoute("/admin/analytics/{type}", RequestType.GET)]
		public List<Analytic> GetAnalytics(string type, string team_id = "")
		{
			throw new NotImplementedException();
			Dictionary<string, string> options = new Dictionary<string, string>();
			if (!string.IsNullOrWhiteSpace(team_id))
				options.Add("team_id", team_id);

			if (options.Count == 0)
				return APIGet<List<Analytic>>($"/admin/analytics/{type}");
			else
				return APIGet<List<Analytic>>($"/admin/analytics/{type}", options);
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
