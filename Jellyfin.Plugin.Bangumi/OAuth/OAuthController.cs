using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Bangumi.Model;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Bangumi.OAuth;

[ApiController]
[Route("Plugins/Bangumi")]
public class OAuthController(BangumiApi api, OAuthStore store, IAuthorizationContext authorizationContext)
    : ControllerBase
{
    protected internal const string ApplicationId = "bgm16185f43c213d11c9";
    protected internal const string ApplicationSecret = "1b28040afd28882aecf23dcdd86be9f7";

    [HttpGet("OAuthState")]
    [Authorize]
    public async Task<Dictionary<string, object?>?> OAuthState()
    {
        var authorizationInfo = await authorizationContext.GetAuthorizationInfo(Request);
        var user = authorizationInfo.User;
        if (user == null)
            return null;
        store.Load();
        var info = store.Get(user.Id);
        if (info == null)
            return null;

        if (string.IsNullOrEmpty(info.Avatar))
        {
            await info.GetProfile(api);
            store.Save();
        }

        return new Dictionary<string, object?>
        {
            ["id"] = info.UserId,
            ["effective"] = info.EffectiveTime,
            ["expire"] = info.ExpireTime,
            ["avatar"] = info.Avatar,
            ["nickname"] = info.NickName,
            ["url"] = info.ProfileUrl,
            ["autoRefresh"] = !string.IsNullOrEmpty(info.RefreshToken),
            ["options"] = info.Options
        };
    }

    [HttpPost("RefreshOAuthToken")]
    [Authorize]
    public async Task<ActionResult> RefreshOAuthToken()
    {
        var authorizationInfo = await authorizationContext.GetAuthorizationInfo(Request);
        var user = authorizationInfo.User;
        if (user == null)
            return BadRequest();
        store.Load();
        var info = store.Get(user.Id);
        if (info == null)
            return BadRequest();
        var httpClient = api.GetHttpClient();
        await info.Refresh(httpClient);
        await info.GetProfile(api);
        store.Save();
        return Accepted();
    }

    [HttpDelete("OAuth")]
    [Authorize]
    public async Task<ActionResult> DeAuth()
    {
        var authorizationInfo = await authorizationContext.GetAuthorizationInfo(Request);
        var user = authorizationInfo.User;
        if (user == null)
            return BadRequest();
        store.Load();
        store.Delete(user.Id);
        store.Save();
        return Accepted();
    }

    [HttpGet("Redirect")]
    public Task<ActionResult> SetCallbackUrl([FromQuery(Name = "prefix")] string urlPrefix, [FromQuery(Name = "user")] string user)
    {
        var redirectUri = Uri.EscapeDataString($"{urlPrefix}/Plugins/Bangumi/OAuth?user={user}");
        return Task.FromResult<ActionResult>(
            Redirect($"{BangumiApi.BaseWebsiteUrl}/oauth/authorize?client_id={ApplicationId}&redirect_uri={redirectUri}&response_type=code"));
    }

    [HttpGet("OAuth")]
    public async Task<object?> OAuthCallback([FromQuery(Name = "code")] string code, [FromQuery(Name = "user")] string user)
    {
        var urlPrefix = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
        using var formData = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", ApplicationId),
            new KeyValuePair<string, string>("client_secret", ApplicationSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", $"{urlPrefix}?user={user}")
        ]);
        var httpClient = api.GetHttpClient();
        var response = await httpClient.PostAsync($"{BangumiApi.BaseWebsiteUrl}/oauth/access_token", formData);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return JsonSerializer.Deserialize<OAuthError>(responseBody, Constants.JsonSerializerOptions);
        var result = JsonSerializer.Deserialize<OAuthUser>(responseBody, Constants.JsonSerializerOptions)!;
        result.EffectiveTime = DateTime.UtcNow;
        await result.GetProfile(api);
        if (!Guid.TryParse(user, out var userId))
            return BadRequest("invalid user id");
        store.Load();
        store.Set(userId, result);
        store.Save();
        return Content("<script>window.opener.postMessage('BANGUMI-OAUTH-COMPLETE'); window.close()</script>", "text/html");
    }

    [HttpGet("UserOptions")]
    [Authorize]
    public async Task<ActionResult<UserOptions?>> GetUserOptions()
    {
        var authorizationInfo = await authorizationContext.GetAuthorizationInfo(Request);
        var user = authorizationInfo.User;
        if (user == null)
            return BadRequest();
        store.Load();
        return store.GetOptions(user.Id);
    }

    [HttpPut("UserOptions")]
    [Authorize]
    public async Task<ActionResult> SetUserOptions([FromBody] UserOptions options)
    {
        var authorizationInfo = await authorizationContext.GetAuthorizationInfo(Request);
        var user = authorizationInfo.User;
        if (user == null)
            return BadRequest();
        store.SetOptions(user.Id, options);
        return Accepted();
    }

    [HttpPatch("AccessToken")]
    [Authorize]
    public async Task<ActionResult> SetAccessTokenManually([FromForm(Name = "token")] string accessToken)
    {
        var authorizationInfo = await authorizationContext.GetAuthorizationInfo(Request);
        var user = authorizationInfo.User;
        if (user == null)
            return BadRequest();
        using var formData = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("access_token", accessToken)
        ]);;
        var httpClient = api.GetHttpClient();
        var response = await httpClient.PostAsync($"{BangumiApi.BaseWebsiteUrl}/oauth/token_status", formData);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            var error = JsonSerializer.Deserialize<OAuthError>(responseBody, Constants.JsonSerializerOptions);
            return Problem(error?.ErrorDescription);
        }

        var result = JsonSerializer.Deserialize<OAuthUser>(responseBody, Constants.JsonSerializerOptions)!;
        result.AccessToken = accessToken;
        result.EffectiveTime = DateTime.UtcNow;
        result.RefreshToken = "";
        store.Load();
        store.Set(user.Id, result);
        store.Save();
        return Accepted();
    }
}
