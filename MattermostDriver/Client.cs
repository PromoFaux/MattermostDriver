using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using WebSocket4Net;

namespace MattermostDriver
{
	/// <summary>
	/// The client used to consume the Mattermost API.
	/// </summary>
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

		/// <summary>
		/// Creates a new instance of the Mattermost Driver Client.
		/// </summary>
		/// <param name="url">The base url of the Mattermost server.</param>
		/// <param name="port">The port of the Mattermost server.</param>
		/// <param name="logger">A custom ILogger instance for logging output.</param>
		public Client(string url, string port, ILogger logger)
		{
			//Setup logging
			this.logger = logger;

			//Remove last '/' for consistency
			if (url.EndsWith("/"))
				url = url.TrimEnd('/');

			//Generate API base url
			ApiBase = url + (port == "80" || port == "443" ? "" : ":" + port) + "/api/v4";

			//Generate websocket url
			if (url.StartsWith("https"))
				WebsocketUrl = "wss" + url.Substring(5) + "/api/v4/websocket";
			else if (url.StartsWith("http"))
				WebsocketUrl = "ws" + url.Substring(4) + "/api/v4/websocket";
			else
			{
				logger.Error($"Invalid server URL in Client.ctor(): {url}");
				throw new Exception("Invalid server URL.");
			}
			socket = new WebSocket(WebsocketUrl);
			socket.Opened += OnWebsocketOpen;
			socket.MessageReceived += OnWebsocketMessage;
			socket.Closed += OnWebsocketClose;
			socket.Error += Socket_Error;

			//Setup REST client
			client = new RestClient(ApiBase);
		}

		private void Socket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
		{
			Console.WriteLine(e.Exception.ToString());
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

			token = result.Headers[0].Value.ToString();

			return JsonConvert.DeserializeObject<Self>(result.Content);
		}

