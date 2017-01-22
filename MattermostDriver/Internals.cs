using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MattermostDriver
{
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
}
