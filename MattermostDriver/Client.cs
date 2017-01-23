using Newtonsoft.Json;
using System;
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
		/// <summary>
		/// Thrown when the websocket is successfully connected.
		/// </summary>
		public event EventHandler WebsocketConnected;
		/// <summary>
		/// Thrown when a Hello websocket event is received.
		/// </summary>
		public event HelloEventHandler Hello;
		/// <summary>
		/// Thrown when a Status Change websocket event is received.
		/// </summary>
		public event StatusChangeEventHandler StatusChange;
		/// <summary>
		/// Thrown when a Typing websocket event is received.
		/// </summary>
		public event TypingEventHandler Typing;
		/// <summary>
		/// Thrown when a Posted websocket event is received.
		/// </summary>
		public event PostedEventHandler Posted;
		/// <summary>
		/// Thrown when a New User websocket event is received.
		/// </summary>
		public event NewUserEventHandler NewUser;
		#endregion

		/// <summary>
		/// Authenticates and connects to the server's websocket.
		/// </summary>
		/// <param name="url">The base URL of the Mattermost server.</param>
		/// <param name="username">The login ID (email/username/AD/LDAP ID).</param>
		/// <param name="password">The account password.</param>
		/// <param name="logger">An implementation of the ILogger interface.</param>
		/// <returns></returns>
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
		/// <summary>
		/// Creates a new user.
		/// </summary>
		/// <param name="email">Required - The new user's email address.</param>
		/// <param name="username">Required - The new user's username.</param>
		/// <param name="password">Required - The new user's password.</param>
		/// <param name="first_name">Optional - The new user's first name.</param>
		/// <param name="last_name">Optional - The new user's last name.</param>
		/// <param name="nickname">Optional - The new user's nickname.</param>
		/// <param name="locale">Optional - The new user's locale.</param>
		/// <returns></returns>
		public User CreateUser(string email, string username, string password, string first_name = "", string last_name = "", string nickname = "", string locale = "")
		{
			var createUserRequest = new { email = email, username = username, password = password, first_name = first_name, last_name = last_name, nickname = nickname, locale = locale };
			string rawdata = API.Post($"/users/create", createUserRequest);
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<User>(rawdata);
			else
				return null;
		}
		#endregion
	}
}
