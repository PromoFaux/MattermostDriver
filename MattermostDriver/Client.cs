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
			API.ApiBase = url + "/api/v3";

			//Generate websocket url
			if (url.StartsWith("https"))
				websocket = "wss" + url.Substring(5) + "/api/v3/users/websocket";
			else if (url.StartsWith("http"))
				websocket = "ws" + url.Substring(4) + "/api/v3/users/websocket";
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
				case "hello":
					logger.Debug("Hello event received.");
					Hello?.Invoke(JsonConvert.DeserializeObject<HelloEvent>(rawdata));
					break;
				case "new_user":
					NewUserEvent nuevent = JsonConvert.DeserializeObject<NewUserEvent>(rawdata);
					logger.Debug("New user event received: " + nuevent.ToString());
					NewUser?.Invoke(nuevent);
					break;
				case "posted":
					PrePostedEvent ppevent = JsonConvert.DeserializeObject<PrePostedEvent>(rawdata);
					PostedEvent pevent = new PostedEvent()
					{
						broadcast = ppevent.broadcast,
						@event = ppevent.@event,
						data = new PostedEvent.Data()
						{
							channel_display_name = ppevent.data.channel_display_name,
							channel_name = ppevent.data.channel_name,
							channel_type = ppevent.data.channel_type,
							sender_name = ppevent.data.sender_name,
							team_id = ppevent.data.team_id,
							post = JsonConvert.DeserializeObject<Post>(ppevent.data.post)
						}
					};
					logger.Debug("Posted event received: " + pevent.ToString());
					Posted?.Invoke(pevent);
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
		[ApiRoute("/users/create", RequestType.POST)]
		public User CreateUser(string email, string username, string password, string first_name = "", string last_name = "", string nickname = "", string locale = "")
		{
			var createUserRequest = new { email = email, username = username, password = password, first_name = first_name, last_name = last_name, nickname = nickname, locale = locale };
			string rawdata = API.Post($"/users/create", createUserRequest);
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<User>(rawdata);
			else
				return null;
		}

		[ApiRoute("/users/me", RequestType.GET)]
		public Self Me()
		{
			string rawdata = API.Get($"/users/me");
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<Self>(rawdata);
			else
				return null;
		}

		[ApiRoute("/users/login", RequestType.POST)]
		public Self Login(string login_id, string password, string token = "", string device_id = "")
		{
			var loginRequest = new { login_id = login_id, password = password, token = token, device_id = device_id };
			string rawdata = API.Post($"/users/login", loginRequest);
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<Self>(rawdata);
			else
				return null;
		}

		[ApiRoute("/users/logout", RequestType.POST)]
		public void Logout()
		{
			API.Post($"/users/logout", null);
		}

		[ApiRoute("/users/{offset}/{limit}", RequestType.GET)]
		public Dictionary<string, User> GetUsers(int offset, int limit)
		{
			string rawdata = API.Get($"/users/{offset}/{limit}");
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<Dictionary<string, User>>(rawdata);
			else
				return null;
		}

		[ApiRoute("/teams/{team_id}/users/{offset}/{limit}", RequestType.GET)]
		public Dictionary<string, User> GetUsersInTeam(string team_id, int offset, int limit)
		{
			string rawdata = API.Get($"/teams/{team_id}/users/{offset}/{limit}");
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<Dictionary<string, User>>(rawdata);
			else
				return null;
		}

		[ApiRoute("/users/search", RequestType.POST)]
		public List<User> SearchUsers(string term, string team_id = "", string in_channel_id = "", string not_in_channel_id = "", bool allow_inactive = false)
		{
			var searchUserRequest = new { term = term, team_id = team_id, in_channel_id = in_channel_id, not_in_channel_id = not_in_channel_id, allow_inactive = allow_inactive };
			string rawdata = API.Post($"/users/search", searchUserRequest);
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<List<User>>(rawdata);
			else
				return null;
		}

		[ApiRoute("/users/name/{username}", RequestType.GET)]
		public User GetUserByUsername(string username)
		{
			string rawdata = API.Get($"/users/name/{username}");
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<User>(rawdata);
			else
				return null;
		}

		[ApiRoute("/users/email/{email}", RequestType.GET)]
		public User GetUserByEmail(string email)
		{
			string rawdata = API.Get($"/users/email/{email}");
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<User>(rawdata);
			else
				return null;
		}

		[ApiRoute("/users/ids", RequestType.POST)]
		public Dictionary<string,User> GetUsersByIDs(List<string> ids)
		{
			string rawdata = API.Post($"/users/ids", ids);
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<Dictionary<string, User>>(rawdata);
			else
				return null;
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/users/{offset}/{limit}", RequestType.GET)]
		public Dictionary<string,User> GetUsersInChannel(string team_id, string channel_id, int offset, int limit)
		{
			string rawdata = API.Get($"/teams/{team_id}/channels/{channel_id}/users/{offset}/{limit}");
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<Dictionary<string, User>>(rawdata);
			else
				return null;
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/users/not_in_channel/{offset}/{limit}", RequestType.GET)]
		public Dictionary<string, User> GetUsersNotInChannel(string team_id, string channel_id, int offset, int limit)
		{
			string rawdata = API.Get($"/teams/{team_id}/channels/{channel_id}/users/not_in_channel/{offset}/{limit}");
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<Dictionary<string, User>>(rawdata);
			else
				return null;
		}

		[ApiRoute("/users/update", RequestType.POST)]
		public User UpdateUser(User user)
		{
			string rawdata = API.Post($"/users/update", user);
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<User>(rawdata);
			else
				return null;
		}

		[ApiRoute("/users/update_roles", RequestType.POST)]
		public void UpdateUserRoles(string user_id, string new_roles, string team_id = "")
		{
			if (!string.IsNullOrWhiteSpace(team_id))
			{
				var request = new { user_id = user_id, team_id = team_id, new_roles = new_roles };
				API.Post($"/users/update_roles", request);
			}
			else
			{
				var request = new { user_id = user_id, new_roles = new_roles };
				API.Post($"/users/{user_id}/update_roles", request);
			}
		}

		[ApiRoute("/users/update_active", RequestType.POST)]
		public User UpdateActive(string user_id, bool active)
		{
			var request = new { user_id = user_id, active = active };
			string rawdata = API.Post($"/users/update_active", request);
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<User>(rawdata);
			else
				return null;
		}

		[ApiRoute("/users/update_notify", RequestType.POST)]
		//public void UpdateNotify(string user_id, string email, string desktop_sound, string desktop, string comments, string desktop_duration = "", string first_name = "", string mention_keys = "", string push = "", string push_status = "")
		public void UpdateNotify(string user_id, string comments, string desktop, string desktop_sound, string email, string channel = "", string desktop_duration = "", string first_name = "", string mention_keys = "", string push = "", string push_status = "")
		{
			var obj = new { user_id = user_id, comments = comments, desktop = desktop, desktop_sound = desktop_sound, email = email, channel = channel, desktop_duration = desktop_duration, first_name = first_name, mention_keys = mention_keys, push = push, push_status = push_status };
			API.Post($"/users/update_notify", obj);
		}

		/*
		 * TODO:
		[ApiRoute("/users/newpassword", RequestType.POST)]
		[ApiRoute("/users/send_password_reset", RequestType.POST)]
		[ApiRoute("/users/reset_password", RequestType.POST)]
		[ApiRoute("/users/revoke_session", RequestType.POST)]
		[ApiRoute("/users/attach_device", RequestType.POST)]
		[ApiRoute("/users/verify_email", RequestType.POST)]
		[ApiRoute("/users/resend_verification", RequestType.POST)]
		[ApiRoute("/users/newimage", RequestType.POST)]
		[ApiRoute("/users/autocomplete", RequestType.GET)]
		[ApiRoute("/teams/{team_id}/users/autocomplete", RequestType.GET)]
		[ApiRoute("/teams/{team_id}/channels/{channel_id}/users/autocomplete", RequestType.GET)]
		[ApiRoute("/users/mfa", RequestType.POST)]
		[ApiRoute("/users/generate_mfa_secret", RequestType.POST)]
		[ApiRoute("/users/update_mfa", RequestType.POST)]
		[ApiRoute("/users/claim/email_to_oauth", RequestType.POST)]
		[ApiRoute("/users/claim/oauth_to_email", RequestType.POST)]
		[ApiRoute("/users/claim/email_to_ldap", RequestType.POST)]
		[ApiRoute("/users/claim/ldap_to_email", RequestType.POST)]
		[ApiRoute("/users/{user_id}/get", RequestType.GET)]
		[ApiRoute("/users/{user_id}/sessions", RequestType.GET)]
		[ApiRoute("/users/{user_id}/audits", RequestType.GET)]
		[ApiRoute("/users/{user_id}/image", RequestType.GET)]
		*/
		#endregion
	}
}
