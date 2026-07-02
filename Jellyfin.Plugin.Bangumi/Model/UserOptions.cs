using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bangumi.Model;

public class UserOptions
{
    /// <summary>
    /// 是否同步播放进度到 Bangumi。null 表示使用全局设置。
    /// </summary>
    [JsonPropertyName("reportPlaybackStatusToBangumi")]
    public bool? ReportPlaybackStatusToBangumi { get; set; }

    /// <summary>
    /// 是否禁用 NSFW 条目上报。null 表示使用全局设置。
    /// </summary>
    [JsonPropertyName("skipNSFWPlaybackReport")]
    public bool? SkipNSFWPlaybackReport { get; set; }

    /// <summary>
    /// 是否同步手动更新后的播放状态到 Bangumi。null 表示使用全局设置。
    /// </summary>
    [JsonPropertyName("reportManualStatusChangeToBangumi")]
    public bool? ReportManualStatusChangeToBangumi { get; set; }

    /// <summary>
    /// NSFW 条目上报是否仅自己可见。null 表示使用全局设置。
    /// </summary>
    [JsonPropertyName("privateNSFWPlaybackReport")]
    public bool? PrivateNSFWPlaybackReport { get; set; }
}
