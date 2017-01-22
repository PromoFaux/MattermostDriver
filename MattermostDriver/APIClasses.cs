using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MattermostDriver
{
	public class Self : User
	{
		public bool email_verified;
		public bool allow_marketing;
		public UserNotifProps notify_props;
		public long last_password_update;

		public class UserNotifProps
		{
			public string channel;
			public string desktop;
			public string desktop_sound;
			public string email;
			public string first_name;
			public string mention_keys;
			public string push;
		}
	}

	public class User
	{
		public string id;
		public long create_at;
		public long update_at;
		public long delete_at;
		public string username;
		public string auth_data;
		public string auth_service;
		public string email;
		public string nickname;
		public string first_name;
		public string last_name;
		public string position;
		public string roles;
		public string locale;
	}
}
