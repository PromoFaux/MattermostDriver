using System.Collections.Generic;

namespace MattermostDriver
{
	public class StatusOK
	{
		public string status;
	}

	public class Self : User
	{
		public bool email_verified;
		public bool allow_marketing;
		public NotificationProperties notify_props;
		public long last_password_update;
		public long last_picture_update;

		public class NotificationProperties
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

	public class AutoCompleteResponse
	{
		public List<User> in_channel;
		public List<User> out_of_channel;
	}

	public class TeamMember
	{
		public string team_id;
		public string user_id;
		public string roles;
		public long delete_at;
	}

	public class MessageCount
	{
		public string team_id;
		public int msg_count;
		public int mention_count;
	}

	public class TeamStats
	{
		public string team_id;
		public int total_member_count;
		public int active_member_count;
	}

	public class ChannelMember
	{
		public string channel_id;
		public string user_id;
		public string roles;
		public long last_viewed_at;
		public int msg_count;
		public int mention_count;
		public NotificationProperties notify_props;
		public long last_update_at;

		public class NotificationProperties
		{
			public string desktop;
			public string mark_unread;
		}
	}

	public class ChannelInfo
	{
		public Channel channel;
		public ChannelMember member;
	}

	public class ChannelStats
	{
		public string channel_id;
		public int member_count;
	}

	public class SearchResult
	{
		public List<string> order;
		public Dictionary<string, Post> posts;
	}

	public class Reaction
	{
		public string user_id;
		public string post_id;
		public string emoji_name;
		public long create_at;
	}

	public class Preference
	{
		public string user_id;
		public string category;
		public string name;
		public string value;
	}

	public class IncomingWebook
	{
		public string id;
		public long create_at;
		public long update_at;
		public long delete_at;
		public string channel_id;
		public string description;
		public string display_name;
	}

	public class Audit
	{
		public string id;
		public long create_at;
		public string user_id;
		public string action;
		public string extra_info;
		public string ip_address;
		public string session_id;
	}

	public class Config
	{
		public ServiceSetting ServiceSettings;
		public TeamSetting TeamSettings;
		public SqlSetting SqlSettings;
		public LogSetting LogSettings;
		public PasswordSetting PasswordSettings;
		public FileSetting FileSettings;
		public EmailSetting EmailSettings;
		public RateLimitSetting RateLimitSettings;
		public PrivacySetting PrivacySettings;
		public SupportSetting SupportSettings;
		public GitLabSetting GitLabSettings;
		public GoogleSetting GoogleSettings;
		public Office365Setting Office365Settings;
		public LdapSetting LdapSettings;
		public ComplianceSetting ComplianceSettings;
		public LocalizationSetting LocalizationSettings;
		public SamlSetting SamlSettings;
		public NativeAppSetting NativeAppSettings;
		public ClusterSetting ClusterSettings;
		public MetricsSetting MetricsSettings;
		public AnalyticsSetting AnalyticsSettings;
		public WebrtcSetting WebrtcSettings;

		public class ServiceSetting
		{
			public string SiteURL;
			public string ListenAddress;
			public string ConnectionSecurity;
			public string TLSCertFile;
			public string TLSKeyFile;
			public bool UseLetsEncrypt;
			public string LetsEncryptCertificateCacheFile;
			public bool Forward80To443;
			public int ReadTimeout;
			public int WriteTimeout;
			public int MaximumLoginAttempts;
			public string SegmentDeveloperKey;
			public string GoogleDeveloperKey;
			public bool EnableOAuthServiceProvider;
			public bool EnableIncomingWebhooks;
			public bool EnableOutgoingWebhooks;
			public bool EnableCommands;
			public bool EnableOnlyAdminIntegrations;
			public bool EnablePostUsernameOverride;
			public bool EnablePostIconOverride;
			public bool EnableTesting;
			public bool EnableDeveloper;
			public bool EnableSecurityFixAlert;
			public bool EnableInsecureOutgoingConnections;
			public bool EnableMultifactorAuthentication;
			public bool EnforceMultifactorAuthentication;
			public bool AllowCorsFrom;
			public int SessionLengthWebInDays;
			public int SessionLengthMobileInDays;
			public int SessionLengthSSOInDays;
			public int SessionCacheInMinutes;
			public int WebsocketSecurePort;
			public int WebsocketPort;
			public int WebserverMode;
			public int EnableCustomEmoji;
			public string RestrictCustomEmojiCreation;
		}

		public class TeamSetting
		{
			public string SiteName;
			public int MaxUsersPerTeam;
			public bool EnableTeamCreation;
			public bool EnableUserCreation;
			public bool EnableOpenServer;
			public string RestrictCreationToDomains;
			public bool EnableCustomBrand;
			public string CustomBrandText;
			public string CustomDescriptionText;
			public string RestrictDirectMessage;
			public string RestrictTeamInvite;
			public string RestrictPublicChannelManagement;
			public string RestrictPrivateChannelManagement;
			public string RestrictPublicChannelCreation;
			public string RestrictPrivateChannelCreation;
			public string RestrictPublicChannelDeletion;
			public string RestrictPrivateChannelDeletion;
			public int UserStatusAwayTimeout;
			public int MaxChannelsPerTeam;
			public int MaxNotificationsPerChannel;
		}

		public class SqlSetting
		{
			public string DriverName;
			public string DataSource;
			public List<string> DataSourceReplicas;
			public int MaxIdleConns;
			public int MaxOpenConns;
			public bool Trace;
			public string AtRestEncryptKey;
		}

