namespace MattermostDriver
{
	//Event Handler Delegates
	public delegate void EventHandler();
	public delegate void HelloEventHandler(HelloEvent e);
	public delegate void StatusChangeEventHandler(StatusChangeEvent e);

	internal class AuthorizationRequest
	{
		public string login_id;
		public string password;

		public AuthorizationRequest(string l, string p)
		{
			login_id = l;
			password = p;
		}
	}

	internal class AppError
	{
		public string id;
		public string message;
		public string detailed_error;
		public string request_id;
		public string status_code;

		public override string ToString()
		{
			return $"ID: {id} | message: {message} | detailed_error: {detailed_error} | request_id: {request_id} | status_code: {status_code}";
		}
	}

	internal class AuthChallengeRequest
	{
		public int seq;
		public string action;
		public Data data;

		public AuthChallengeRequest(int s, string t)
		{
			seq = s;
			action = "authentication_challenge";
			data = new Data(t);
		}

		internal class Data
		{
			public string token;
			public Data(string t)
			{
				token = t;
			}
		}
	}

	internal class AuthResponse
	{
		public string status;
		public string seq_reply;
	}
}