		private T APIPost<T>(string endpoint, object jsonbody)
		{
			RestRequest request = new RestRequest(endpoint, Method.POST);
			request.AddHeader("Content-Type", "application/json");
			if (!string.IsNullOrWhiteSpace(token))
				request.AddHeader("Authorization", "Bearer " + token);
			var obj = JsonConvert.SerializeObject(jsonbody);
			if (jsonbody != null)
				request.AddParameter("application/json", obj, ParameterType.RequestBody);

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

		/// <summary>
		/// Connects to the server via websocket.
		/// </summary>
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

		/// <summary>
		/// Sends a UserTyping websocket event to the server.
		/// </summary>
		/// <param name="channel_id">The channel ID.</param>
		/// <param name="parent_id"></param>
		public void SendUserTyping(string channel_id, string parent_id = "")
		{
			if (socket.State != WebSocketState.Open)
			{
				logger.Error("Cannot send websocket UserTyping event without connecting to websocket.");
				return;
			}
			var request = new { seq = ++seq, action = "user_typing", data = new { channel_id = channel_id, parent_id = parent_id } };
			awaiting_ok = true;
			socket.Send(JsonConvert.SerializeObject(request));
		}
		#endregion

		#region User Methods
		/// <summary>
		/// Deactivates a user account.
		/// </summary>
		/// <param name="user_id">The user ID.</param>
		[ApiRoute("/users/{user_id}", RequestType.DELETE)]
		public void DeleteUser(string user_id)
		{
			APIDelete($"/users/{user_id}");
		}

		/// <summary>
		/// Gets a user object based on the specified user.
		/// </summary>
		/// <param name="user_id">The user ID.</param>
		/// <returns>The User object requested.</returns>
		[ApiRoute("/users/{user_id}", RequestType.GET)]
		public User GetUser(string user_id)
		{
			return APIGet<User>($"/users/{user_id}");
		}

		/// <summary>
		/// Updates a user based on the provided user object.
		/// </summary>
		/// <param name="user">The updated User object.</param>
		/// <returns>The updated User object.</returns>
		[ApiRoute("/users/{user_id}", RequestType.PUT)]
		public User UpdateUser(User user)
		{
			return APIPut<User>($"/users/{user.ID}", user);
		}
		
		/// <summary>
		/// Updates the 'active' status of the specified user.
		/// </summary>
		/// <param name="user_id">The user ID.</param>
		/// <param name="active">If true, the user is active.</param>
		[ApiRoute("/users/{user_id}/active", RequestType.PUT)]
		public void UpdateUserActive(string user_id, bool active)
		{
			var obj = new { active = active };
			APIPut<StatusOK>($"/users/{user_id}/active", obj);
		}

		// GetUserAudits returns a list of audit based on the provided user id string.
		/// <summary>
		/// Gets a list of audit logs for the specified user.
		/// </summary>
		/// <param name="user_id">The user ID.</param>
		/// <param name="page">The page of results to retrieve.</param>
		/// <param name="per_page">The number of entries per page to retrieve.</param>
		/// <returns>A list of Audit objects.</returns>
		[ApiRoute("/users/{user_id}/audits", RequestType.GET)]
		public List<Audit> GetAudits(string user_id, int page, int per_page)
		{
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
			return APIGet<List<Audit>>($"/users/{user_id}/audits", options);
		}

		/// <summary>
		/// Activates or deactivates multi-factor authentication for the specified user.
		/// </summary>
		/// <param name="user_id">The user ID.</param>
		/// <param name="activate">If true, MFA will be activated and a code is required. If false, MFA will be deactivated and a code is not required.</param>
		/// <param name="code">(Optional) If activating MFA, a valid code is required.</param>
		[ApiRoute("/users/{user_id}/mfa", RequestType.PUT)]
		public void UpdateUserMFA(string user_id, bool activate, string code = "")
		{
			var obj = new { code = code, activate = activate };
			APIPut<StatusOK>($"/users/{user_id}/mfa", obj);
		}

		// GenerateMfaSecret will generate a new MFA secret for a user and return it as a string.
		/// <summary>
		/// Generates a new multi-factor authentication secret for the specified user.
		/// </summary>
		/// <param name="user_id">The user ID.</param>
		/// <returns>The newly-generated MFA secret.</returns>
		[ApiRoute("/users/{user_id}/mfa/generate", RequestType.POST)]
		public string GenerateMFA(string user_id)
		{
			return APIPost<GetMFA>($"/users/{user_id}/mfa/generate", null).secret;
		}

		// PatchUser partially updates a user in the system. Any missing fields are not updated.
		[ApiRoute("/users/{user_id}/patch", RequestType.PUT)]
		public User UpdateUser(string user_id, string username = "", string nickname = "", string first_name = "", string last_name = "", string position = "", string email = "", string locale = "")
		{
			var obj = new { username = username, nickname = nickname, first_name = first_name, last_name = last_name, position = position, email = email, locale = locale };
			return APIPut<User>($"/users/{user_id}/patch", obj);
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

		// UpdateUserRoles updates a user's roles in the system. A user can have "system_user" and "system_admin" roles.
		[ApiRoute("/users/{user_id}/roles", RequestType.PUT)]
		public void UpdateUserRoles(string user_id, string roles)
		{
			var obj = new { user_id = user_id, roles = roles };
			APIPut<StatusOK>($"/users/{user_id}/roles", obj);
		}

		// GetSessions returns a list of sessions based on the provided user id string.
		[ApiRoute("/users/{user_id}/sessions", RequestType.GET)]
		public List<Session> GetSessions(string user_id)
		{
			return APIGet<List<Session>>($"/users/{user_id}/sessions");
		}

		// RevokeSession revokes a user session based on the provided user id and session id strings.
		[ApiRoute("/users/{user_id}/sessions/revoke", RequestType.POST)]
		public void RevokeSessions(string user_id, string session_id)
		{
			var obj = new { session_id = session_id };
			APIPost<StatusOK>($"/users/{user_id}/sessions/revoke", obj);
		}

		// CreateUser creates a user in the system based on the provided user struct.
		//TODO: add hash/inviteID options?
		[ApiRoute("/users", RequestType.POST)]
		public User CreateUser(string email, string username, string password, string first_name = "", string last_name = "", string nickname = "", string locale = "")
		{
			var obj = new { email = email, username = username, password = password, first_name = first_name, last_name = last_name, nickname = nickname, locale = locale };
			return APIPost<User>($"/users", obj);
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

		// AutocompleteUsers returns the users in the system, team, or channel based on search term.
		[ApiRoute("/users/autocomplete", RequestType.GET)]
		public UserAutoComplete AutoCompleteUsers(string username, string in_channel = "", string in_team = "")
		{
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "name", username }
			};
			if (!string.IsNullOrWhiteSpace(in_channel))
				options.Add("in_channel", in_channel);
			if (!string.IsNullOrWhiteSpace(in_team))
				options.Add("in_team", in_team);

			return APIGet<UserAutoComplete>($"/users/autocomplete", options);
		}

		// GetUserByEmail returns a user based on the provided user email string.
		[ApiRoute("/users/email/{email}", RequestType.GET)]
		public User GetUserByEmail(string email)
		{
			return APIGet<User>($"/users/email/{email}");
		}

		// VerifyUserEmail will verify a user's email using user id and hash strings.
		[ApiRoute("/users/email/verify", RequestType.POST)]
		public void VerifyUserEmail(string user_id, string hash_id)
		{
			var obj = new { user_id = user_id, hash_id = hash_id };
			APIPost<StatusOK>($"/users/email/verify", obj);
		}

		// SendVerificationEmail will send an email to the user with the provided email address, if
		// that user exists. The email will contain a link that can be used to verify the user's
		// email address.
		[ApiRoute("/users/email/verify/send", RequestType.POST)]
		public void SendVerificationEmail(string email)
		{
			var obj = new { email = email };
			APIPost<StatusOK>($"/users/email/verify/send", obj);
		}

		// GetUsersByIds returns a list of users based on the provided user ids.
		[ApiRoute("/users/ids", RequestType.POST)]
		public List<User> GetUsersByIDs(List<string> ids)
		{
			return APIPost<List<User>>($"/users/ids", ids);
		}

		// Login authenticates a user by login id, which can be username, email or some sort
		// of SSO identifier based on server configuration, and a password.
		[ApiRoute("/users/login", RequestType.POST)]
		public Self Login(string login_id, string password)
		{
			var obj = new { login_id = login_id, password = password };
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
			var obj = new { login_id = login_id, password = password, device_id = device_id };
			return APIPostGetAuth(obj);
		}

		// Logout terminates the current user's session.
		[ApiRoute("/users/logout", RequestType.POST)]
		public void Logout()
		{
			token = "";
			APIPost<StatusOK>($"/users/logout", null);
		}

		// SwitchAccountType changes a user's login type from one type to another.
		[ApiRoute("/users/login/switch", RequestType.POST)]
		public string SwitchAccountType(string current_service, string new_service, string email, string current_password = "", string new_password = "", string mfa_code = "", string ldap_id = "")
		{
			var obj = new { current_service = current_service, new_service = new_service, email = email, current_password = current_password, new_password = new_password, mfa_code = mfa_code, ldap_id = ldap_id };
			return APIPost<FollowLink>("/users/login/switch", obj).follow_link;
		}

		// CheckUserMfa checks whether a user has MFA active on their account or not based on the
		// provided login id.
		[ApiRoute("/users/mfa", RequestType.POST)]
		public bool CheckUserMfa(string login_id)
		{
			var obj = new { login_id = login_id };
			var resp = APIPost<CheckMFA>("/users/mfa", obj);
			return resp.mfa_required == "true";
		}

		// SearchUsers returns a list of users based on some search criteria.
		[ApiRoute("/users/search", RequestType.POST)]
		public List<User> SearchUsers(string term, string team_id, string not_in_team_id = "", string in_channel_id = "", string not_in_channel_id = "", bool allow_inactive = false, bool without_team = false)
		{
			var obj = new { term = term, team_id = team_id, not_in_team_id = not_in_team_id, in_channel_id = in_channel_id, not_in_channel_id = not_in_channel_id, allow_inactive = allow_inactive, without_team = without_team };
			return APIPost<List<User>>($"/users/search", obj);
		}

		// AttachDeviceId attaches a mobile device ID to the current session.
		[ApiRoute("/users/sessions/device", RequestType.PUT)]
		public void AttachDeviceId(string device_id)
		{
			var obj = new { device_id = device_id };
			APIPut<StatusOK>("/users/sessions/device", obj);
		}

		// GetUserByUsername returns a user based on the provided user name string.
		[ApiRoute("/users/username/{username}", RequestType.GET)]
		public User GetUserByUsername(string username)
		{
			return APIGet<User>($"/users/username/{username}");
		}

		// Not implemented
		// [ApiRoute("/users/{user_id}/image"), RequestType.POST)]
		// [ApiRoute("/users/{user_id}/image"), RequestType.GET)]
		#endregion

		#region Team Methods
		// GetAllTeams returns all teams based on permissions.
		[ApiRoute("/teams", RequestType.GET)]
		public List<Team> GetTeams(int page, int per_page)
		{
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};

			return APIGet<List<Team>>("/teams", options);
		}

