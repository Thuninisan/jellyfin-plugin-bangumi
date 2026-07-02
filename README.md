# [bgm.tv](https://bgm.tv) metadata provider for Jellyfin

[![Jellyfin Plugin](https://github.com/kookxiang/jellyfin-plugin-bangumi/actions/workflows/build.yml/badge.svg)](https://github.com/kookxiang/jellyfin-plugin-bangumi/actions/workflows/build.yml)

Jellyfin bgm.tv 数据源插件，用于拉取中文番剧信息及图片。

支持将播放进度同步至 bgm.tv

![后台配置](https://github.com/user-attachments/assets/1d1bdfd9-a932-4bf5-9b0a-ec15a1aed3a0)

## 多用户支持

插件现已支持多用户使用，每个 Jellyfin 用户可以绑定各自独立的 Bangumi 账号：

- **独立授权**：每个用户（包括非管理员）可以通过 OAuth 弹窗或手动填写 Token 绑定自己的 Bangumi 账号
- **个人同步设置**：每个用户可以独立控制「是否同步播放进度」「是否同步手动状态变更」「NSFW 条目上报」等选项，未设置时默认使用全局设置
- **播放记录隔离**：不同用户的观看记录会分别同步到各自绑定的 Bangumi 账号
- **管理员权限分级**：插件支持区分超级管理员与普通管理员，普通管理员只能管理自己的账号授权和个人同步设置，无法修改全局配置

> **注意**：由于 Jellyfin 的 `configurationpage` 路由仅对管理员开放，非管理员用户暂时需要通过直接访问 `http://<你的Jellyfin地址>/Plugins/Bangumi/UserPage` 来进行授权和设置。后续版本将改进此体验。

## 近期修复

### 高风险缺陷修复
- **HttpClient 连接池化**：修复每次 API 请求创建新 HttpClient 导致的端口耗尽问题，改为共享单例
- **OAuthStore 并发安全**：增加锁保护，防止多用户并发操作导致数据丢失或字典崩溃
- **OAuth 回调竞态**：移除静态 `_oAuthPath` 字段，改为从 Request 上下文推断回调 URL，避免多用户同时授权时相互干扰
- **同步失败静默丢失**：播放同步异常现在会正确写入日志，不再静默丢弃
- **Token 过期时间**：统一使用 UTC 时间，避免服务器时区或夏令时切换导致误判

### Bug 修复
- **OAuth 授权 Guid 格式不一致**：修复前端传来的用户 ID（带连字符）与服务端 `Guid.ToString("N")`（无连字符）不匹配导致授权后查不到的问题
- **用户数据缓存串号**：修复 `/v0/me` 和用户剧集状态 API 使用 URL 作为缓存 key（不含 access token），导致不同用户看到彼此的 Bangumi 头像昵称

# 下载

 - [CI 最新版](https://github.com/kookxiang/jellyfin-plugin-bangumi/releases/tag/ci)
 - [GitHub 稳定版](https://github.com/kookxiang/jellyfin-plugin-bangumi/releases/latest)

# 安装

## 通过插件库安装

1. 控制台中选择 插件 - 存储库 - 添加
2. 在插件目录中找到 Bangumi 插件安装

目前有三个插件库地址可供选择，可以视网络情况自行选择：
 - GitHub Pages\
   https://kookxiang.github.io/jellyfin-plugin-bangumi/repository.json
 - CloudFlare Pages\
   https://jellyfin-plugin-bangumi.kookxiang.dev/repository.json
 - CloudFlare Pages（不推荐）\
   https://jellyfin-plugin-bangumi.pages.dev/repository.json

安装后可在后台更新，推荐使用此方式安装

## 手动安装

1. 下载插件 DLL 文件至 `Jellyfin 数据目录/Plugins/Bangumi`
2. 重新启动 Jellyfin

# Emby 安装

Emby 版本的插件要求 4.9.0.33 及以上的版本，低于这个版本的会无法启动插件。

可以从 [linuxserver/emby](https://hub.docker.com/r/linuxserver/emby/tags) 和 [emby/embyserver](https://hub.docker.com/r/emby/embyserver/tags) 上找到比 4.9.0.33 更高的版本。

1. 下载插件 DLL 文件至 `Emby 数据目录/plugins/`
2. 重新启动 Emby
