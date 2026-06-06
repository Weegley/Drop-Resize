using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DropResize
{
    public class UpdateCheckService
    {
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/Weegley/Drop-Resize/releases/latest";
        private const string UpdateApiUrlOverrideVariable = "DROPRESIZE_UPDATE_API_URL";
        private static readonly HttpClient Client = CreateClient();

        public async Task<UpdateInfo> CheckForUpdateAsync(CancellationToken cancellationToken)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using (var request = new HttpRequestMessage(HttpMethod.Get, GetLatestReleaseApiUrl()))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", "DropResize");
                request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");

                using (var response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var latest = ParseLatestRelease(json);
                    var current = GetCurrentVersion();

                    if (latest.Version <= current)
                    {
                        return null;
                    }

                    return latest;
                }
            }
        }

        private static HttpClient CreateClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        private static string GetLatestReleaseApiUrl()
        {
            var overrideUrl = Environment.GetEnvironmentVariable(UpdateApiUrlOverrideVariable);
            return string.IsNullOrWhiteSpace(overrideUrl) ? LatestReleaseApiUrl : overrideUrl;
        }

        private static Version GetCurrentVersion()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            var fileVersion = FileVersionInfo.GetVersionInfo(location).FileVersion;
            Version version;
            return Version.TryParse(fileVersion, out version)
                ? version
                : Assembly.GetExecutingAssembly().GetName().Version;
        }

        private static UpdateInfo ParseLatestRelease(string json)
        {
            var tagName = ReadJsonString(json, "tag_name");
            var releaseUrl = ReadJsonString(json, "html_url");
            var downloadUrl = ReadDownloadUrl(json);

            if (string.IsNullOrWhiteSpace(tagName))
            {
                throw new InvalidOperationException("GitHub release response does not contain tag_name.");
            }

            var versionText = tagName.Trim();
            if (versionText.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                versionText = versionText.Substring(1);
            }

            Version version;
            if (!Version.TryParse(versionText, out version))
            {
                throw new InvalidOperationException("GitHub release version is invalid: " + tagName);
            }

            return new UpdateInfo
            {
                Version = version,
                VersionText = versionText,
                ReleaseUrl = releaseUrl,
                DownloadUrl = downloadUrl
            };
        }

        private static string ReadJsonString(string json, string propertyName)
        {
            var pattern = "\"" + Regex.Escape(propertyName) + "\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"";
            var match = Regex.Match(json, pattern);
            return match.Success ? Regex.Unescape(match.Groups["value"].Value) : null;
        }

        private static string ReadDownloadUrl(string json)
        {
            var pattern = "\"browser_download_url\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"";
            var matches = Regex.Matches(json, pattern);
            string firstUrl = null;

            foreach (Match match in matches)
            {
                var url = Regex.Unescape(match.Groups["value"].Value);
                if (firstUrl == null)
                {
                    firstUrl = url;
                }

                if (url.EndsWith("DropResize-Windows.zip", StringComparison.OrdinalIgnoreCase))
                {
                    return url;
                }
            }

            return firstUrl;
        }
    }
}