		// CreateTeam creates a team in the system based on the provided team struct.
		[ApiRoute("/teams", RequestType.POST)]
		public Team CreateTeam(Team team)
		{
			return APIPost<Team>($"/teams", team);
		}

		// GetTeam returns a team based on the provided team id string.
		[ApiRoute("/teams/{team_id}", RequestType.GET)]
		public Team GetTeam(string team_id)
		{
			return APIGet<Team>($"/teams/{team_id}");
		}

		// UpdateTeam will update a team.
		[ApiRoute("/teams/{team_id}", RequestType.PUT)]
		public Team UpdateTeam(Team team)
		{
			return APIPut<Team>($"/teams/{team.ID}", team);
		}

		// SoftDeleteTeam deletes the team softly (archive only, not permanent delete).
		[ApiRoute("/teams/{team_id}", RequestType.DELETE)]
		public void DeleteTeam(string team_id)
		{
			APIDelete("/teams/{team_id}");
		}

		// GetTeamMembers returns team members based on the provided team id string.
		[ApiRoute("/teams/{team_id}/members", RequestType.GET)]
		public List<TeamMember> GetTeamMembers(string team_id, int page, int per_page)
		{
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};

			return APIGet<List<TeamMember>>($"/teams/{team_id}/members", options);
		}

		// AddTeamMember adds user to a team and return a team member.
		[ApiRoute("/teams/{team_id}/members", RequestType.POST)]
		public TeamMember CreateTeamMember(string team_id, string user_id)
		{
			var obj = new TeamMember()
			{
				TeamID = team_id,
				UserID = user_id
			};
			return APIPost<TeamMember>($"/teams/{team_id}/members", obj);
		}

		// AddTeamMembers adds a number of users to a team and returns the team members.
		[ApiRoute("/teams/{team_id}/members/batch", RequestType.POST)]
		public List<TeamMember> CreateTeamMembers(string team_id, List<string> user_ids)
		{
			List<TeamMember> obj = new List<TeamMember>();
			foreach (string userid in user_ids)
			{
				obj.Add(new TeamMember()
				{
					TeamID = team_id,
					UserID = userid
				});
			}
			return APIPost<List<TeamMember>>($"/teams/{team_id}/members/batch", obj);
		}

		// GetTeamMembersByIds will return an array of team members based on the
		// team id and a list of user ids provided. Must be authenticated.
		[ApiRoute("/teams/{team_id}/members/ids", RequestType.POST)]
		public List<TeamMember> GetTeamMembersByIDs(string team_id, List<string> ids)
		{
			return APIPost<List<TeamMember>>($"/teams/{team_id}/members/ids", ids);
		}

		// GetTeamMember returns a team member based on the provided team and user id strings.
		[ApiRoute("/teams/{team_id}/members/{user_id}", RequestType.GET)]
		public TeamMember GetTeamMember(string team_id, string user_id)
		{
			return APIGet<TeamMember>($"/teams/{team_id}/members/{user_id}");
		}

		// RemoveTeamMember will remove a user from a team.
		[ApiRoute("/teams/{team_id}/members/{user_id}", RequestType.DELETE)]
		public void DeleteTeamMember(string team_id, string user_id)
		{
			APIDelete($"/teams/{team_id}/members/{user_id}");
		}

		// UpdateTeamMemberRoles will update the roles on a team for a user.
		[ApiRoute("/teams/{team_id}/members/{user_id}/roles", RequestType.PUT)]
		public void UpdateTeamMemberRoles(string team_id, string user_id, string roles)
		{
			var obj = new { roles = roles };
			APIPut<StatusOK>($"/teams/{team_id}/members/{user_id}/roles", obj);
		}

		// PatchTeam partially updates a team. Any missing fields are not updated.
		[ApiRoute("/teams/{team_id}/patch", RequestType.PUT)]
		public Team UpdateTeam(string team_id, string display_name = "", string description = "", string company_name = "", string invite_id = "", string allow_open_invite = "")
		{
			var obj = new { display_name = display_name, description = description, company_name = company_name, invite_id = invite_id, allow_open_invite = allow_open_invite };
			return APIPut<Team>($"/teams/{team_id}/patch", obj);
		}

		// GetTeamStats returns a team stats based on the team id string.
		// Must be authenticated.
		[ApiRoute("/teams/{team_id}/stats", RequestType.GET)]
		public TeamStats GetTeamStats(string team_id)
		{
			return APIGet<TeamStats>($"/teams/{team_id}/stats");
		}

		// GetTeamByName returns a team based on the provided team name string.
		[ApiRoute("/teams/name/{name}", RequestType.GET)]
		public Team GetTeamByName(string name)
		{
			return APIGet<Team>($"/teams/name/{name}");
		}

		// TeamExists returns true or false if the team exist or not.
		[ApiRoute("/teams/name/{name}/exists", RequestType.GET)]
		public bool CheckTeamExists(string name)
		{
			return APIGet<Exists>($"/teams/name/{name}/exists").exists;
		}

		// SearchTeams returns teams matching the provided search term.
		[ApiRoute("/teams/search", RequestType.POST)]
		public List<Team> SearchTeams(string term)
		{
			var obj = new { term = term };
			return APIPost<List<Team>>("/teams/search", obj);
		}

		// GetTeamsForUser returns a list of teams a user is on. Must be logged in as the user
		// or be a system administrator.
		[ApiRoute("/users/{user_id}/teams", RequestType.GET)]
		public List<Team> GetTeamsForUser(string user_id)
		{
			return APIGet<List<Team>>($"/users/{user_id}/teams");
		}

		// GetTeamMembersForUser returns the team members for a user.
		[ApiRoute("/users/{user_id}/teams/members", RequestType.GET)]
		public List<TeamMember> GetTeamMembersForUser(string user_id)
		{
			return APIGet<List<TeamMember>>($"/users/{user_id}/teams/members");
		}

		// GetTeamsUnreadForUser will return an array with TeamUnread objects that contain the amount
		// of unread messages and mentions the current user has for the teams it belongs to.
		// An optional team ID can be set to exclude that team from the results. Must be authenticated.
		[ApiRoute("/users/{user_id}/teams/unread", RequestType.GET)]
		public List<MessageCount> GetUnreadsFromAllTeams(string user_id, string exclude_team = "")
		{
			if (!string.IsNullOrWhiteSpace(exclude_team))
			{
				Dictionary<string, string> options = new Dictionary<string, string>()
				{
					{ "exclude_team", exclude_team }
				};
				return APIGet<List<MessageCount>>($"/users/{user_id}/teams/unread", options);
			}
			else
			{
				return APIGet<List<MessageCount>>($"/users/{user_id}/teams/unread");
			}
		}

		// GetTeamUnread will return a TeamUnread object that contains the amount of
		// unread messages and mentions the user has for the specified team.
		// Must be authenticated.
		[ApiRoute("/users/{user_id}/teams/{team_id}/unread", RequestType.GET)]
		public TeamUnread GetTeamUnread(string team_id, string user_id)
		{
			return APIGet<TeamUnread>($"/users/{user_id}/teams/{team_id}/unread");
		}

		// InviteUsersToTeam invite users by email to the team.
		[ApiRoute("/teams/{team_id}/invite/email", RequestType.POST)]
		public void InviteUserToTeam(string team_id, List<string> user_emails)
		{
			APIPost<StatusOK>($"/teams/{team_id}/invite/email", user_emails);
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

		// DeleteChannel deletes channel based on the provided channel id string.
		[ApiRoute("/channels/{channel_id}", RequestType.DELETE)]
		public void DeleteChannel(string channel_id)
		{
			APIDelete($"/channels/{channel_id}");
		}

		// GetChannel returns a channel based on the provided channel id string.
		[ApiRoute("/channels/{channel_id}", RequestType.GET)]
		public Channel GetChannel(string channel_id)
		{
			return APIGet<Channel>($"/channels/{channel_id}");
		}

		// UpdateChannel update a channel based on the provided channel struct.
		[ApiRoute("/channels/{channel_id}", RequestType.PUT)]
		public Channel UpdateChannel(Channel channel)
		{
			return APIPut<Channel>($"/channels/{channel.ID}", channel);
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

		// AddChannelMember adds user to channel and return a channel member.
		[ApiRoute("/channels/{channel_id}/members", RequestType.POST)]
		public ChannelMember CreateChannelMember(string channel_id, string user_id)
		{
			var obj = new { user_id = user_id };
			return APIPost<ChannelMember>($"/channels/{channel_id}/members", obj);
		}

		// GetChannelMembersByIds gets the channel members in a channel for a list of user ids.
		[ApiRoute("/channels/{channel_id}/members/ids", RequestType.POST)]
		public List<ChannelMember> GetChannelMembersByIDs(string channel_id, List<string> ids)
		{
			return APIPost<List<ChannelMember>>($"/channels/{channel_id}/members/ids", ids);
		}

		// GetChannelMember gets a channel member.
		[ApiRoute("/channels/{channel_id}/members/{user_id}", RequestType.GET)]
		public ChannelMember GetChannelMember(string channel_id, string user_id)
		{
			return APIGet<ChannelMember>($"/channels/{channel_id}/members/{user_id}");
		}

		// RemoveUserFromChannel will delete the channel member object for a user, effectively removing the user from a channel.
		[ApiRoute("/channels/{channel_id}/members/{user_id}", RequestType.DELETE)]
		public void DeleteChannelMember(string channel_id, string user_id)
		{
			APIDelete($"/channels/{channel_id}/members/{user_id}");
		}

		// UpdateChannelNotifyProps will update the notification properties on a channel for a user.
		[ApiRoute("/channels/{channel_id}/members/{user_id}/notify_props", RequestType.PUT)]
		public void UpdateChannelNotifyProps(string channel_id, string user_id, ChannelMember.NotificationProperties notify_props)
		{
			APIPut<StatusOK>($"/channels/{channel_id}/members/{user_id}/notify_props", notify_props);
		}

		// UpdateChannelRoles will update the roles on a channel for a user.
		[ApiRoute("/channels/{channel_id}/members/{user_id}/roles", RequestType.PUT)]
		public void UpdateChannelMemberRoles(string channel_id, string user_id, string roles)
		{
			var obj = new { roles = roles };
			APIPut<StatusOK>($"/channels/{channel_id}/members/{user_id}/roles", obj);
		}

		// PatchChannel partially updates a channel. Any missing fields are not updated.
		[ApiRoute("/channels/{channel_id}/patch", RequestType.PUT)]
		public Channel UpdateChannel(string channel_id, string display_name = "", string name = "", string header = "", string purpose = "")
		{
			var obj = new { display_name = display_name, name = name, header = header, purpose = purpose };
			return APIPut<Channel>($"/channels/{channel_id}/patch", obj);
		}

		// GetPinnedPosts gets a list of pinned posts.
		[ApiRoute("/channels/{channel_id}/pinned", RequestType.GET)]
		public PostList GetPinnedPosts(string channel_id)
		{
			return APIGet<PostList>($"/channels/{channel_id}/pinned");
		}

		// GetChannelStats returns statistics for a channel.
		[ApiRoute("/channels/{channel_id}/stats", RequestType.GET)]
		public ChannelStats GetChannelStats(string channel_id)
		{
			return APIGet<ChannelStats>($"/channels/{channel_id}/stats");
		}

		// CreateDirectChannel creates a direct message channel based on the two user
		// ids provided.
		[ApiRoute("/channels/direct", RequestType.POST)]
		public Channel CreateDirectChannel(string userOne, string userTwo)
		{
			List<string> userIDs = new List<string>() { userOne, userTwo };
			return APIPost<Channel>($"/channels/direct", userIDs);
		}

		// CreateGroupChannel creates a group message channel based on userIds provided
		[ApiRoute("/channels/group", RequestType.POST)]
		public Channel CreateGroupChannel(List<string> user_ids)
		{
			return APIPost<Channel>("/channels/group", user_ids);
		}

		// ViewChannel performs a view action for a user. Synonymous with switching channels or marking channels as read by a user.
		[ApiRoute("/channels/members/{user_id}/view", RequestType.POST)]
		public void ViewChannel(string user_id, string channel_id, string prev_channel_id)
		{
			var obj = new { channel_id = channel_id, prev_channel_id = prev_channel_id };
			APIPost<StatusOK>($"/channels/members/{user_id}/view", obj);
		}

		// GetPublicChannelsForTeam returns a list of public channels based on the provided team id string.
		[ApiRoute("/teams/{team_id}/channels", RequestType.GET)]
		public List<Channel> GetPublicChannelsForTeam(string team_id, int page, int per_page)
		{
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};

			return APIGet<List<Channel>>($"/teams/{team_id}/channels", options);
		}

		// GetPublicChannelsByIdsForTeam returns a list of public channels based on provided team id string
		[ApiRoute("/teams/{team_id}/channels/ids", RequestType.POST)]
		public List<Channel> GetPublicChannelsForTeam(string team_id, List<string> channel_ids)
		{
			return APIPost<List<Channel>>($"/teams/{team_id}/channels/ids", channel_ids);
		}

		// GetChannelByName returns a channel based on the provided channel name and team id strings.
		[ApiRoute("/teams/{team_id}/channels/name/{channel_name}", RequestType.GET)]
		public Channel GetChannelByName(string team_id, string channel_name)
		{
			return APIGet<Channel>($"/teams/{team_id}/channels/name/{channel_name}");
		}

		// SearchChannels returns the channels on a team matching the provided search term.
		[ApiRoute("/teams/{team_id}/channels/search", RequestType.POST)]
		public List<Channel> SearchChannels(string team_id, string term)
		{
			var obj = new { term = term };
			return APIPost<List<Channel>>($"/teams/{team_id}/channels/search", obj);
		}

		// GetChannelByNameForTeamName returns a channel based on the provided channel name and team name strings.
		[ApiRoute("/teams/name/{team_name}/channels/name/{channel_name}", RequestType.GET)]
		public Channel GetChannelByNameForTeamName(string team_name, string channel_name)
		{
			return APIGet<Channel>($"/teams/name/{team_name}/channels/name/{channel_name}");
		}

		// GetChannelUnread will return a ChannelUnread object that contains the number of
		// unread messages and mentions for a user.
		[ApiRoute("/users/{user_id}/channels/{channel_id}/unread", RequestType.GET)]
		public ChannelUnread GetChannelUnread(string channel_id, string user_id)
		{
			return APIGet<ChannelUnread>($"/users/{user_id}/channels/{channel_id}/unread");
		}

		// GetChannelsForTeamForUser returns a list channels of on a team for a user.
		[ApiRoute("/users/{user_id}/teams/{team_id}/channels", RequestType.GET)]
		public List<Channel> GetChannelsForTeamForUser(string team_id, string user_id)
		{
			return APIGet<List<Channel>>($"/users/{user_id}/teams/{team_id}/channels");
		}

		// GetChannelMembersForUser gets all the channel members for a user on a team.
		[ApiRoute("/users/{user_id}/teams/{team_id}/channels/members", RequestType.GET)]
		public List<ChannelMember> GetChannelMembersForUser(string user_id, string team_id)
		{
			return APIGet<List<ChannelMember>>($"/users/{user_id}/teams/{team_id}/channels/members");
		}
		#endregion

		#region Post Methods
		// GetPostsForChannel gets a page of posts with an array for ordering for a channel.
		[ApiRoute("/channels/{channel_id}/posts", RequestType.GET)]
		public PostList GetPosts(string channel_id, int page, int per_page)//, long since = 0, string before = "", string after = "")
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
			return APIGet<PostList>($"/channels/{channel_id}/posts", options);
		}

		// CreatePost creates a post based on the provided post struct.
		[ApiRoute("/posts", RequestType.POST)]
		public Post CreatePost(Post newPost)
		{
			return APIPost<Post>($"/posts", newPost);
		}

		// GetPost gets a single post.
		[ApiRoute("/posts/{post_id}", RequestType.GET)]
		public Post GetPost(string post_id)
		{
			return APIGet<Post>($"/posts/{post_id}");
		}

		// UpdatePost updates a post based on the provided post struct.
		[ApiRoute("/posts/{post_id}", RequestType.PUT)]
		public Post UpdatePost(Post post)
		{
			return APIPut<Post>($"/posts/{post.ID}", post);
		}

		// DeletePost deletes a post from the provided post id string.
		[ApiRoute("/posts/{post_id}", RequestType.DELETE)]
		public void DeletePost(string post_id)
		{
			APIDelete($"/posts/{post_id}");
		}

		// PatchPost partially updates a post. Any missing fields are not updated.
		[ApiRoute("/posts/{post_id}/patch", RequestType.PUT)]
		public Post UpdatePost(string post_id, bool is_pinned = false, List<string> file_ids = null, bool has_reactions = false)
		{
			var obj = new { is_pinned = is_pinned, file_ids = file_ids, has_reactions = has_reactions };
			return APIPut<Post>($"/posts/{post_id}/patch", obj);
		}

		// PinPost pin a post based on provided post id string.
		[ApiRoute("/posts/{post_id}/pin", RequestType.POST)]
		public void PinPost(string post_id)
		{
			APIPost<StatusOK>($"/posts/{post_id}/pin", null);
		}

		// GetPostThread gets a post with all the other posts in the same thread.
		[ApiRoute("/posts/{post_id}/thread", RequestType.GET)]
		public PostList GetPostThread(string post_id)
		{
			return APIGet<PostList>($"/posts/{post_id}/thread");
		}

		// UnpinPost unpin a post based on provided post id string.
		[ApiRoute("/posts/{post_id}/unpin", RequestType.POST)]
		public void UnpinPost(string post_id)
		{
			APIPost<StatusOK>($"/posts/{post_id}/unpin", null);
		}

		// SearchPosts returns any posts with matching terms string.
		[ApiRoute("/teams/{team_id}/posts/search", RequestType.POST)]
		public PostList SearchPosts(string team_id, string terms, bool is_or_search)
		{
			var obj = new { terms = terms, is_or_search = is_or_search };
			return APIPost<PostList>($"/teams/{team_id}/posts/search", obj);
		}

		// GetFlaggedPostsForUser returns flagged posts of a user [in a team/channel] based on user id string.
		[ApiRoute("/users/{user_id}/posts/flagged", RequestType.GET)]
		public PostList GetFlaggedPostsForUser(string user_id, string in_team = "", string in_channel = "")
		{
			Dictionary<string, string> options = new Dictionary<string, string>();
			if (!string.IsNullOrWhiteSpace(in_team))
				options.Add("in_team", in_team);
			else if (!string.IsNullOrWhiteSpace(in_channel))
				options.Add("in_channel", in_channel);

			if (options.Count == 0)
				return APIGet<PostList>($"/users/{user_id}/posts/flagged");
			else
				return APIGet<PostList>($"/users/{user_id}/posts/flagged", options);
		}

		//Not implemented
		//[ApiRoute("/posts/{post_id}/files/info", RequestType.GET)]
		#endregion

		#region Preference Methods
		// GetPreferences returns the user's preferences.
		[ApiRoute("/users/{user_id}/preferences", RequestType.GET)]
		public List<Preference> GetPreferences(string user_id)
		{
			return APIGet<List<Preference>>($"/users/{user_id}/preferences");
		}

		// UpdatePreferences saves the user's preferences.
		[ApiRoute("/users/{user_id}/preferences", RequestType.PUT)]
		public bool UpdatePreferences(string user_id, List<Preference> preferences)
		{
			return APIPut<bool>($"/users/{user_id}/preferences", preferences);
		}

		// DeletePreferences deletes the user's preferences.
		[ApiRoute("/users/{user_id}/preferences/delete", RequestType.POST)]
		public bool DeletePreferences(string user_id, List<Preference> preferences)
		{
			return APIPost<bool>($"/users/{user_id}/preferences/delete", preferences);
		}

		// GetPreferencesByCategory returns the user's preferences from the provided category string.
		[ApiRoute("/users/{user_id}/preferences/{category}", RequestType.GET)]
		public List<Preference> GetPreferences(string user_id, string category)
		{
			return APIGet<List<Preference>>($"/users/{user_id}/preferences/{category}");
		}

		// GetPreferenceByCategoryAndName returns the user's preferences from the provided category and preference name string.
		[ApiRoute("/users/{user_id}/preferences/{category}/name/{name}", RequestType.GET)]
		public Preference GetPreference(string user_id, string category, string name)
		{
			return APIGet<Preference>($"/users/{user_id}/preferences/{category}/name/{name}");
		}
		#endregion

		#region System Methods
		// GetAudits returns a list of audits for the whole system.
		[ApiRoute("/audits", RequestType.GET)]
		public List<Audit> GetAudits(int page, int per_page)
		{
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
			return APIGet<List<Audit>>("/audits", options);
		}

		// InvalidateCaches will purge the cache and can affect the performance while is cleaning.
		[ApiRoute("/caches/invalidate", RequestType.POST)]
		public void InvalidateCaches()
		{
			APIPost<StatusOK>("/caches/invalidate", null);
		}

		// GetConfig will retrieve the server config with some sanitized items.
		[ApiRoute("/config", RequestType.GET)]
		public Config GetServerConfig()
		{
			return APIGet<Config>("/config");
		}

		// UpdateConfig will update the server configuration
		[ApiRoute("/config", RequestType.PUT)]
		public Config UpdateConfig(Config config)
		{
			return APIPut<Config>("/config", config);
		}

		// ReloadConfig will reload the server configuration.
		[ApiRoute("/config/reload", RequestType.POST)]
		public void ReloadConfig()
		{
			APIPost<StatusOK>("/config/reload", null);
		}

		// DatabaseRecycle will recycle the connections. Discard current connection and get new one.
		[ApiRoute("/database/recycle", RequestType.POST)]
		public void DatabaseRecycle()
		{
			APIPost<StatusOK>("/database/recycle", null);
		}

		// TestEmail will attempt to connect to the configured SMTP server.
		[ApiRoute("/email/test", RequestType.POST)]
		public void TestEmail()
		{
			APIPost<StatusOK>("/email/test", null);
		}

		// GetLogs page of logs as a string array.
		[ApiRoute("/logs", RequestType.GET)]
		public List<string> GetLogs(int page, int per_page)
		{
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
			return APIGet<List<string>>("/logs", options);
		}

		// PostLog is a convenience Web Service call so clients can log messages into
		// the server-side logs.  For example we typically log javascript error messages
		// into the server-side.  It returns the log message if the logging was successful.
		[ApiRoute("/logs", RequestType.POST)]
		public void PostLog(string log, string level)
		{
			var obj = new { level = level, message = log };
			APIPost<LogEntry>("/logs", obj);
		}

		// GetPing will ping the server and to see if it is up and running.
		[ApiRoute("/system/ping", RequestType.GET)]
		public void Ping()
		{
			APIGet<StatusOK>("/system/ping");
		}
		#endregion

		#region Command methods
		// CreateCommand will create a new command if the user have the right permissions.
		[ApiRoute("/commands", RequestType.POST)]
		public Command CreateCommand(Command command)
		{
			return APIPost<Command>("/commands", command);
		}

		// ListCommands will retrieve a list of commands available in the team.
		[ApiRoute("/commands", RequestType.GET)]
		public List<Command> ListCommands(string team_id, bool custom_only = false)
		{
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "team_id", team_id },
				{ "custom_only", custom_only.ToString() }
			};
			return APIGet<List<Command>>($"/commands", options);
		}

		// UpdateCommand updates a command based on the provided Command struct
		[ApiRoute("/commands/{command_id}", RequestType.PUT)]
		public Command UpdateCommand(Command command)
		{
			return APIPut<Command>($"/commands/{command.ID}", command);
		}

		// DeleteCommand deletes a command based on the provided command id string
		[ApiRoute("/commands/{command_id}", RequestType.DELETE)]
		public void DeleteCommand(string command_id)
		{
			APIDelete($"/commands/{command_id}");
		}

		// RegenCommandToken will create a new token if the user have the right permissions.
		[ApiRoute("/commands/{command_id}/regen_token", RequestType.PUT)]
		public string RegerateCommandToken(string command_id)
		{
			return APIPut<Token>($"/commands/{command_id}/regen_token", null).token;
		}

		// ListCommands will retrieve a list of commands available in the team.
		[ApiRoute("/teams/{team_id}/commands/autocomplete", RequestType.GET)]
		public List<Command> AutoCompleteCommands(string team_id)
		{
			return APIGet<List<Command>>($"/teams/{team_id}/commands/autocomplete");
		}
		#endregion

		#region Webhook Methods
		// CreateIncomingWebhook creates an incoming webhook for a channel.
		[ApiRoute("/hooks/incoming", RequestType.POST)]
		public IncomingWebook CreateIncomingWebhook(IncomingWebook hook)
		{
			return APIPost<IncomingWebook>("/hooks/incoming", hook);
		}

		// GetIncomingWebhooks returns a page of incoming webhooks on the system. Page counting starts at 0.
		[ApiRoute("/hooks/incoming", RequestType.GET)]
		public List<IncomingWebook> GetIncomingWebhooks(int page, int per_page, string team_id = "")
		{
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
			if (!string.IsNullOrWhiteSpace(team_id))
				options.Add("team_id", team_id);

			return APIGet<List<IncomingWebook>>("/hooks/incoming", options);
		}

		// GetIncomingWebhook returns an Incoming webhook given the hook ID
		[ApiRoute("/hooks/incoming/{hook_id}", RequestType.GET)]
		public IncomingWebook GetIncomingWebhook(string hook_id)
		{
			return APIGet<IncomingWebook>($"/hooks/incoming/{hook_id}");
		}

		// UpdateIncomingWebhook updates an incoming webhook for a channel.
		[ApiRoute("/hooks/incoming/{hook_id}", RequestType.PUT)]
		public IncomingWebook UpdateIncomingWebhook(IncomingWebook hook)
		{
			return APIPut<IncomingWebook>($"/hooks/incoming/{hook.ID}", hook);
		}

		// DeleteIncomingWebhook deletes and Incoming Webhook given the hook ID
		[ApiRoute("/hooks/incoming/{hook_id}", RequestType.DELETE)]
		public void DeleteIncomingWebhook(string hook_id)
		{
			APIDelete($"/hooks/incoming/{hook_id}");
		}

		// CreateOutgoingWebhook creates an outgoing webhook for a team or channel.
		[ApiRoute("/hooks/outgoing", RequestType.POST)]
		public OutgoingWebhook CreateOutgoingWebhook(OutgoingWebhook hook)
		{
			return APIPost<OutgoingWebhook>("/hooks/outgoing", hook);
		}

		// GetOutgoingWebhooks returns a page of outgoing webhooks on the system. Page counting starts at 0.
		[ApiRoute("/hooks/outgoing", RequestType.GET)]
		public List<OutgoingWebhook> GetOutgoingWebhooks(int page, int per_page, string team_id = "", string channel_id = "")
		{
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ "page", page.ToString() },
				{ "per_page", per_page.ToString() }
			};
			if (!string.IsNullOrWhiteSpace(team_id))
				options.Add("team_id", team_id);
			else if (!string.IsNullOrWhiteSpace(channel_id))
				options.Add("channel_id", channel_id);

			return APIGet<List<OutgoingWebhook>>("/hooks/outgoing", options);
		}

		// GetOutgoingWebhook outgoing webhooks on the system requested by Hook Id.
		[ApiRoute("/hooks/outgoing/{hook_id}", RequestType.GET)]
		public OutgoingWebhook GetOutgoingWebhook(string hook_id)
		{
			return APIGet<OutgoingWebhook>($"/hooks/outgoing/{hook_id}");
		}

		// UpdateOutgoingWebhook creates an outgoing webhook for a team or channel.
		[ApiRoute("/hooks/outgoing/{hook_id}", RequestType.PUT)]
		public OutgoingWebhook UpdateOutgoingWebhook(OutgoingWebhook hook)
		{
			return APIPut<OutgoingWebhook>($"/hooks/outgoing/{hook.ID}", hook);
		}

		// DeleteOutgoingWebhook delete the outgoing webhook on the system requested by Hook Id.
		[ApiRoute("/hooks/outgoing/{hook_id}", RequestType.DELETE)]
		public void DeleteOutgoingWebhook(string hook_id)
		{
			APIDelete($"/hooks/outgoing/{hook_id}");
		}

		// RegenOutgoingHookToken regenerate the outgoing webhook token.
		[ApiRoute("/hooks/outgoing/{hook_id}/regen_token", RequestType.POST)]
		public OutgoingWebhook RegenerateOutgoingWebhookToken(string hook_id)
		{
			return APIPost<OutgoingWebhook>($"/hooks/outgoing/{hook_id}/regen_token", null);
		}
		#endregion

		#region Status Methods
		// GetUsersStatusesByIds returns a list of users status based on the provided user ids.
		[ApiRoute("/users/status/ids", RequestType.POST)]
		public List<UserStatus> GetUserStatuses(List<string> user_ids)
		{
			return APIPost<List<UserStatus>>("/users/status/ids", user_ids);
		}

		// GetUserStatus returns a user based on the provided user id string.
		[ApiRoute("/users/{user_id}/status", RequestType.GET)]
		public UserStatus GetUserStatus(string user_id)
		{
			return APIGet<UserStatus>($"/users/{user_id}/status");
		}

		// UpdateUserStatus sets a user's status based on the provided user id string.
		[ApiRoute("/users/{user_id}/status", RequestType.PUT)]
		public UserStatus UpdateUserStatus(string user_id, UserStatus status)
		{
			return APIPut<UserStatus>($"/users/{user_id}/status", status);
		}
		#endregion

		#region Reaction Methods
		// SaveReaction saves an emoji reaction for a post. Returns the saved reaction if successful, otherwise an error will be returned.
		[ApiRoute("/reactions", RequestType.POST)]
		public Reaction CreateReaction(Reaction reaction)
		{
			return APIPost<Reaction>("/reactions", reaction);
		}

		// GetReactions returns a list of reactions to a post.
		[ApiRoute("/posts/{post_id}/reactions", RequestType.GET)]
		public List<Reaction> GetReactions(string post_id)
		{
			return APIGet<List<Reaction>>($"/posts/{post_id}/reactions");
		}
		#endregion

		#region Emoji Methods
		// GetEmojiList returns a list of custom emoji in the system.
		[ApiRoute("/emoji", RequestType.GET)]
		public List<Emoji> GetEmojiList()
		{
			return APIGet<List<Emoji>>("/emoji");
		}

		// DeleteEmoji delete an custom emoji on the provided emoji id string.
		[ApiRoute("/emoji/{emoji_id}", RequestType.DELETE)]
		public void DeleteEmoji(string emoji_id)
		{
			APIDelete($"/emoji/{emoji_id}");
		}

		[ApiRoute("/emoji/{emoji_id}", RequestType.GET)]
		public Emoji GetEmoji(string emoji_id)
		{
			return APIGet<Emoji>($"/emoji/{emoji_id}");
		}
		#endregion

		#region LDAP Methods
		// SyncLdap will force a sync with the configured LDAP server.
		[ApiRoute("/ldap/sync", RequestType.POST)]
		public void SyncLdap()
		{
			APIPost<StatusOK>("/ldap/sync", null);
		}

		// TestLdap will attempt to connect to the configured LDAP server and return OK if configured
		// correctly.
		[ApiRoute("/ldap/test", RequestType.POST)]
		public void TestLdap()
		{
			APIPost<StatusOK>("/ldap/test", null);
		}
		#endregion

		#region OAuth Methods
		// GetOAuthApps gets a page of registered OAuth 2.0 client applications with Mattermost acting as an OAuth 2.0 service provider.
		[ApiRoute("/oauth/apps", RequestType.GET)]
		public List<OAuthApp> GetOAuthApps(int page, int per_page)
		{
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{
					"page", page.ToString()
				},
				{
					"per_page", per_page.ToString()
				}
			};
			return APIGet<List<OAuthApp>>("/oauth/apps", options);
		}

		// CreateOAuthApp will register a new OAuth 2.0 client application with Mattermost acting as an OAuth 2.0 service provider.
		[ApiRoute("/oauth/apps", RequestType.POST)]
		public OAuthApp CreateOAuthApp(OAuthApp app)
		{
			return APIPost<OAuthApp>("/oauth/apps", app);
		}

		// DeleteOAuthApp deletes a registered OAuth 2.0 client application.
		[ApiRoute("/oauth/apps/{app_id}", RequestType.DELETE)]
		public void DeleteOAuthApp(string app_id)
		{
			APIDelete($"/oauth/apps/{app_id}");
		}

		// GetOAuthApp gets a registered OAuth 2.0 client application with Mattermost acting as an OAuth 2.0 service provider.
		[ApiRoute("/oauth/apps/{app_id}", RequestType.GET)]
		public OAuthApp GetOAuthApp(string app_id)
		{
			return APIGet<OAuthApp>($"/oauth/apps/{app_id}");
		}

		// GetOAuthAppInfo gets a sanitized version of a registered OAuth 2.0 client application with Mattermost acting as an OAuth 2.0 service provider.
		[ApiRoute("/oauth/apps/{app_id}/info", RequestType.GET)]
		public OAuthApp GetOAuthAppInfo(string app_id)
		{
			return APIGet<OAuthApp>($"/oauth/apps/{app_id}/info");
		}

		// RegenerateOAuthAppSecret regenerates the client secret for a registered OAuth 2.0 client application.
		[ApiRoute("/oauth/apps/{app_id}/regen_secret", RequestType.POST)]
		public OAuthApp RegenerateOAuthAppSecret(string app_id)
		{
			return APIPost<OAuthApp>($"/oauth/apps/{app_id}/regen_secret", null);
		}

		// GetAuthorizedOAuthAppsForUser gets a page of OAuth 2.0 client applications the user has authorized to use access their account.
		[ApiRoute("/users/{user_id}/oauth/apps/authorized", RequestType.GET)]
		public List<OAuthApp> GetAuthorizedOAuthAppsForUser(string user_id, int page, int per_page)
		{
			Dictionary<string, string> options = new Dictionary<string, string>()
			{
				{
					"page", page.ToString()
				},
				{
					"per_page", per_page.ToString()
				}
			};
			return APIGet<List<OAuthApp>>($"/users/{user_id}/oauth/apps/authorized", options);
		}
		#endregion

		#region SAML Methods
		// DeleteSamlIdpCertificate deletes the SAML IDP certificate from the server and updates the config to not use it and disable SAML.
		[ApiRoute("/saml/certificate/idp", RequestType.DELETE)]
		public void DeleteSamlIdpCertificate()
		{
			APIDelete("/saml/certificate/public");
		}

		// DeleteSamlPublicCertificate deletes the SAML IDP certificate from the server and updates the config to not use it and disable SAML.
		[ApiRoute("/saml/certificate/public", RequestType.DELETE)]
		public void DeleteSamlPublicCertificate()
		{
			APIDelete("/saml/certificate/public");
		}

		// DeleteSamlPrivateCertificate deletes the SAML IDP certificate from the server and updates the config to not use it and disable SAML.
		[ApiRoute("/saml/certificate/private", RequestType.DELETE)]
		public void DeleteSamlPrivateCertificate()
		{
			APIDelete("/saml/certificate/public");
		}

		// GetSamlCertificateStatus returns metadata for the SAML configuration.
		[ApiRoute("/saml/certificate/status", RequestType.GET)]
		public SamlCertificateStatus GetSamlCertificateStatus()
		{
			return APIGet<SamlCertificateStatus>("/saml/certificate/status");
		}

		// GetSamlMetadata returns metadata for the SAML configuration.
		[ApiRoute("/saml/metadata", RequestType.GET)]
		public string GetSamlMetadata()
		{
			return APIGet<string>("/saml/metadata");
		}
		#endregion

		#region Webrtc Methods
		// GetWebrtcToken returns a valid token, stun server and turn server with credentials to
		// use with the Mattermost WebRTC service.
		[ApiRoute("/webrtc/token", RequestType.GET)]
		public WebrtcInfoResponse GetWebrtcToken()
		{
			return APIGet<WebrtcInfoResponse>("/webrtc/token");
		}
		#endregion

		//Not implemented
		//[ApiRoute("/brand/image", RequestType.GET)] - image support needed
		//[ApiRoute("/brand/image", RequestType.POST)] - image support needed
		//[ApiRoute("/cluster/status", RequestType.GET)] - need to test received clusterinfo struct
		//[ApiRoute("/compliance/reports", RequestType.POST)] - file support needed
		//[ApiRoute("/compliance/reports/{report_id}", RequestType.GET)] - file support needed
		//[ApiRoute("/compliance/reports", RequestType.GET)] - file support needed
		//[ApiRoute("/compliance/reports/{report_id}/download", RequestType.GET)] - file support needed
		//[ApiRoute("/emoji", RequestType.POST)] - image support needed
		//[ApiRoute("/emoji/{emoji_id}/image", RequestType.GET)] - image support needed
		//[ApiRoute("/files", RequestType.POST)] - file support needed
		//[ApiRoute("/files/{file_id}", RequestType.GET)] - file support needed
		//[ApiRoute("/files/{file_id}/thumbnail", RequestType.GET)] - file support needed
		//[ApiRoute("/files/{file_id}/preview", RequestType.GET)] - file support needed
		//[ApiRoute("/files/{file_id}/info", RequestType.GET)] - file support needed
		//[ApiRoute("/files/{file_id}/link", RequestType.GET)] - file support needed
		//[ApiRoute("/files/{file_id}/public", RequestType.GET)] - file support needed
		//[ApiRoute("/license", RequestType.POST)]
		//[ApiRoute("/license", RequestType.DELETE)]
		//[ApiRoute("/license/settings", RequestType.GET)]
		//[ApiRoute("/saml/certificate/public", RequestType.POST)] - file support needed
		//[ApiRoute("/saml/certificate/private", RequestType.POST)] - file support needed
		//[ApiRoute("/saml/certificate/idp", RequestType.POST)] - file support needed
		//[ApiRoute("/websocket"), RequestType.GET)] - use Connect() instead
		//[ApiRoute("/webrtc/token", RequestType.GET)]
	}
}
