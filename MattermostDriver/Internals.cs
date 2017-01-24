using System;

namespace MattermostDriver
{
	//Event Handler Delegates
	public delegate void EventHandler();
	public delegate void HelloEventHandler(HelloEvent e);
	public delegate void StatusChangeEventHandler(StatusChangeEvent e);
	public delegate void TypingEventHandler(TypingEvent e);
	public delegate void PostedEventHandler(PostedEvent e);
	public delegate void NewUserEventHandler(NewUserEvent e);
	public delegate void ChannelDeletedEventHandler(ChannelDeletedEvent e);
	public delegate void DirectAddedEventHandler(DirectAddedEvent e);

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class ApiRoute : Attribute
	{
		string route;
		RequestType requestType;

		public ApiRoute(string route, RequestType requestType)
		{
			this.route = route;
			this.requestType = requestType;
		}
	}

	public enum RequestType
	{
		POST,
		GET
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
}
