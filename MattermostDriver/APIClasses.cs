using System.Collections.Generic;
using Newtonsoft.Json;

namespace MattermostDriver
{
	public class Analytic
	{
		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "value")]
		public int Value { get; set; }
	}

	public class Audit
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "user_id")]
		public string UserID { get; set; }

		[JsonProperty(PropertyName = "action")]
		public string Action { get; set; }

		[JsonProperty(PropertyName = "extra_info")]
		public string ExtraInfo { get; set; }

		[JsonProperty(PropertyName = "ip_address")]
		public string IPAddress { get; set; }

		[JsonProperty(PropertyName = "session_id")]
		public string SessionID { get; set; }
	}

	public class Channel
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "update_at")]
		public long UpdateAt { get; set; }

		[JsonProperty(PropertyName = "delete_at")]
		public long DeleteAt { get; set; }

		[JsonProperty(PropertyName = "team_id")]
		public string TeamID { get; set; }

		[JsonProperty(PropertyName = "type")]
		public string Type { get; set; }

		[JsonProperty(PropertyName = "display_name")]
		public string DisplayName { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "header")]
		public string Header { get; set; }

		[JsonProperty(PropertyName = "purpose")]
		public string Purpose { get; set; }

		[JsonProperty(PropertyName = "last_post_at")]
		public long LastPostAt { get; set; }

		[JsonProperty(PropertyName = "total_msg_count")]
		public int TotalMsgCount { get; set; }

		[JsonProperty(PropertyName = "extra_update_at")]
		public long ExtraUpdateAt { get; set; }

		[JsonProperty(PropertyName = "creator_id")]
		public string CreatorID { get; set; }
	}

	public class ChannelAutoComplete
	{
		[JsonProperty(PropertyName = "in_channel")]
		public List<User> InChannel { get; set; }

		[JsonProperty(PropertyName = "out_of_channel")]
		public List<User> OutOfChannel { get; set; }
	}

	public class ChannelCounts
	{
		[JsonProperty(PropertyName = "counts")]
		public Dictionary<string, int> Counts { get; set; }

		[JsonProperty(PropertyName = "update_times")]
		public Dictionary<string, long> UpdateTimes { get; set; }
	}

	public class ChannelInfo
	{
		[JsonProperty(PropertyName = "channel")]
		public Channel Channel { get; set; }

		[JsonProperty(PropertyName = "member")]
		public ChannelMember Member { get; set; }
	}

	public class ChannelMember
	{
		[JsonProperty(PropertyName = "channel_id")]
		public string ChannelID { get; set; }

		[JsonProperty(PropertyName = "user_id")]
		public string UserID { get; set; }

		[JsonProperty(PropertyName = "roles")]
		public string Roles { get; set; }

		[JsonProperty(PropertyName = "last_viewed_at")]
		public long LastViewedAt { get; set; }

		[JsonProperty(PropertyName = "msg_count")]
		public int MsgCount { get; set; }

		[JsonProperty(PropertyName = "mention_count")]
		public int MentionCount { get; set; }

		[JsonProperty(PropertyName = "notify_props")]
		public NotificationProperties NotificationProps { get; set; }

		[JsonProperty(PropertyName = "last_update_at")]
		public long LastUpdateAt { get; set; }

		public class NotificationProperties
		{
			[JsonProperty(PropertyName = "desktop")]
			public string Desktop { get; set; }

			[JsonProperty(PropertyName = "mark_unread")]
			public string MarkUnread { get; set; }
		}
	}

	public class ChannelStats
	{
		[JsonProperty(PropertyName = "channel_id")]
		public string ChannelID { get; set; }

		[JsonProperty(PropertyName = "member_count")]
		public int MemberCount { get; set; }
	}

	public class ChannelUnread
	{
		[JsonProperty(PropertyName = "team_id")]
		public string TeamID { get; set; }

		[JsonProperty(PropertyName = "channel_id")]
		public string ChannelID { get; set; }

		[JsonProperty(PropertyName = "msg_count")]
		public long MsgCount { get; set; }

		[JsonProperty(PropertyName = "mention_count")]
		public long MentionCount { get; set; }

		//public NotificationProperties NotificationProps { get; set; }
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

	public class ClusterInfo
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "version")]
		public string Version { get; set; }

		[JsonProperty(PropertyName = "config_hash")]
		public string ConfigHash { get; set; }

		[JsonProperty(PropertyName = "internode_url")]
		public string InternodeUrl { get; set; }

		[JsonProperty(PropertyName = "hostname")]
		public string Hostname { get; set; }

		[JsonProperty(PropertyName = "last_ping")]
		public long LastPing { get; set; }

		[JsonProperty(PropertyName = "is_alive")]
		public string IsAlive { get; set; }
	}

	public class Command
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "token")]
		public string Token { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "update_at")]
		public long UpdateAt { get; set; }

		[JsonProperty(PropertyName = "delete_at")]
		public long DeleteAt { get; set; }

		[JsonProperty(PropertyName = "creator_id")]
		public string CreatorID { get; set; }

		[JsonProperty(PropertyName = "team_id")]
		public string TeamID { get; set; }

		[JsonProperty(PropertyName = "trigger")]
		public string Trigger { get; set; }

		[JsonProperty(PropertyName = "method")]
		public string Method { get; set; }

		[JsonProperty(PropertyName = "username")]
		public string Username { get; set; }

		[JsonProperty(PropertyName = "icon_url")]
		public string IconURL { get; set; }

		[JsonProperty(PropertyName = "auto_complete")]
		public bool AutoComplete { get; set; }

		[JsonProperty(PropertyName = "auto_complete_desc")]
		public string AutoCompleteDesc { get; set; }

		[JsonProperty(PropertyName = "auto_complete_hint")]
		public string AutoCompleteHint { get; set; }

		[JsonProperty(PropertyName = "display_name")]
		public string DisplayName { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "url")]
		public string URL { get; set; }
	}

	public class CommandArgs
	{
		[JsonProperty(PropertyName = "channel_id")]
		public string ChannelID { get; set; }

		[JsonProperty(PropertyName = "root_id")]
		public string RootID { get; set; }

		[JsonProperty(PropertyName = "parent_id")]
		public string ParentID { get; set; }

		[JsonProperty(PropertyName = "command")]
		public string Command { get; set; }
	}

	public class CommandResponse
	{
		[JsonProperty(PropertyName = "response_type")]
		public string ResponseType { get; set; }

		[JsonProperty(PropertyName = "text")]
		public string Text { get; set; }

		[JsonProperty(PropertyName = "username")]
		public string Username { get; set; }

		[JsonProperty(PropertyName = "icon_url")]
		public string IconURL { get; set; }

		[JsonProperty(PropertyName = "goto_location")]
		public string GotoLocation { get; set; }

		//public -- Attachments { get; set; }
	}

	public class Compliance
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "user_id")]
		public string UserID { get; set; }

		[JsonProperty(PropertyName = "status")]
		public string Status { get; set; }

		[JsonProperty(PropertyName = "desc")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "type")]
		public string Type { get; set; }

		[JsonProperty(PropertyName = "start_at")]
		public long StartAt { get; set; }

		[JsonProperty(PropertyName = "end_at")]
		public long EndAt { get; set; }

		[JsonProperty(PropertyName = "keywords")]
		public string Keywords { get; set; }

		[JsonProperty(PropertyName = "emails")]
		public string Emails { get; set; }
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
			public string LicenseFileLocation;
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
			public string GoogleDeveloperKey;
			public bool EnableOAuthServiceProvider;
			public bool EnableIncomingWebhooks;
			public bool EnableOutgoingWebhooks;
			public bool EnableCommands;
			public bool EnableOnlyAdminIntegrations;
			public bool EnablePostUsernameOverride;
			public bool EnablePostIconOverride;
			public bool EnableLinkPreviews;
			public bool EnableTesting;
			public bool EnableDeveloper;
			public bool EnableSecurityFixAlert;
			public bool EnableInsecureOutgoingConnections;
			public bool EnableMultifactorAuthentication;
			public bool EnforceMultifactorAuthentication;
			public string AllowCorsFrom;
			public int SessionLengthWebInDays;
			public int SessionLengthMobileInDays;
			public int SessionLengthSSOInDays;
			public int SessionCacheInMinutes;
			public int WebsocketSecurePort;
			public int WebsocketPort;
			public string WebserverMode;
			public bool EnableCustomEmoji;
			public string RestrictCustomEmojiCreation;
			public string RestrictPostDelete;
			public string AllowEditPost;
			public int PostEditTimeLimit;
			public int TimeBetweenUserTypingUpdatesMilliseconds;
			public bool EnablePostSearch;
			public bool EnableUserTypingMessages;
			public int ClusterLogTimeoutMilliseconds;
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
			public bool SkipServerCertificateVerification;
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

	public class Emoji
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "update_at")]
		public long UpdateAt { get; set; }

		[JsonProperty(PropertyName = "delete_at")]
		public long DeleteAt { get; set; }

		[JsonProperty(PropertyName = "creator_id")]
		public string CreatorID { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }
	}

	public class FileInfo
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "creator_id")]
		public string CreatorID { get; set; }

		[JsonProperty(PropertyName = "post_id", NullValueHandling = NullValueHandling.Ignore)]
		public string PostID { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "update_at")]
		public long UpdateAt { get; set; }

		[JsonProperty(PropertyName = "delete_at")]
		public long DeleteAt { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "extension")]
		public string Extension { get; set; }

		[JsonProperty(PropertyName = "size")]
		public long Size { get; set; }

		[JsonProperty(PropertyName = "mime_type")]
		public string MimeType { get; set; }

		[JsonProperty(PropertyName = "width", NullValueHandling = NullValueHandling.Ignore)]
		public int Width { get; set; }

		[JsonProperty(PropertyName = "height", NullValueHandling = NullValueHandling.Ignore)]
		public int Height { get; set; }

		[JsonProperty(PropertyName = "has_preview_image", NullValueHandling = NullValueHandling.Ignore)]
		public bool HasPreviewImage { get; set; }
	}

	public class IncomingWebook
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "update_at")]
		public long UpdateAt { get; set; }

		[JsonProperty(PropertyName = "delete_at")]
		public long DeleteAt { get; set; }

		[JsonProperty(PropertyName = "channel_id")]
		public string ChannelID { get; set; }

		[JsonProperty(PropertyName = "team_id")]
		public string TeamID { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "display_name")]
		public string DisplayName { get; set; }
	}

	public class InitialLoad
	{
		[JsonProperty(PropertyName = "user")]
		public User User { get; set; }

		[JsonProperty(PropertyName = "team_members")]
		public List<TeamMember> TeamMembers { get; set; }

		[JsonProperty(PropertyName = "teams")]
		public List<Team> Teams { get; set; }

		[JsonProperty(PropertyName = "preferences")]
		public List<Preference> Preferences { get; set; }

		[JsonProperty(PropertyName = "client_cfg")]
		public ClientConfig ClientConfig { get; set; }

		[JsonProperty(PropertyName = "license_cfg")]
		public License License { get; set; }

		[JsonProperty(PropertyName = "no_accounts")]
		public bool NoAccounts { get; set; }
	}

	public class Invite
	{
		[JsonProperty(PropertyName = "email")]
		public string Email { get; set; }
	}

	public class InviteInfo
	{
		[JsonProperty(PropertyName = "display_name")]
		public string DisplayName { get; set; }
		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }
		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }
	}

	public class License
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "issued_at")]
		public long IssuedAt { get; set; }

		[JsonProperty(PropertyName = "starts_at")]
		public long StartsAt { get; set; }

		[JsonProperty(PropertyName = "expires_at")]
		public long ExpiresAt { get; set; }

		[JsonProperty(PropertyName = "customer")]
		public Customer customer { get; set; }

		[JsonProperty(PropertyName = "features")]
		public Features features { get; set; }

		public class Customer
		{
			[JsonProperty(PropertyName = "id")]
			public string ID { get; set; }

			[JsonProperty(PropertyName = "name")]
			public string Name { get; set; }

			[JsonProperty(PropertyName = "email")]
			public string Email { get; set; }

			[JsonProperty(PropertyName = "company")]
			public string Company { get; set; }

			[JsonProperty(PropertyName = "phone_number")]
			public string PhoneNumber { get; set; }
		}

		public class Features
		{
			[JsonProperty(PropertyName = "users")]
			public int Users { get; set; }

			[JsonProperty(PropertyName = "ldap")]
			public bool LDAP { get; set; }

			[JsonProperty(PropertyName = "mfa")]
			public bool MFA { get; set; }

			[JsonProperty(PropertyName = "google_oauth")]
			public bool GoogleOAuth { get; set; }

			[JsonProperty(PropertyName = "office365_oauth")]
			public bool Office365OAuth { get; set; }

			[JsonProperty(PropertyName = "compliance")]
			public bool Compliance { get; set; }

			[JsonProperty(PropertyName = "cluster")]
			public bool Cluster { get; set; }

			[JsonProperty(PropertyName = "metrics")]
			public bool Metrics { get; set; }

			[JsonProperty(PropertyName = "custom_brand")]
			public bool CustomBrand { get; set; }

			[JsonProperty(PropertyName = "mhpns")]
			public bool MHPNS { get; set; }

			[JsonProperty(PropertyName = "saml")]
			public bool SAML { get; set; }

			[JsonProperty(PropertyName = "password_requirements")]
			public bool PasswordRequirements { get; set; }

			[JsonProperty(PropertyName = "future_features")]
			public bool FutureFeatures { get; set; }
		}
	}

	public class MessageCount
	{
		[JsonProperty(PropertyName = "team_id")]
		public string TeamID { get; set; }

		[JsonProperty(PropertyName = "msg_count")]
		public int MsgCount { get; set; }

		[JsonProperty(PropertyName = "mention_count")]
		public int MentionCount { get; set; }
	}

	public class OAuthApp
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "creator_id")]
		public string CreatorID { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "update_at")]
		public long UpdateAt { get; set; }

		[JsonProperty(PropertyName = "client_secret")]
		public string ClientSecret { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "icon_url")]
		public string IconURL { get; set; }

		[JsonProperty(PropertyName = "callback_urls")]
		public List<string> CallbackURLs { get; set; }

		[JsonProperty(PropertyName = "homepage")]
		public string Homepage { get; set; }

		[JsonProperty(PropertyName = "is_trusted")]
		public bool IsTrusted { get; set; }
	}

	public class OutgoingWebhook
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "token")]
		public string Token { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "update_at")]
		public long UpdateAt { get; set; }

		[JsonProperty(PropertyName = "delete_at")]
		public long DeleteAt { get; set; }

		[JsonProperty(PropertyName = "creator_id")]
		public string CreatorID { get; set; }

		[JsonProperty(PropertyName = "channel_id")]
		public string ChannelID { get; set; }

		[JsonProperty(PropertyName = "team_id")]
		public string TeamID { get; set; }

		[JsonProperty(PropertyName = "trigger_words")]
		public List<string> TriggerWords { get; set; }

		[JsonProperty(PropertyName = "trigger_when")]
		public int TriggerWhen { get; set; }

		[JsonProperty(PropertyName = "callback_urls")]
		public List<string> CallbackURLs { get; set; }

		[JsonProperty(PropertyName = "display_name")]
		public string DisplayName { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "content_type")]
		public string ContentType { get; set; }
	}

	public class Pong
	{
		[JsonProperty(PropertyName = "version")]
		public string Version { get; set; }

		[JsonProperty(PropertyName = "server_time")]
		public string ServerTime { get; set; }
	}

	public class Post
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "update_at")]
		public long UpdateAt { get; set; }

		[JsonProperty(PropertyName = "delete_at")]
		public long DeleteAt { get; set; }

		[JsonProperty(PropertyName = "user_id")]
		public string UserID { get; set; }

		[JsonProperty(PropertyName = "channel_id")]
		public string ChannelID { get; set; }

		[JsonProperty(PropertyName = "root_id")]
		public string RootID { get; set; }

		[JsonProperty(PropertyName = "parent_id")]
		public string ParentID { get; set; }

		[JsonProperty(PropertyName = "original_id")]
		public string OriginalID { get; set; }

		[JsonProperty(PropertyName = "message")]
		public string Message { get; set; }

		[JsonProperty(PropertyName = "type")]
		public string Type { get; set; }

		[JsonProperty(PropertyName = "Props")]
		public Properties Props { get; set; }

		[JsonProperty(PropertyName = "hashtag")]
		public string Hashtag { get; set; }

		[JsonProperty(PropertyName = "filenames")]
		public List<string> FileNames { get; set; }

		[JsonProperty(PropertyName = "file_ids")]
		public List<string> FileIDs { get; set; }

		[JsonProperty(PropertyName = "pending_post_id")]
		public string PendingPostID { get; set; }

		public class Properties
		{
			[JsonProperty(PropertyName = "username")]
			public string Username { get; set; }
		}
	}

	public class PostList
	{
		[JsonProperty(PropertyName = "order")]
		public List<string> Order { get; set; }

		[JsonProperty(PropertyName = "posts")]
		public Dictionary<string, Post> Posts { get; set; }
	}

	public class Preference
	{
		[JsonProperty(PropertyName = "user_id")]
		public string UserID { get; set; }

		[JsonProperty(PropertyName = "category")]
		public string Category { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "value")]
		public string Value { get; set; }
	}

	public class Reaction
	{
		[JsonProperty(PropertyName = "user_id")]
		public string UserID { get; set; }

		[JsonProperty(PropertyName = "post_id")]
		public string PostID { get; set; }

		[JsonProperty(PropertyName = "emoji_name")]
		public string EmojiName { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }
	}

	public class SamlCertificateStatus
	{
		[JsonProperty(PropertyName = "idp_certificate_file")]
		public bool IdpCertificateFile { get; set; }

		[JsonProperty(PropertyName = "private_key_file")]
		public bool PrivateKeyFile { get; set; }

		[JsonProperty(PropertyName = "public_certificate_file")]
		public bool PublicCertificateFile { get; set; }
	}

	public class Self : User
	{
		[JsonProperty(PropertyName = "email_verified")]
		public bool EmailVerified { get; set; }

		[JsonProperty(PropertyName = "allow_marketing")]
		public bool AllowMarketing { get; set; }

		[JsonProperty(PropertyName = "notify_props")]
		public NotificationProperties NotificationProps { get; set; }

		[JsonProperty(PropertyName = "last_password_update")]
		public long LastPasswordUpdate { get; set; }

		[JsonProperty(PropertyName = "last_picture_update")]
		public long LastPictureUpdate { get; set; }

		public class NotificationProperties
		{
			[JsonProperty(PropertyName = "channel")]
			public string Channel { get; set; }

			[JsonProperty(PropertyName = "comments")]
			public string Comments { get; set; }

			[JsonProperty(PropertyName = "desktop")]
			public string Desktop { get; set; }

			[JsonProperty(PropertyName = "desktop_duration")]
			public string DesktopDuration { get; set; }

			[JsonProperty(PropertyName = "desktop_sound")]
			public string DesktopSound { get; set; }

			[JsonProperty(PropertyName = "email")]
			public string Email { get; set; }

			[JsonProperty(PropertyName = "first_name")]
			public string FirstName { get; set; }

			[JsonProperty(PropertyName = "mention_keys")]
			public string MentionKeys { get; set; }

			[JsonProperty(PropertyName = "push")]
			public string Push { get; set; }

			[JsonProperty(PropertyName = "push_status")]
			public string PushStatus { get; set; }
		}
	}

	public class Session
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "token")]
		public string Token { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "expires_at")]
		public long ExpiresAt { get; set; }

		[JsonProperty(PropertyName = "last_activity_at")]
		public long LastActivityAt { get; set; }

		[JsonProperty(PropertyName = "user_id")]
		public string UserID { get; set; }

		[JsonProperty(PropertyName = "device_id")]
		public string DeviceID { get; set; }

		[JsonProperty(PropertyName = "roles")]
		public string Roles { get; set; }

		[JsonProperty(PropertyName = "is_oauth")]
		public bool IsOAuth { get; set; }

		[JsonProperty(PropertyName = "props")]
		public Properties Props { get; set; }

		[JsonProperty(PropertyName = "team_members")]
		public List<TeamMember> TeamMembers { get; set; }

		public class Properties
		{

		}
	}

	public class UserStatus
	{
		[JsonProperty(PropertyName = "user_id")]
		public string UserID { get; set; }

		[JsonProperty(PropertyName = "status")]
		public string Status { get; set; }

		[JsonProperty(PropertyName = "manual")]
		public bool Manual { get; set; }

		[JsonProperty(PropertyName = "last_activity_at")]
		public long LastActivityAt { get; set; }
	}
	
	public class Team
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "update_at")]
		public long UpdateAt { get; set; }

		[JsonProperty(PropertyName = "delete_at")]
		public long DeleteAt { get; set; }

		[JsonProperty(PropertyName = "display_name")]
		public string DisplayName { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "email")]
		public string Email { get; set; }

		[JsonProperty(PropertyName = "type")]
		public string Type { get; set; }

		[JsonProperty(PropertyName = "company_name")]
		public string CompanyName { get; set; }

		[JsonProperty(PropertyName = "allowed_domains")]
		public string AllowedDomains { get; set; }

		[JsonProperty(PropertyName = "invite_id")]
		public string InviteID { get; set; }

		[JsonProperty(PropertyName = "allow_open_invite")]
		public bool AllowOpenInvite { get; set; }
	}

	public class TeamAutoComplete
	{
		[JsonProperty(PropertyName = "in_team")]
		public List<User> InTeam { get; set; }
	}

	public class TeamMember
	{
		[JsonProperty(PropertyName = "team_id")]
		public string TeamID { get; set; }

		[JsonProperty(PropertyName = "user_id")]
		public string UserID { get; set; }

		[JsonProperty(PropertyName = "roles")]
		public string Roles { get; set; }

		[JsonProperty(PropertyName = "delete_at")]
		public long DeleteAt { get; set; }
	}

	public class TeamStats
	{
		[JsonProperty(PropertyName = "team_id")]
		public string TeamID { get; set; }

		[JsonProperty(PropertyName = "total_member_count")]
		public int TotalMemberCount { get; set; }

		[JsonProperty(PropertyName = "active_member_count")]
		public int ActiveMemberCount { get; set; }
	}

	public class TeamUnread
	{
		[JsonProperty(PropertyName = "team_id")]
		public string TeamID { get; set; }

		[JsonProperty(PropertyName = "msg_count")]
		public long MsgCount { get; set; }

		[JsonProperty(PropertyName = "mention_count")]
		public long MentionCount { get; set; }
	}

	public class User
	{
		[JsonProperty(PropertyName = "id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "create_at")]
		public long CreateAt { get; set; }

		[JsonProperty(PropertyName = "update_at")]
		public long UpdateAt { get; set; }

		[JsonProperty(PropertyName = "delete_at")]
		public long DeleteAt { get; set; }

		[JsonProperty(PropertyName = "username")]
		public string Username { get; set; }

		[JsonProperty(PropertyName = "auth_data")]
		public string AuthData { get; set; }

		[JsonProperty(PropertyName = "auth_service")]
		public string AuthService { get; set; }

		[JsonProperty(PropertyName = "email")]
		public string Email { get; set; }

		[JsonProperty(PropertyName = "nickname")]
		public string Nickname { get; set; }

		[JsonProperty(PropertyName = "first_name")]
		public string FirstName { get; set; }

		[JsonProperty(PropertyName = "last_name")]
		public string LastName { get; set; }

		[JsonProperty(PropertyName = "position")]
		public string Position { get; set; }

		[JsonProperty(PropertyName = "roles")]
		public string Roles { get; set; }

		[JsonProperty(PropertyName = "locale")]
		public string Locale { get; set; }
	}

	public class WebrtcInfoResponse
	{
		[JsonProperty(PropertyName = "token")]
		public string Token { get; set; }
		[JsonProperty(PropertyName = "gateway_url")]
		public string GatewayUrl { get; set; }
		[JsonProperty(PropertyName = "stun_uri", NullValueHandling = NullValueHandling.Ignore)]
		public string StunUri { get; set; }
		[JsonProperty(PropertyName = "turn_uri", NullValueHandling = NullValueHandling.Ignore)]
		public string TurnUri { get; set; }
		[JsonProperty(PropertyName = "turn_password", NullValueHandling = NullValueHandling.Ignore)]
		public string TurnPassword { get; set; }
		[JsonProperty(PropertyName = "turn_username", NullValueHandling = NullValueHandling.Ignore)]
		public string TurnUsername { get; set; }
	}
}
