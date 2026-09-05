using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jellyfin.Plugin.Bangumi.Model;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.Bangumi.OAuth;

public class OAuthStore
{
    private readonly IApplicationPaths _applicationPaths;

    private readonly object _lock = new();

    private Dictionary<string, OAuthUser> _users = new();

    public OAuthStore(IApplicationPaths applicationPaths)
    {
        _applicationPaths = applicationPaths;
        Load();
    }

    private string StorePath => Path.Join(_applicationPaths.PluginConfigurationsPath, "Jellyfin.Plugin.Bangumi.OAuth.dat");

    public void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(StorePath))
                return;
            var result = JsonSerializer.Deserialize<Dictionary<string, OAuthUser>>(
                File.ReadAllText(StorePath),
                Constants.JsonSerializerOptions
            );
            _users = result ?? new Dictionary<string, OAuthUser>();
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            if (!Directory.Exists(_applicationPaths.PluginConfigurationsPath))
                Directory.CreateDirectory(_applicationPaths.PluginConfigurationsPath);
            var tempPath = StorePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(_users));
            File.Move(tempPath, StorePath, overwrite: true);
        }
    }

    public bool Contains(string userId)
    {
        lock (_lock)
        {
            return _users.ContainsKey(userId);
        }
    }

    public bool Contains(Guid guid)
    {
        return Contains(guid.ToString("N"));
    }

    public OAuthUser? Get(string userId)
    {
        lock (_lock)
        {
            var user = _users.GetValueOrDefault(userId);
            return user?.Expired == true ? null : user;
        }
    }

    public OAuthUser? Get(Guid guid)
    {
        return Get(guid.ToString("N"));
    }

    public OAuthUser? GetAvailable()
    {
        lock (_lock)
        {
            try
            {
                return _users.First(user => !user.Value.Expired).Value;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }

    public void Set(string userId, OAuthUser oAuthResult)
    {
        lock (_lock)
        {
            _users[userId] = oAuthResult;
        }
    }

    public void Set(Guid guid, OAuthUser oAuthResult)
    {
        Set(guid.ToString("N"), oAuthResult);
    }


    public void Delete(string userId)
    {
        lock (_lock)
        {
            _users.Remove(userId);
        }
    }

    public void Delete(Guid guid)
    {
        Delete(guid.ToString("N"));
    }

    protected internal Dictionary<string, OAuthUser> GetUsers()
    {
        lock (_lock)
        {
            return _users;
        }
    }

    public UserOptions? GetOptions(Guid guid)
    {
        return Get(guid)?.Options;
    }

    public void SetOptions(Guid guid, UserOptions options)
    {
        lock (_lock)
        {
            Load();
            var user = _users.GetValueOrDefault(guid.ToString("N"));
            if (user == null)
                return;
            user.Options = options;
            Save();
        }
    }
}