		public class LogSetting
		{
			public bool EnableConsole;
			public string ConsoleLevel;
			public bool EnableFile;
			public string FileLevel;
			public string FileFormat;
			public string FileLocation;
			public bool EnableWebhookDebugging;
			public bool EnableDiagnostics;
		}

		public class PasswordSetting
		{
			public int MinimumLength;
			public bool Lowercase;
			public bool Number;
			public bool Uppercase;
			public bool Symbol;
		}

		public class FileSetting
		{
			public int MaxFileSize;
			public string DriverName;
			public string Directory;
			public bool EnablePublicLink;
			public string PublicLinkSalt;
			public int ThumbnailWidth;
			public int ThumbnailHeight;
			public int PreviewWidth;
			public int PreviewHeight;
			public int ProfileWidth;
			public int ProfileHeight;
			public string InitialFont;
			public string AmazonS3AccessKeyId;
			public string AmazonS3SecretAccessKey;
			public string AmazonS3Bucket;
			public string AmazonS3Region;
			public string AmazonS3Endpoint;
			public bool AmazonS3SSL;
		}

		public class EmailSetting
		{
			public bool EnableSignUpWithEmail;
			public bool EnableSignInWithEmail;
			public bool EnableSignInWithUsername;
			public bool SendEmailNotifications;
			public bool RequireEmailVerification;
			public string FeedbackName;
			public string FeedbackEmail;
			public string FeedbackOrganization;
			public string SMTPUsername;
			public string SMTPPassword;
			public string SMTPServer;
			public string SMTPPort;
			public string ConnectionSecurity;
			public string InviteSalt;
			public string PasswordResetSalt;
			public bool SendPushNotifications;
			public string PushNotificationServer;
			public string PushNotificationContents;
			public bool EnableEmailBatching;
			public int EmailBatchingBufferSize;
			public int EmailBatchingInterval;
		}

		public class RateLimitSetting
		{
			public bool Enable;
			public int PerSec;
			public int MaxBurst;
			public int MemoryStoreSize;
			public bool VaryByRemoteAddr;
			public string VaryByHeader;
		}

		public class PrivacySetting
		{
			public bool ShowEmailAddress;
			public bool ShowFullName;
		}

		public class SupportSetting
		{
			public string TermsOfServiceLink;
			public string PrivacyPolicyLink;
			public string AboutLink;
			public string HelpLink;
			public string ReportAProblemLink;
			public string SupportEmail;
		}

		public class GitLabSetting
		{
			public bool Enable;
			public string Secret;
			public string Id;
			public string Scope;
			public string AuthEndpoint;
			public string TokenEndpoint;
			public string UserApiEndpoint;
		}

		public class GoogleSetting
		{
			public bool Enable;
			public string Secret;
			public string Id;
			public string Scope;
			public string AuthEndpoint;
			public string TokenEndpoint;
			public string UserApiEndpoint;
		}

		public class Office365Setting
		{
			public bool Enable;
			public string Secret;
			public string Id;
			public string Scope;
			public string AuthEndpoint;
			public string TokenEndpoint;
			public string UserApiEndpoint;
		}

		public class LdapSetting
		{
			public bool Enable;
			public string LdapServer;
			public int LdapPort;
			public string ConnectionSecurity;
			public string BaseDN;
			public string BindUsername;
			public string BindPassword;
			public string UserFilter;
			public string FirstNameAttribute;
			public string LastNameAttribute;
			public string EmailAttribute;
			public string UsernameAttribute;
			public string NicknameAttribute;
			public string IdAttribute;
			public string PositionAttribute;
			public int SyncIntervalMinutes;
			public bool SkipCertificateVerification;
			public int QueryTimeout;
			public int MaxPageSize;
			public string LoginFieldName;
		}

		public class ComplianceSetting
		{
			public bool Enable;
			public string Directory;
			public bool EnableDaily;
		}

		public class LocalizationSetting
		{
			public string DefaultServerLocale;
			public string DefaultClientLocale;
			public string AvailableLocales;
		}

		public class SamlSetting
		{
			public bool Enable;
			public bool Verify;
			public bool Encrypt;
			public string IdpUrl;
			public string IdpDescriptorUrl;
			public string AssertionConsumerServiceURL;
			public string IdpCertificateFile;
			public string PublicCertificateFile;
			public string PrivateKeyFile;
			public string FirstNameAttribute;
			public string LastNameAttribute;
			public string EmailAttribute;
			public string UsernameAttribute;
			public string NicknameAttribute;
			public string LocaleAttribute;
			public string PositionAttribute;
			public string LoginButtonText;
		}

		public class NativeAppSetting
		{
			public string AppDownloadLink;
			public string AndroidAppDownloadLink;
			public string IosAppDownloadLink;
		}

		public class ClusterSetting
		{
			public bool Enable;
			public string InterNodeListenAddress;
			public List<string> InterNodeUrls;
		}

		public class MetricsSetting
		{
			public bool Enable;
			public int BlockProfileRate;
			public string ListenAddress;
		}

		public class AnalyticsSetting
		{
			public int MaxUsersForStatistics;
		}

		public class WebrtcSetting
		{
			public bool Enable;
			public string GatewayWebsocketUrl;
			public string GatewayAdminUrl;
			public string GatewayAdminSecret;
			public string StunURI;
			public string TurnURI;
			public string TurnUsername;
			public string TurnSharedKey;
		}
	}

	public class Analytic
	{
		public string name;
		public int value;
	}

	public class ChannelCounts
	{
		public Dictionary<string, int> counts;
		public Dictionary<string, long> update_times;
	}
}
