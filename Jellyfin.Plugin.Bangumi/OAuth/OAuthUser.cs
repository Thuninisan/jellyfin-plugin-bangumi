using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Bangumi.Model;

namespace Jellyfin.Plugin.Bangumi.OAuth;

[SuppressMessage("Design", "CA1044: Properties should not be write only")]
public partial class OAuthUser
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("username")]
    public string UserName { get; set; } = "";

    [JsonPropertyName("expires_time")]
    public DateTime ExpireTime { get; set; }

    [JsonPropertyName("effective_time")]
    public DateTime? EffectiveTime { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpireIn
    {
        set => ExpireTime = DateTime.UtcNow.AddSeconds(value);
    }

    [JsonPropertyName("expires")]
    public int Expire
    {
        set => ExpireTime = DateTime.UnixEpoch.AddSeconds(value);
    }

    [JsonPropertyName("options")]
    public UserOptions? Options { get; set; }
}
