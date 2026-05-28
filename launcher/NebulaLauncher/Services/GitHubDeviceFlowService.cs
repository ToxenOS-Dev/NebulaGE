using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaLauncher.Services;

/// <summary>
/// Implements GitHub's OAuth Device Authorization Flow.
/// https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps#device-flow
///
/// Setup:
///   1. Register a GitHub OAuth App at https://github.com/settings/developers
///   2. Enable "Device Flow" on the app's settings page
///   3. Paste the Client ID below (no secret needed for public device-flow clients)
/// </summary>
public static class GitHubDeviceFlowService
{
    // ── Replace with your OAuth App's Client ID ───────────────
    public const string ClientId = "Ov23lie70Lt2fS8kUlZ6";

    // ──────────────────────────────────────────────────────────

    private static readonly HttpClient _http = new()
    {
        DefaultRequestHeaders =
        {
            Accept    = { MediaTypeWithQualityHeaderValue.Parse("application/json") },
            UserAgent = { ProductInfoHeaderValue.Parse("NebulaGE/1.0") },
        },
    };

    // ── Step 1: request device + user codes ──────────────────

    public record DeviceCodeInfo(
        string DeviceCode,
        string UserCode,
        string VerificationUri,
        int    ExpiresIn,
        int    Interval);

    public static async Task<DeviceCodeInfo?> RequestDeviceCodeAsync()
    {
        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["scope"]     = "repo read:user",
            });

            var response = await _http.PostAsync(
                "https://github.com/login/device/code", content);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            return new DeviceCodeInfo(
                DeviceCode:      root.GetProperty("device_code").GetString()!,
                UserCode:        root.GetProperty("user_code").GetString()!,
                VerificationUri: root.GetProperty("verification_uri").GetString()!,
                ExpiresIn:       root.GetProperty("expires_in").GetInt32(),
                Interval:        root.GetProperty("interval").GetInt32());
        }
        catch { return null; }
    }

    // ── Step 2: poll until the user authorizes ────────────────

    public static async Task<string?> PollForTokenAsync(
        string deviceCode, int pollIntervalSecs, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(pollIntervalSecs), ct)
                      .ContinueWith(_ => { }); // swallow cancellation exception

            if (ct.IsCancellationRequested) break;

            try
            {
                // FormUrlEncodedContent is single-use, so recreate each iteration
                var body = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"]   = ClientId,
                    ["device_code"] = deviceCode,
                    ["grant_type"]  = "urn:ietf:params:oauth:grant-type:device_code",
                });

                var response = await _http.PostAsync(
                    "https://github.com/login/oauth/access_token", body, ct);

                using var doc = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync(ct));
                var root = doc.RootElement;

                // ── Success ───────────────────────────────────
                if (root.TryGetProperty("access_token", out var tok))
                    return tok.GetString();

                // ── Error handling ────────────────────────────
                if (root.TryGetProperty("error", out var err))
                {
                    switch (err.GetString())
                    {
                        case "authorization_pending":
                            break;  // normal — keep polling
                        case "slow_down":
                            pollIntervalSecs += 5;  // GitHub asked us to back off
                            break;
                        default:
                            return null;  // expired / access_denied / unknown
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch { /* transient network error — retry next interval */ }
        }

        return null;
    }

    // ── Step 3: resolve the GitHub username from the token ────

    public static async Task<string?> GetUserLoginAsync(string token)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get,
                "https://api.github.com/user");
            req.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(req);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("login").GetString();
        }
        catch { return null; }
    }

    // ── Step 4: persist the token so `gh` CLI can use it ─────

    /// <summary>
    /// Writes the token to <c>~/.config/gh/hosts.yml</c> in the format
    /// the <c>gh</c> CLI expects, so cloning and other gh operations work
    /// without any extra setup from the user.
    /// </summary>
    public static void SaveToGhConfig(string token, string login)
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "gh");
        var hostsPath = Path.Combine(configDir, "hosts.yml");

        Directory.CreateDirectory(configDir);

        // Minimal YAML — no library dependency needed for this fixed shape
        var yaml =
            $"github.com:\n" +
            $"    oauth_token: {token}\n" +
            $"    user: {login}\n" +
            $"    git_protocol: https\n";

        File.WriteAllText(hostsPath, yaml);
    }

    /// <summary>
    /// Removes the stored GitHub credentials from <c>~/.config/gh/hosts.yml</c>.
    /// We wrote it, so we clear it — no reliance on <c>gh auth logout</c>.
    /// </summary>
    public static void ClearGhConfig()
    {
        var hostsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "gh", "hosts.yml");

        if (File.Exists(hostsPath))
            File.Delete(hostsPath);
    }
}
