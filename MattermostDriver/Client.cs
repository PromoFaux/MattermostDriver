using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace MattermostDriver
{
	public class Client
	{
		internal static ILogger logger;
		private WebSocket socket;

		//Events
		public static Action WebsocketConnected;

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
			string rawdata = API.PostGetAuth(new AuthorizationRequest(username, password));

			//Connect to Websocket
			socket = new WebSocket(websocket);
			socket.OnOpen += OnWebsocketOpen;
			socket.OnMessage += OnWebsocketMessage;

			//Return Self information
			if (!string.IsNullOrWhiteSpace(rawdata))
				return JsonConvert.DeserializeObject<Self>(rawdata);
			else
				return null;
		}

		private void OnWebsocketOpen(object sender, EventArgs e)
		{
			WebsocketConnected?.Invoke();
		}

		private void OnWebsocketMessage(object sender, MessageEventArgs e)
		{
			throw new NotImplementedException();
		}
	}
}
