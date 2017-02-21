/*
 * TODO:
 * -Async-ify
 * -(Testing)
 * -License
 * -Nuget
 * 
 * -File/Image Support
 */

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
			ApiBase = url + "/api/v3";

			//Generate websocket url
			if (url.StartsWith("https"))
				WebsocketUrl = "wss" + url.Substring(5) + "/api/v3/users/websocket";
			else if (url.StartsWith("http"))
				WebsocketUrl = "ws" + url.Substring(4) + "/api/v3/users/websocket";
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

		private T APIPost<T>(string endpoint, object jsonbody, Dictionary<string, string> parameters = null)
		{
			RestRequest request = new RestRequest(endpoint, Method.POST);
			request.AddHeader("Content-Type", "application/json");
			if (!string.IsNullOrWhiteSpace(token))
				request.AddHeader("Authorization", "Bearer " + token);
			if (jsonbody != null)
				request.AddJsonBody(jsonbody);
			if (parameters != null)
			{
				foreach (var kvp in parameters)
					request.AddQueryParameter(kvp.Key, kvp.Value);
			}
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
			//awaiting_ok = true;
			socket.Send(JsonConvert.SerializeObject(request));
		}

		private void OnWebsocketMessage(object sender, MessageReceivedEventArgs e)
		{
			string rawdata = e.Message;

			try
			{
				var res = JsonConvert.DeserializeObject<ReplyACK>(rawdata);
				if (res.status == "OK" && res.seq_reply > 0)
				{
					if (res.seq_reply == 2)
						logger.Debug("Authentication challenge successful. Awaiting hello event.");
					else
						logger.Debug($"[{res.seq_reply.ToString()}] Reply OK.");
					return;
				}
				
			}
			catch
			{

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
			socket.Send(JsonConvert.SerializeObject(request));
		}
		#endregion

		#region Admin Methods
		[ApiRoute("/admin/analytics/{name}", RequestType.GET)]
		public List<Analytic> GetAnalytics(string name)
		{
			return APIGet<List<Analytic>>($"/admin/analytics/{name}");
		}

		[ApiRoute("/admin/analytics/{team_id}/{name}", RequestType.GET)]
		public List<Analytic> GetAnalyticsByTeam(string team_id, string name)
		{
			return APIGet<List<Analytic>>($"/admin/analytics/{team_id}/{name}");
		}

		[ApiRoute("/admin/audits", RequestType.GET)]
		public List<Audit> GetAudits()
		{
			return APIGet<List<Audit>>("/admin/audits");
		}

		[ApiRoute("/admin/cluster_status", RequestType.GET)]
		public List<ClusterInfo> GetClusterStatus()
		{
			return APIGet<List<ClusterInfo>>("/admin/cluster_status");
		}

		[ApiRoute("/admin/compliance_reports", RequestType.GET)]
		public List<Compliance> GetComplianceReports()
		{
			return APIGet<List<Compliance>>("/admin/compliance_reports");
		}

		[ApiRoute("/admin/config", RequestType.GET)]
		public Config GetServerConfig()
		{
			return APIGet<Config>("/admin/config");
		}

		[ApiRoute("/admin/invalidate_all_caches", RequestType.GET)]
		public bool InvalidateAllCaches()
		{
			return APIGet<StatusOK>("/admin/invalidate_all_caches") != null;
		}

		// Initiate immediate synchronization of LDAP users.
		// The synchronization will be performed asynchronously and this function will
		// always return OK unless you don't have permissions.
		// You must be the system administrator to use this function.
		[ApiRoute("/admin/ldap_sync_now", RequestType.POST)]
		public bool LdapSyncNow()
		{
			return APIPost<StatusOK>("/admin/ldap_sync_now", null) != null;
		}

		[ApiRoute("/admin/ldap_test", RequestType.POST)]
		public bool TestLdap(Config config)
		{
			return APIPost<StatusOK>($"/admin/ldap_test", config) != null;
		}

		[ApiRoute("/admin/logs", RequestType.GET)]
		public List<string> GetLogs()
		{
			return APIGet<List<string>>("/admin/logs");
		}

		[ApiRoute("/admin/recently_active_users/{team_id}", RequestType.GET)]
		public Dictionary<string, User> GetRecentlyActiveUsers(string team_id)
		{
			return APIGet<Dictionary<string, User>>($"/admin/recently_active_users/{team_id}");
		}

		// RecycleDatabaseConnection will attempt to recycle the database connections.
		// You must have the system admin role to call this method.  It will return status=OK
		// if it's successfully recycled the connections, otherwise check the returned error.
		[ApiRoute("/admin/recycle_db_conn", RequestType.GET)]
		public bool RecycleDBConn()
		{
			return APIGet<StatusOK>("/admin/recycle_db_conn") != null;
		}

		// ReloadConfig will reload the config.json file from disk.  Properties
		// requiring a server restart will still need a server restart.  You must
		// have the system admin role to call this method.  It will return status=OK
		// if it's successfully reloaded the config file, otherwise check the returned error.
		[ApiRoute("/admin/reload_config", RequestType.GET)]
		public bool ReloadServerConfig()
		{
			return APIGet<StatusOK>("/admin/reload_config") != null;
		}

		[ApiRoute("/admin/remove_certificate", RequestType.POST)]
		public bool RemoveCertificate(string filename)
		{
			var obj = new { filename = filename };
			return APIPost<StatusOK>("/admin/remove_certificate", obj) != null;
		}

		[ApiRoute("/admin/reset_mfa", RequestType.POST)]
		public bool AdminResetMFA(string user_id)
		{
			var obj = new { user_id = user_id };
			return APIPost<StatusOK>($"/admin/reset_mfa", obj) != null;
		}

		[ApiRoute("/admin/reset_password", RequestType.POST)]
		public bool AdminResetPassword(string user_id, string new_password)
		{
			var obj = new { user_id = user_id, new_password = new_password };
			return APIPost<StatusOK>("/admin/reset_password", obj) != null;
		}

		[ApiRoute("/admin/save_compliance_report", RequestType.POST)]
		public Compliance SaveComplianceReport(Compliance compliance)
		{
			return APIPost<Compliance>($"/admin/save_compliance_report", compliance);
		}

		[ApiRoute("/admin/save_config", RequestType.POST)]
		public bool SaveServerConfig(Config config)
		{
			return APIPost<StatusOK>("/admin/save_config", config) != null;
		}

		[ApiRoute("/admin/test_email", RequestType.POST)]
		public bool TestEmail(Config config)
		{
			return APIPost<Success>($"/admin/test_email", config) != null;
		}
		#endregion

		#region Channel Methods
		[ApiRoute("/teams/{team_id}/channels/", RequestType.GET)]
		public List<Channel> GetChannels(string team_id)
		{
			return APIGet<List<Channel>>($"/teams/{team_id}/channels/");
		}

		// AutocompleteChannels will return a list of open channels that match the provided
		// string. Must be authenticated.
		[ApiRoute("/teams/{team_id}/channels/autocomplete", RequestType.GET)]
		public List<Channel> AutoCompleteChannels(string team_id, string term)
		{
			Dictionary<string, string> obj = new Dictionary<string, string>()
			{
				{ "term", term }
			};
			return APIGet<List<Channel>>($"/teams/{team_id}/channels/autocomplete", obj);
		}

		[ApiRoute("/teams/{team_id}/channels/counts", RequestType.GET)]
		public ChannelCounts GetChannelCounts(string team_id)
		{
			return APIGet<ChannelCounts>($"/teams/{team_id}/channels/counts");
		}

		[ApiRoute("/teams/{team_id}/channels/create", RequestType.POST)]
		public Channel CreateChannel(Channel channel)
		{
			return APIPost<Channel>($"/teams/{channel.TeamID}/channels/create", channel);
		}

		[ApiRoute("/teams/{team_id}/channels/create_direct", RequestType.POST)]
		public Channel CreateDirectChannel(string team_id, string user_id)
		{
			var obj = new { user_id = user_id };
			return APIPost<Channel>($"/teams/{team_id}/channels/create_direct", obj);
		}

		[ApiRoute("/teams/{team_id}/channels/members", RequestType.GET)]
		public List<ChannelMember> GetMyChannelMembers(string team_id)
		{
			return APIGet<List<ChannelMember>>($"/teams/{team_id}/channels/members");
		}

		// GetMoreChannelsPage will return a page of open channels the user is not in based on
		// the provided offset and limit. Must be authenticated.
		[ApiRoute("/teams/{team_id}/channels/more/{offset}/{limit}", RequestType.GET)]
		public List<Channel> GetChannels(string team_id, int offset, int limit)
		{
			return APIGet<List<Channel>>($"/teams/{team_id}/channels/more/{offset}/{limit}");
		}

		// SearchMoreChannels will return a list of open channels the user is not in, that matches
		// the search criteria provided. Must be authenticated.
		[ApiRoute("/teams/{team_id}/channels/more/search", RequestType.POST)]
		public List<Channel> SearchChannels(string team_id, string term)
		{
			var obj = new { term = term };
			return APIPost<List<Channel>>($"/teams/{team_id}/channels/more/search", obj);
		}

		[ApiRoute("/teams/{team_id}/channels/name/{channel_name}", RequestType.GET)]
		public Channel GetChannelByName(string team_id, string channel_name)
		{
			return APIGet<Channel>($"/teams/{team_id}/channels/name/{channel_name}");
		}

		[ApiRoute("/teams/{team_id}/channels/name/{channel_name}/join", RequestType.POST)]
		public Channel JoinChannelByName(string team_id, string channel_name)
		{
			return APIPost<Channel>($"/teams/{team_id}/channels/name/{channel_name}/join", null);
		}

		[ApiRoute("/teams/{team_id}/channels/update", RequestType.POST)]
		public Channel UpdateChannel(Channel channel)
		{
			return APIPost<Channel>($"/teams/{channel.TeamID}/channels/update", channel);
		}

		[ApiRoute("/teams/{team_id}/channels/update_header", RequestType.POST)]
		public Channel UpdateChannelHeader(string team_id, string channel_id, string channel_header)
		{
			var obj = new { channel_id = channel_id, channel_header = channel_header };
			return APIPost<Channel>($"/teams/{team_id}/channels/update_header", obj);
		}

		[ApiRoute("/teams/{team_id}/channels/update_notify_props", RequestType.POST)]
		public ChannelMember.NotificationProperties UpdateChannelNotifProps(string team_id, string channel_id, string user_id, string mark_unread = "", string desktop = "")
		{
			bool hasunread = !string.IsNullOrWhiteSpace(mark_unread);
			bool hasdesktop = !string.IsNullOrWhiteSpace(desktop);

			if (hasunread && hasdesktop)
			{
				var obj = new { user_id = user_id, channel_id = channel_id, mark_unread = mark_unread, desktop = desktop };
				return APIPost<ChannelMember.NotificationProperties>($"/teams/{team_id}/channels/update_notify_props", obj);
			}
			else if (hasunread)
			{
				var obj = new { user_id = user_id, channel_id = channel_id, mark_unread = mark_unread };
				return APIPost<ChannelMember.NotificationProperties>($"/teams/{team_id}/channels/update_notify_props", obj);
			}
			else if (hasdesktop)
			{
				var obj = new { user_id = user_id, channel_id = channel_id, desktop = desktop };
				return APIPost<ChannelMember.NotificationProperties>($"/teams/{team_id}/channels/update_notify_props", obj);
			}
			else
			{
				logger.Error($"Error updating channel notification properties: No properties provided to change.");
				return null;
			}
		}

		[ApiRoute("/teams/{team_id}/channels/update_purpose", RequestType.POST)]
		public Channel UpdateChannelPurpose(string team_id, string channel_id, string channel_purpose)
		{
			var obj = new { channel_id = channel_id, channel_purpose = channel_purpose };
			return APIPost<Channel>($"/teams/{team_id}/channels/update_purpose", obj);
		}

		// ViewChannel performs all the actions related to viewing a channel. This includes marking
		// the channel and the previous one as read, and marking the channel as being actively viewed.
		// ChannelId is required but may be blank to indicate no channel is being viewed.
		// PrevChannelId is optional, populate to indicate a channel switch occurred.
		[ApiRoute("/teams/{team_id}/channels/view", RequestType.POST)]
		public bool ViewChannel(string team_id, string channel_id, string prev_channel_id = "")
		{
			var obj = new { channel_id = channel_id, prev_channel_id = prev_channel_id };
			return APIPost<StatusOK>($"/teams/{team_id}/channels/view", obj) != null;
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/", RequestType.GET)]
		public Channel GetChannel(string team_id, string channel_id)
		{
			return APIGet<Channel>($"/teams/{team_id}/channels/{channel_id}/");
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/add", RequestType.POST)]
		public ChannelMember AddChannelMember(string team_id, string channel_id, string user_id)
		{
			var obj = new { user_id = user_id };
			return APIPost<ChannelMember>($"/teams/{team_id}/channels/{channel_id}/add", obj);
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/delete", RequestType.POST)]
		public bool DeleteChannel(string team_id, string channel_id)
		{
			return APIPost<IDResponse>($"/teams/{team_id}/channels/{channel_id}/delete", null) != null;
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/join", RequestType.POST)]
		public Channel JoinChannel(string team_id, string channel_id)
		{
			return APIPost<Channel>($"/teams/{team_id}/channels/{channel_id}/join", null);
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/leave", RequestType.POST)]
		public bool LeaveChannel(string team_id, string channel_id)
		{
			return APIPost<IDResponse>($"/teams/{team_id}/channels/{channel_id}/leave", null) != null;
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/members/{user_id}", RequestType.GET)]
		public ChannelMember GetChannelMember(string team_id, string channel_id, string user_id)
		{
			return APIGet<ChannelMember>($"/teams/{team_id}/channels/{channel_id}/members/{user_id}");
		}

		// GetChannelMembersByIds will return channel member objects as an array based on the
		// channel id and a list of user ids provided. Must be authenticated.
		[ApiRoute("/teams/{team_id}/channels/{channel_id}/members/ids", RequestType.POST)]
		public List<ChannelMember> GetChannelMembersByIds(string team_id, string channel_id, List<string> user_ids)
		{
			return APIPost<List<ChannelMember>>($"/teams/{team_id}/channels/{channel_id}/members/ids", user_ids);
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/remove", RequestType.POST)]
		public bool RemoveUserFromChannel(string team_id, string channel_id, string user_id)
		{
			var obj = new { user_id = user_id };
			return APIPost<RemovedUser>($"/teams/{team_id}/channels/{channel_id}/remove", obj) != null;
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/stats", RequestType.GET)]
		public ChannelStats GetChannelStats(string team_id, string channel_id)
		{
			return APIGet<ChannelStats>($"/teams/{team_id}/channels/{channel_id}/stats");
		}
		#endregion

		#region Command Methods
		[ApiRoute("/teams/{team_id}/commands/create", RequestType.POST)]
		public Command CreateCommand(Command command)
		{
			return APIPost<Command>($"/teams/{command.TeamID}/commands/create", command);
		}

		[ApiRoute("/teams/{team_id}/commands/delete", RequestType.POST)]
		public bool DeleteCommand(string team_id, string command_id)
		{
			var obj = new { id = command_id };
			return APIPost<IDResponse>($"/teams/{team_id}/commands/delete", obj) != null;
		}

		[ApiRoute("/teams/{team_id}/commands/execute", RequestType.POST)]
		public CommandResponse ExecuteCommand(string team_id, string channel_id, string command)
		{
			var obj = new CommandArgs() { ChannelID = channel_id, Command = command };
			return APIPost<CommandResponse>($"/teams/{team_id}/commands/execute", obj);
		}

		[ApiRoute("/teams/{team_id}/commands/list", RequestType.GET)]
		public List<Command> ListCommands(string team_id)
		{
			return APIGet<List<Command>>($"/teams/{team_id}/commands/list");
		}

		[ApiRoute("/teams/{team_id}/commands/list_team_commands", RequestType.GET)]
		public List<Command> ListTeamCommands(string team_id)
		{
			return APIGet<List<Command>>($"/teams/{team_id}/commands/list_team_commands");
		}

		[ApiRoute("/teams/{team_id}/commands/regen_token", RequestType.POST)]
		public Command RegenCommandToken(string team_id, string command_id)
		{
			var obj = new { id = command_id };
			return APIPost<Command>($"/teams/{team_id}/commands/regen_token", obj);
		}

		[ApiRoute("/teams/{team_id}/commands/update", RequestType.POST)]
		public Command UpdateCommand(Command command)
		{
			return APIPost<Command>($"/teams/{command.TeamID}/commands/update", command);
		}
		#endregion

		#region Emoji Methods
		// DeleteEmoji will delete an emoji from the server if the current user has permission
		// to do so. If successful, it will return status=ok. Otherwise, an error will be returned.
		[ApiRoute("/emoji/delete", RequestType.POST)]
		public bool DeleteEmoji(string emoji_id)
		{
			var obj = new { id = emoji_id };
			return APIPost<StatusOK>("/emoji/delete", obj) != null;
		}

		// ListEmoji returns a list of all user-created emoji for the server.
		[ApiRoute("/emoji/list", RequestType.GET)]
		public List<Emoji> GetEmojis()
		{
			return APIGet<List<Emoji>>("/emoji/list");
		}
		#endregion

		#region General Methods
		// GetClientProperties returns properties needed by the client to show/hide
		// certian features.  It returns a map of strings.
		[ApiRoute("/general/client_props", RequestType.GET)]
		public ClientConfig GetClientProperties()
		{
			return APIGet<ClientConfig>("/general/client_props");
		}

		// LogClient is a convenience Web Service call so clients can log messages into
		// the server-side logs.  For example we typically log javascript error messages
		// into the server-side.  It returns true if the logging was successful.
		[ApiRoute("/general/log_client", RequestType.POST)]
		public bool CreateLogMessage(string message)
		{
			var obj = new { level = "ERROR", message = message };
			return APIPost<StatusOK>("/general/log_client", obj) != null;
		}

		// GetPing returns a map of strings with server time, server version, and node Id.
		// Systems that want to check on health status of the server should check the
		// url /api/v3/ping for a 200 status response.
		[ApiRoute("/general/ping", RequestType.GET)]
		public Pong Ping()
		{
			return APIGet<Pong>("/general/ping");
		}
		#endregion

		#region License Methods
		[ApiRoute("/license/client_config", RequestType.GET)]
		public License GetClientLicense()
		{
			return APIGet<License>($"/license/client_config");
		}

		[ApiRoute("/license/remove", RequestType.POST)]
		public bool DeleteLicense()
		{
			return APIPost<StatusOK>("/license/remove", null) != null;
		}
		#endregion

		#region OAuth Methods
		// AllowOAuth allows a new session by an OAuth2 App. On success
		// it returns the url to be redirected back to the app which initiated the oauth2 flow.
		// Must be authenticated as a user.
		[ApiRoute("/oauth/allow", RequestType.GET)]
		public string AllowOAuth(string response_type, string client_id, string redirect_uri, string scope, string state)
		{
			Dictionary<string, string> obj = new Dictionary<string, string>()
			{
				{ "response_type", response_type },
				{ "client_id", client_id },
				{ "redirect_uri", redirect_uri },
				{ "scope", scope },
				{ "state", state }
			};
			return APIGet<OAuthResponse>("/oauth/allow", obj).redirect;
		}

		// GetOAuthAppInfo lookup an OAuth2 App using the client_id. On success
		// it returns a Sanitized OAuth2 App. Must be authenticated as a user.
		[ApiRoute("/oauth/app/{client_id}", RequestType.GET)]
		public OAuthApp GetOAuthAppInfo(string client_id)
		{
			return APIGet<OAuthApp>($"/oauth/app/{client_id}");
		}

		// GetOAuthAuthorizedApps returns the OAuth2 Apps authorized by the user. On success
		// it returns a list of sanitized OAuth2 Authorized Apps by the user.
		[ApiRoute("/oauth/authorized", RequestType.GET)]
		public List<OAuthApp> GetOAuthAuthorizedApps()
		{
			return APIGet<List<OAuthApp>>("/oauth/authorized");
		}

		// DeleteOAuthApp deletes an OAuth2 app, the app must be deleted by the same user who created it or
		// a System Administrator. On success returs Status OK. Must be authenticated as a user.
		[ApiRoute("/oauth/delete", RequestType.POST)]
		public bool DeleteOAuthApp(string id)
		{
			var obj = new { id = id };
			return APIPost<StatusOK>("/oauth/delete", obj) != null;
		}

		// GetOAuthAppsByUser returns the OAuth2 Apps registered by the user. On success
		// it returns a list of OAuth2 Apps from the same user or all the registered apps if the user
		// is a System Administrator. Must be authenticated as a user.
		[ApiRoute("/oauth/list", RequestType.GET)]
		public List<OAuthApp> GetOAuthAppsByUser()
		{
			return APIGet<List<OAuthApp>>("/oauth/list");
		}

		// RegisterApp creates a new OAuth2 app to be used with the OAuth2 Provider. On success
		// it returns the created app. Must be authenticated as a user.
		[ApiRoute("/oauth/register", RequestType.POST)]
		public OAuthApp RegisterOAuthApp(OAuthApp app)
		{
			return APIPost<OAuthApp>("/oauth/register", app);
		}

		// OAuthDeauthorizeApp deauthorize a user an OAuth 2.0 app. On success
		// it returns status OK or an AppError on fail.
		[ApiRoute("/oauth/{client_id}/deauthorize", RequestType.POST)]
		public bool DeauthorizeOAuthApp(string client_id)
		{
			return APIPost<StatusOK>($"/oauth/{client_id}/deauthorize", null) != null;
		}

		// RegenerateOAuthAppSecret generates a new OAuth App Client Secret. On success
		// it returns an OAuth2 App. Must be authenticated as a user and the same user who
		// registered the app or a System Admin.
		[ApiRoute("/oauth/{client_id}/regen_secret", RequestType.POST)]
		public OAuthApp RegenerateOAuthAppSecret(string client_id)
		{
			return APIPost<OAuthApp>($"/oauth/{client_id}/regen_secret", null);
		}
		#endregion

		#region Post Methods
		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/create", RequestType.POST)]
		public Post CreatePost(string team_id, Post post)
		{
			return APIPost<Post>($"/teams/{team_id}/channels/{post.ChannelID}/posts/create", post);
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/page/{offset}/{limit}", RequestType.GET)]
		public PostList GetPosts(string team_id, string channel_id, int offset, int limit)
		{
			return APIGet<PostList>($"/teams/{team_id}/channels/{channel_id}/posts/page/{offset}/{limit}");
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/since/{time}", RequestType.GET)]
		public PostList GetPostsSince(string team_id, string channel_id, long time)
		{
			return APIGet<PostList>($"/teams/{team_id}/channels/{channel_id}/posts/since/{time}");
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/update", RequestType.POST)]
		public Post UpdatePost(string team_id, Post post)
		{
			return APIPost<Post>($"/teams/{team_id}/channels/{post.ChannelID}/posts/update", post);
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/after/{offset}/{limit}", RequestType.GET)]
		public PostList GetPostsAfter(string team_id, string channel_id, string post_id, int offset, int limit)
		{
			return APIGet<PostList>($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/after/{offset}/{limit}");
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/before/{offset}/{limit}", RequestType.GET)]
		public PostList GetPostsBefore(string team_id, string channel_id, string post_id, int offset, int limit)
		{
			return APIGet<PostList>($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/before/{offset}/{limit}");
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/delete", RequestType.POST)]
		public bool DeletePost(string team_id, string channel_id, string post_id)
		{
			return APIPost<IDResponse>($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/delete", null) != null;
		}

		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/get", RequestType.GET)]
		public PostList GetPost(string team_id, string channel_id, string post_id)
		{
			return APIGet<PostList>($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/get");
		}

		// GetFileInfosForPost returns a list of FileInfo objects for a given post id, if successful.
		// Otherwise, it returns an error.
		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/get_file_infos", RequestType.GET)]
		public List<FileInfo> GetFileInfos(string team_id, string channel_id, string post_id)
		{
			return APIGet<List<FileInfo>>($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/get_file_infos");
		}

		[ApiRoute("/teams/{team_id}/pltmp/{post_id}", RequestType.GET)]
		public PostList GetPermalinkTmp(string team_id, string post_id)
		{
			return APIGet<PostList>($"/teams/{team_id}/pltmp/{post_id}");
		}

		[ApiRoute("/teams/{team_id}/posts/{post_id}", RequestType.GET)]
		public PostList GetPostByID(string team_id, string post_id)
		{
			return APIGet<PostList>($"/teams/{team_id}/posts/{post_id}");
		}

		// GetFlaggedPosts will return a post list of posts that have been flagged by the user.
		// The page is set by the integer parameters offset and limit.
		[ApiRoute("/teams/{team_id}/posts/flagged/{offset}/{limit}", RequestType.GET)]
		public PostList GetFlaggedPosts(string team_id, int offset, int limit)
		{
			return APIGet<PostList>($"/teams/{team_id}/posts/flagged/{offset}/{limit}");
		}

		[ApiRoute("/teams/{team_id}/posts/search", RequestType.POST)]
		public PostList SearchPosts(string team_id, string terms, bool isOrSearch)
		{
			var obj = new { terms = terms, is_or_search = isOrSearch.ToString() };
			return APIPost<PostList>($"/teams/{team_id}/posts/search", obj);
		}
		#endregion

		#region Preferences Methods
		[ApiRoute("/preferences/", RequestType.GET)]
		public List<Preference> GetPreferences()
		{
			return APIGet<List<Preference>>($"/preferences/");
		}

		// DeletePreferences deletes a list of preferences owned by the current user. If successful,
		// it will return status=ok. Otherwise, an error will be returned.
		[ApiRoute("/preferences/delete", RequestType.POST)]
		public bool DeletePreferences(List<Preference> preferences)
		{
			return APIPost<StatusOK>($"/preferences/delete", preferences) != null;
		}

		[ApiRoute("/preferences/save", RequestType.POST)]
		public List<Preference> SavePreferences(List<Preference> preferences)
		{
			return APIPost<List<Preference>>("/preferences/save", preferences);
		}

		[ApiRoute("/preferences/{category_name}", RequestType.GET)]
		public List<Preference> GetPreferences(string category_name)
		{
			return APIGet<List<Preference>>($"/preferences/{category_name}");
		}

		[ApiRoute("/preferences/{category_name}/{pref_name}", RequestType.GET)]
		public Preference GetPreference(string category_name, string pref_name)
		{
			return APIGet<Preference>($"/preferences/{category_name}/{pref_name}");
		}
		#endregion

		#region Reaction Methods
		// Lists all emoji reactions made for the given post in the given channel. Returns a list of Reactions if successful, otherwise returns an AppError.
		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions", RequestType.GET)]
		public List<Reaction> GetReactions(string team_id, string channel_id, string post_id)
		{
			return APIGet<List<Reaction>>($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions");
		}

		// Removes an emoji reaction for a post in the given channel. Returns nil if successful, otherwise returns an AppError.
		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions/delete", RequestType.POST)]
		public bool DeleteReaction(string team_id, string channel_id, string post_id, Reaction reaction)
		{
			return APIPost<StatusOK>($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions/delete", reaction) != null;
		}

		// Saves an emoji reaction for a post in the given channel. Returns the saved reaction if successful, otherwise returns an AppError.
		[ApiRoute("/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions/save", RequestType.POST)]
		public Reaction CreateReaction(string team_id, string channel_id, string post_id, Reaction reaction)
		{
			return APIPost<Reaction>($"/teams/{team_id}/channels/{channel_id}/posts/{post_id}/reactions/save", reaction);
		}
		#endregion

		#region Team Methods
		// AddUserToTeamFromInvite adds a user to a team based off data provided in an invite link.
		// Either hash and dataToHash are required or inviteId is required.
		[ApiRoute("/teams/add_user_to_team_from_invite", RequestType.POST)]
		public Team AddUserToTeamFromInvite(string hash, string data, string invite_id)
		{
			var obj = new { hash = hash, data = data, invite_id = invite_id };
			return APIPost<Team>("/teams/add_user_to_team_from_invite", obj);
		}

		// GetAllTeams returns a map of all teams using team ids as the key.
		[ApiRoute("/teams/all", RequestType.GET)]
		public Dictionary<string, Team> GetTeams()
		{
			return APIGet<Dictionary<string, Team>>("/teams/all");
		}

		// GetAllTeamListings returns a map of all teams that are available to join
		// using team ids as the key. Must be authenticated.
		[ApiRoute("/teams/all_team_listings", RequestType.GET)]
		public Dictionary<string, Team> GetAvailableTeams()
		{
			return APIGet<Dictionary<string, Team>>("/teams/all_team_listings");
		}

		// CreateTeam creates a team based on the provided Team struct. On success it returns
		// the Team struct with the Id, CreateAt and other server-decided fields populated.
		[ApiRoute("/teams/create", RequestType.POST)]
		public Team CreateTeam(Team team)
		{
			return APIPost<Team>("/teams/create", team);
		}

		// FindTeamByName returns the strings "true" or "false" depending on if a team
		// with the provided name was found.
		[ApiRoute("/teams/find_team_by_name", RequestType.POST)]
		public bool CheckTeam(string team_name)
		{
			var obj = new { name = team_name };
			return bool.Parse(APIPost<string>("/teams/find_team_by_name", obj));
		}

		[ApiRoute("/teams/get_invite_info", RequestType.POST)]
		public InviteInfo GetInviteInfo(string invite_id)
		{
			var obj = new { invite_id = invite_id };
			return APIPost<InviteInfo>($"/teams/get_invite_info", obj);
		}

		// GetMyTeamMembers will return an array with team member objects that the current user
		// is a member of. Must be authenticated.
		[ApiRoute("/teams/members", RequestType.GET)]
		public List<TeamMember> GetMyTeamMembers()
		{
			return APIGet<List<TeamMember>>("/teams/members");
		}

		[ApiRoute("/teams/team_name/{team_name}", RequestType.GET)]
		public Team GetTeamByName(string team_name)
		{
			return APIGet<Team>($"/teams/team_name/{team_name}");
		}

		// GetMyTeamsUnread will return an array with TeamUnread objects that contain the amount of
		// unread messages and mentions the current user has for the teams it belongs to.
		// An optional team ID can be set to exclude that team from the results. Must be authenticated.
		[ApiRoute("/teams/unread", RequestType.GET)]
		public List<TeamUnread> GetMyTeamsUnread(string team_id = "")
		{
			Dictionary<string, string> obj = new Dictionary<string, string>();
			if (!string.IsNullOrWhiteSpace(team_id))
				obj.Add("id", team_id);
			if (obj.Count == 0)
				return APIGet<List<TeamUnread>>("/teams/unread");
			else
				return APIGet<List<TeamUnread>>("/teams/unread", obj);
		}

		//  Adds a user directly to the team without sending an invite.
		//  The teamId and userId are required.  You must be a valid member of the team and/or
		//  have the correct role to add new users to the team.  Returns a map of user_id=userId
		//  if successful, otherwise returns an AppError.
		[ApiRoute("/teams/{team_id}/add_user_to_team", RequestType.POST)]
		public bool AddUserToTeam(string team_id, string user_id)
		{
			var obj = new { user_id = user_id };

			return APIPost<UserIDResponse>($"/teams/{team_id}/add_user_to_team", obj) != null;
		}

		[ApiRoute("/teams/{team_id}/invite_members", RequestType.POST)]
		public List<Invite> InviteMembers(string team_id, List<Invite> invites)
		{
			return APIPost<List<Invite>>($"/teams/{team_id}/invite_members", invites);
		}

		[ApiRoute("/teams/{team_id}/me", RequestType.GET)]
		public Team GetMyTeam(string team_id)
		{
			return APIGet<Team>($"/teams/{team_id}/me");
		}

		// GetTeamMembersByIds will return team member objects as an array based on the
		// team id and a list of user ids provided. Must be authenticated.
		[ApiRoute("/teams/{team_id}/members/ids", RequestType.POST)]
		public List<TeamMember> GetTeamMembersByIds(string team_id, List<string> user_ids)
		{
			return APIPost<List<TeamMember>>($"/teams/{team_id}/members/ids", user_ids);
		}

		// GetTeamMember will return a team member object based on the team id and user id provided.
		// Must be authenticated.
		[ApiRoute("/teams/{team_id}/members/{user_id}", RequestType.GET)]
		public TeamMember GetTeamMember(string team_id, string user_id)
		{
			return APIGet<TeamMember>($"/teams/{team_id}/members/{user_id}");
		}

		// GetTeamMembers will return a page of team member objects as an array paged based on the
		// team id, offset and limit provided. Must be authenticated.
		[ApiRoute("/teams/{team_id}/members/{offset}/{limit}", RequestType.GET)]
		public List<TeamMember> GetTeamMembers(string team_id, int offset, int limit)
		{
			return APIGet<List<TeamMember>>($"/teams/{team_id}/members/{offset}/{limit}");
		}

		//  Removes a user directly from the team.
		//  The teamId and userId are required.  You must be a valid member of the team and/or
		//  have the correct role to remove a user from the team.  Returns a map of user_id=userId
		//  if successful, otherwise returns an AppError.
		[ApiRoute("/teams/{team_id}/remove_user_from_team", RequestType.POST)]
		public bool RemoveUserFromTeam(string team_id, string user_id)
		{
			var obj = new { user_id = user_id };
			return APIPost<UserIDResponse>($"/teams/{team_id}/remove_user_from_team", obj) != null;
		}

		// GetTeamStats will return a team stats object containing the number of users on the team
		// based on the team id provided. Must be authenticated.
		[ApiRoute("/teams/{team_id}/stats", RequestType.GET)]
		public TeamStats GetTeamStats(string team_id)
		{
			return APIGet<TeamStats>($"/teams/{team_id}/stats");
		}

		// UpdateTeam updates a team based on the changes in the provided team struct. On success
		// it returns a sanitized version of the updated team. Must be authenticated as a team admin
		// for that team or a system admin.
		[ApiRoute("/teams/{team_id}/update", RequestType.POST)]
		public Team UpdateTeam(Team team)
		{
			return APIPost<Team>($"/teams/{team.ID}/update", team);
		}

		[ApiRoute("/teams/{team_id}/update_member_roles", RequestType.POST)]
		public bool UpdateTeamRoles(string team_id, string user_id, string roles)
		{
			var obj = new { new_roles = roles, user_id = user_id };
			return APIPost<StatusOK>($"/teams/{team_id}/update_member_roles", obj) != null;
		}
		#endregion

		#region User Methods
		[ApiRoute("/users/attach_device", RequestType.POST)]
		public User AttachDeviceId(string device_id)
		{
			var obj = new { device_id = device_id };
			return APIPost<User>($"/users/attach_device", obj);
		}

		// AutocompleteUsers returns a list for autocompletion of users on the system that match the provided term,
		// matching against username, full name and nickname. Must be authenticated.
		[ApiRoute("/users/autocomplete", RequestType.GET)]
		public List<User> AutoCompleteUsers(string term)
		{
			Dictionary<string, string> obj = new Dictionary<string, string>()
			{
				{ "term", term }
			};
			return APIGet<List<User>>($"/users/autocomplete", obj);
		}

		[ApiRoute("/users/claim/email_to_ldap", RequestType.POST)]
		public string EmailToLdap(string email, string email_password, string ldap_id, string ldap_password, string token)
		{
			var obj = new { email = email, email_password = email_password, ldap_id = ldap_id, ldap_password = ldap_password, token = token };
			return APIPost<FollowLink>("/users/claim/email_to_ldap", obj).follow_link;
		}

		[ApiRoute("/users/claim/email_to_oauth", RequestType.POST)]
		public string EmailToOAuth(string password, string token, string service, string email)
		{
			var obj = new { password = password, token = token, service = service, email = email };
			return APIPost<FollowLink>("/users/claim/email_to_oauth", obj).follow_link;
		}

		[ApiRoute("/users/claim/ldap_to_email", RequestType.POST)]
		public string LdapToEmail(string email, string email_password, string ldap_password, string token)
		{
			var obj = new { email = email, email_password = email_password, ldap_password = ldap_password, token = token };
			return APIPost<FollowLink>("/users/claim/ldap_to_email", obj).follow_link;
		}

		[ApiRoute("/users/claim/oauth_to_email", RequestType.POST)]
		public string OAuthToEmail(string password, string email)
		{
			var obj = new { password = password, email = email };
			return APIPost<FollowLink>("/users/claim/oauth_to_email", obj).follow_link;
		}

		// CreateUser creates a user in the system based on the provided user struct.
		// CreateUserWithInvite creates a user based on the provided user struct. Either the hash and
		// data strings or the inviteId is required from the invite.
		[ApiRoute("/users/create", RequestType.POST)]
		public User CreateUser(User user, string data = "", string hash = "", string invite_id = "")
		{
			Dictionary<string, string> options = new Dictionary<string, string>();
			if (!string.IsNullOrWhiteSpace(data))
				options.Add("data", data);
			if (!string.IsNullOrWhiteSpace(hash))
				options.Add("hash", hash);
			if (!string.IsNullOrWhiteSpace(invite_id))
				options.Add("invite_id", invite_id);

			if (options.Count == 0)
				return APIPost<User>("/users/create", user);
			else
				return APIPost<User>("/users/create", user, options);
		}

		// getByEmail returns a user based on a provided username string. Must be authenticated.
		[ApiRoute("/users/email/{email}", RequestType.GET)]
		public User GetUserByEmail(string email)
		{
			return APIGet<User>($"/users/email/{email}");
		}

		// GenerateMfaSecret returns a QR code image containing the secret, to be scanned
		// by a multi-factor authentication mobile application. It also returns the secret
		// for manual entry. Must be authenticated.
		[ApiRoute("/users/generate_mfa_secret", RequestType.GET)]
		public string GenerateMfaSecret()
		{
			return APIGet<GetMFA>("/users/generate_mfa_secret").secret;
		}

		// GetProfilesByIds returns a map of users based on the user ids provided. Must
		// be authenticated.
		[ApiRoute("/users/ids", RequestType.POST)]
		public Dictionary<string, User> GetUsersByIds(List<string> ids)
		{
			return APIPost<Dictionary<string, User>>("/users/ids", ids);
		}

		[ApiRoute("/users/initial_load", RequestType.GET)]
		public InitialLoad GetInitialLoad()
		{
			return APIGet<InitialLoad>($"/users/initial_load");
		}

		// id - user id
		// login_id - username, email, SSO ident, etc.
		[ApiRoute("/users/login", RequestType.POST)]
		public Self Login(string password, string id = "", string login_id = "", string mfaToken = "", string device_id = "", bool ldap_only = false)
		{
			var obj = new { id = id, login_id = login_id, password = password, token = mfaToken, device_id = device_id, ldap_only = ldap_only.ToString() };
			return APIPostGetAuth(obj);
		}

		[ApiRoute("/users/logout", RequestType.POST)]
		public bool Logout()
		{
			token = "";
			return APIPost<UserIDResponse>("/users/logout", null) != null;
		}

		// GetMe returns the current user.
		[ApiRoute("/users/me", RequestType.GET)]
		public Self GetMe()
		{
			return APIGet<Self>("/users/me");
		}

		// CheckMfa returns a map with key "mfa_required" with the string value "true" or "false",
		// indicating whether MFA is required to log the user in, based on a provided login id
		// (username, email or some sort of SSO identifier based on configuration).
		[ApiRoute("/users/mfa", RequestType.POST)]
		public bool CheckMfa(string login_id)
		{
			var obj = new { login_id = login_id };
			return bool.Parse(APIPost<CheckMFA>("/users/mfa", obj).mfa_required);
		}

		// getByUsername returns a user based on a provided username string. Must be authenticated.
		[ApiRoute("/users/name/{username}", RequestType.GET)]
		public User GetUserByName(string username)
		{
			return APIGet<User>($"/users/name/{username}");
		}

		[ApiRoute("/users/newpassword", RequestType.POST)]
		public bool UpdateUserPassword(string user_id, string current_password, string new_password)
		{
			var obj = new { user_id = user_id, current_password = current_password, new_password = new_password };
			return APIPost<UserIDResponse>("/users/newpassword", obj) != null;
		}

		[ApiRoute("/users/reset_password", RequestType.POST)]
		public bool ResetPassword(string new_password, string code)
		{
			var obj = new { code = code, new_password = new_password };
			return APIPost<StatusOK>("/users/reset_password", obj) != null;
		}

		[ApiRoute("/users/revoke_session", RequestType.POST)]
		public bool RevokeSession(string sessionAltId)
		{
			var obj = new { id = sessionAltId };
			return APIPost<IDResponse>("/users/revoke_session", obj) != null;
		}

		// SearchUsers returns a list of users that have a username matching or similar to the search term. Must
		// be authenticated.
		[ApiRoute("/users/search", RequestType.POST)]
		public List<User> SearchUsers(string term, string team_id = "", string in_channel_id = "", string not_in_channel_id = "", bool allow_inactive = false)
		{
			var obj = new { term = term, team_id = team_id, in_channel_id = in_channel_id, not_in_channel_id = not_in_channel_id, allow_inactive = allow_inactive };
			return APIPost<List<User>>("/users/search", obj);
		}

		[ApiRoute("/users/send_password_reset", RequestType.POST)]
		public bool SendPasswordReset(string email)
		{
			var obj = new { email = email };
			return APIPost<EmailResponse>("/users/send_password_reset", obj) != null;
		}

		[ApiRoute("/users/update", RequestType.POST)]
		public User UpdateUser(User user)
		{
			return APIPost<User>($"/users/update", user);
		}

		[ApiRoute("/users/update_active", RequestType.POST)]
		public User UpdateActive(string user_id, bool active)
		{
			var obj = new { user_id = user_id, active = active.ToString() };
			return APIPost<User>($"/users/update_active", obj);
		}

		// UpdateMfa activates multi-factor authenticates for the current user if activate
		// is true and a valid token is provided. If activate is false, then token is not
		// required and multi-factor authentication is disabled for the current user.
		[ApiRoute("/users/update_mfa", RequestType.POST)]
		public bool UpdateMfa(bool activate, string mfaToken)
		{
			var obj = new { activate = activate, token = mfaToken };
			return APIPost<StatusOK>("/users/update_mfa", obj) != null;
		}

		[ApiRoute("/users/update_notify", RequestType.POST)]
		public User UpdateUserNotify(string user_id, string email, string desktop_sound, string desktop, string comments)
		{
			var obj = new { user_id = user_id, email = email, desktop_sound = desktop_sound, desktop = desktop, comments = comments };
			return APIPost<User>($"/users/update_notify", obj);
		}

		// GetProfiles returns a map of users using user id as the key. Must be authenticated.
		[ApiRoute("/users/{offset}/{limit}", RequestType.GET)]
		public Dictionary<string, User> GetUsers(int offset, int limit)
		{
			return APIGet<Dictionary<string, User>>($"/users/{offset}/{limit}");
		}

		[ApiRoute("/users/{user_id}/audits", RequestType.GET)]
		public List<Audit> GetAudits(string user_id)
		{
			return APIGet<List<Audit>>($"/users/{user_id}/audits");
		}

		// GetUser returns a user based on a provided user id string. Must be authenticated.
		[ApiRoute("/users/{user_id}/get", RequestType.GET)]
		public User GetUserById(string user_id)
		{
			return APIGet<User>($"/users/{user_id}/get");
		}

		[ApiRoute("/users/{user_id}/sessions", RequestType.GET)]
		public List<Session> GetSessions(string user_id)
		{
			return APIGet<List<Session>>($"/users/{user_id}/sessions");
		}

		[ApiRoute("/users/{user_id}/update_roles", RequestType.POST)]
		public bool UpdateUserRoles(string user_id, string roles)
		{
			var obj = new { new_roles = roles };
			return APIPost<StatusOK>($"/users/{user_id}/update_roles", obj) != null;
		}

		// GetProfilesInTeam returns a map of users for a team using user id as the key. Must
		// be authenticated.
		[ApiRoute("/teams/{team_id}/users/{offset}/{limit}", RequestType.GET)]
		public Dictionary<string, User> GetUsersInTeam(string team_id, int offset, int limit)
		{
			return APIGet<Dictionary<string, User>>($"/teams/{team_id}/users/{offset}/{limit}");
		}

		// AutocompleteUsersInTeam returns a list for autocompletion of users in a team. The list "in_team" specifies
		// the users in the team that match the provided term, matching against username, full name and
		// nickname. Must be authenticated.
		[ApiRoute("/teams/{team_id}/users/autocomplete", RequestType.GET)]
		public TeamAutoComplete AutoCompleteUsers(string team_id, string term)
		{
			Dictionary<string, string> obj = new Dictionary<string, string>()
			{
				{ "term", term }
			};
			return APIGet<TeamAutoComplete>($"/teams/{team_id}/users/autocomplete", obj);
		}

		// GetProfilesInChannel returns a map of users for a channel using user id as the key. Must
		// be authenticated.
		[ApiRoute("/teams/{team_id}/channels/{channel_id}/users/{offset}/{limit}", RequestType.GET)]
		public Dictionary<string, User> GetUsersInChannel(string team_id, string channel_id, int offset, int limit)
		{
			return APIGet<Dictionary<string, User>>($"/teams/{team_id}/channels/{channel_id}/users/{offset}/{limit}");
		}

		// AutocompleteUsersInChannel returns two lists for autocompletion of users in a channel. The first list "in_channel",
		// specifies users in the channel. The second list "out_of_channel" specifies users outside of the
		// channel. Term, the string to search against, is required, channel id is also required. Must be authenticated.
		[ApiRoute("/teams/{team_id}/channels/{channel_id}/users/autocomplete", RequestType.GET)]
		public ChannelAutoComplete AutoCompleteUsers(string team_id, string channel_id, string term)
		{
			Dictionary<string, string> obj = new Dictionary<string, string>()
			{
				{ "term", term }
			};
			return APIGet<ChannelAutoComplete>($"/teams/{team_id}/channels/{channel_id}/users/autocomplete", obj);
		}

		// GetProfilesNotInChannel returns a map of users not in a channel but on the team using user id as the key. Must
		// be authenticated.
		[ApiRoute("/teams/{team_id}/channels/{channel_id}/users/not_in_channel/{offset}/{limit}", RequestType.GET)]
		public Dictionary<string, User> GetUsersNotInChannel(string team_id, string channel_id, int offset, int limit)
		{
			return APIGet<Dictionary<string, User>>($"/teams/{team_id}/channels/{channel_id}/users/not_in_channel/{offset}/{limit}");
		}
		#endregion

		#region Webhook Methods
		[ApiRoute("/teams/{team_id}/hooks/incoming/create", RequestType.POST)]
		public IncomingWebook CreateIncomingWebhook(IncomingWebook hook)
		{
			return APIPost<IncomingWebook>($"/teams/{hook.TeamID}/hooks/incoming/create", hook);
		}

		[ApiRoute("/teams/{team_id}/hooks/incoming/delete", RequestType.POST)]
		public bool DeleteIncomingWebhook(string team_id, string hook_id)
		{
			var obj = new { id = hook_id };
			return APIPost<IDResponse>($"/teams/{team_id}/hooks/incoming/delete", obj) != null;
		}

		[ApiRoute("/teams/{team_id}/hooks/incoming/list", RequestType.GET)]
		public List<IncomingWebook> ListIncomingWebhooks(string team_id)
		{
			return APIGet<List<IncomingWebook>>($"/teams/{team_id}/hooks/incoming/list");
		}

		[ApiRoute("/teams/{team_id}/hooks/outgoing/create", RequestType.POST)]
		public OutgoingWebhook CreateOutgoingWebhook(OutgoingWebhook hook)
		{
			return APIPost<OutgoingWebhook>($"/teams/{hook.TeamID}/hooks/outgoing/create", hook);
		}

		[ApiRoute("/teams/{team_id}/hooks/outgoing/delete", RequestType.POST)]
		public bool DeleteOutgoingWebhook(string team_id, string hook_id)
		{
			var obj = new { id = hook_id };
			return APIPost<IDResponse>($"/teams/{team_id}/hooks/outgoing/delete", obj) != null;
		}

		[ApiRoute("/teams/{team_id}/hooks/outgoing/list", RequestType.GET)]
		public List<OutgoingWebhook> ListOutgoingWebhooks(string team_id)
		{
			return APIGet<List<OutgoingWebhook>>($"/teams/{team_id}/hooks/outgoing/list");
		}

		[ApiRoute("/teams/{team_id}/hooks/outgoing/regen_token", RequestType.POST)]
		public OutgoingWebhook RegenOutgoingWehookToken(string team_id, string hook_id)
		{
			var obj = new { id = hook_id };
			return APIPost<OutgoingWebhook>($"/teams/{team_id}/hooks/outgoing/regen_token", obj);
		}
		#endregion

		//Not implemented
		//[ApiRoute("/admin/add_certificate", RequestType.POST)]
		//[ApiRoute("/admin/download_compliance_report/{id}", RequestType.GET)]
		//[ApiRoute("/admin/get_brand_image", RequestType.GET)]
		//[ApiRoute("/admin/saml_cert_status", RequestType.GET)]
		//[ApiRoute("/admin/saml_metadata", RequestType.GET)]
		//[ApiRoute("/admin/upload_brand_image", RequestType.POST)]
		//[ApiRoute("/emoji/create", RequestType.POST)]
		//[ApiRoute("/emoji/{emoji_id}", RequestType.GET)]
		//[ApiRoute("/files/{file_id}/get", RequestType.GET)]
		//[ApiRoute("/files/{file_id}/get_thumbnail", RequestType.GET)]
		//[ApiRoute("/files/{file_id}/get_preview", RequestType.GET)]
		//[ApiRoute("/files/{file_id}/get_public_link", RequestType.GET)]
		//[ApiRoute("/license/add", RequestType.POST)]
		//[ApiRoute("/oauth/access_token", RequestType.POST)]
		//[ApiRoute("/public/files/{file_id}/get", RequestType.GET)]
		//[ApiRoute("/public/files/get/{team_id}/{channel_id}/{user_id}/{file_id}", RequestType.GET)]
		//[ApiRoute("/teams/{team_id}/channels/more", RequestType.GET)] - Deprecated
		//[ApiRoute("/teams/{team_id}/files/upload", RequestType.POST)]
		//[ApiRoute("/users/newimage", RequestType.POST)]
		//[ApiRoute("/users/status", RequestType.GET)]
		//[ApiRoute("/users/status/ids", RequestType.POST)]
		//[ApiRoute("/users/{user_id}/image", RequestType.GET)]
	}
}
