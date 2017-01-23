using System.Collections.Generic;

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
			public string comments;
			public string desktop;
			public string desktop_duration;
			public string desktop_sound;
			public string email;
			public string first_name;
			public string mention_keys;
			public string push;
			public string push_status;
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

	public class Post
	{
		public string id;
		public long create_at;
		public long update_at;
		public long delete_at;
		public string user_id;
		public string channel_id;
		public string root_id;
		public string parent_id;
		public string original_id;
		public string message;
		public string type;
		public PostProperties Props;
		public string hashtag;
		public List<string> filenames;
		public string pending_post_id;

		public class PostProperties
		{

		}
	}

	public class Team
	{
		public string id;
		public long create_at;
		public long update_at;
		public long delete_at;
		public string display_name;
		public string name;
		public string description;
		public string email;
		public string type;
		public string company_name;
		public string allowed_domains;
		public string invite_id;
		public bool allow_open_invite;
	}

	public class ClientConfig
	{
		public string AboutLink;
		public string AndroidAppDownloadLink;
		public string AppDownloadLink;
		public string AvailableLocales;
		public string BuildDate;
		public string BuildEnterpriseReady;
		public string BuildHash;
		public string BuildHashEnterprise;
		public string BuildNumber;
		public string DefaultClientLocale;
		public string EnableCommands;
		public string EnableCustomEmoji;
		public string EnableDeveloper;
		public string EnableDiagnostics;
		public string EnableEmailBatching;
		public string EnableIncomingWebhooks;
		public string EnableOAuthServiceProvider;
		public string EnableOnlyAdminIntegrations;
		public string EnableOpenServer;
		public string EnableOutgoingWebhooks;
		public string EnablePostIconOverride;
		public string EnablePostUsernameOverride;
		public string EnablePublicLink;
		public string EnableSignInWithEmail;
		public string EnableSignInWithUsername;
		public string EnableSignUpWithEmail;
		public string EnableSignUpWithGitLab;
		public string EnableTeamCreation;
		public string EnableTesting;
		public string EnableUserCreation;
		public string EnableWebrtc;
		public string GoogleDeveloperKey;
		public string HelpLink;
		public string IosAppDownloadLink;
		public string MaxFileSize;
		public string PrivacyPolicyLink;
		public string ProfileHeight;
		public string ProfileWidth;
		public string ReportAProblemLink;
		public string RequireEmailVerification;
		public string RestrictCustomEmojiCreation;
		public string RestrictDirectMessage;
		public string RestrictPrivateChannelCreation;
		public string RestrictPrivateChannelDeletion;
		public string RestrictPrivateChannelManagement;
		public string RestrictPublicChannelCreation;
		public string RestrictPublicChannelDeletion;
		public string RestrictPublicChannelManagement;
		public string RestrictTeamInvite;
		public string SQLDriverName;
		public string SegmentDeveloperKey;
		public string SendEmailNotifications;
		public string SendPushNotifications;
		public string ShowEmailAddress;
		public string SiteName;
		public string SiteURL;
		public string SupportEmail;
		public string TermsOfServiceLink;
		public string Version;
		public string WebsocketPort;
		public string WebsocketSecurePort;
	}

	public class Channel
	{
		public string id;
		public long create_at;
		public long update_at;
		public long delete_at;
		public string team_id;
		public string type;
		public string display_name;
		public string name;
		public string header;
		public string purpose;
		public long last_post_at;
		public int total_msg_count;
		public long extra_update_at;
		public string creator_id;
	}
}
