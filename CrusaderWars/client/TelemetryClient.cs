using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Management;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Runtime.Versioning;
using CrusaderWars.armies;
using CrusaderWars.twbattle;
using CrusaderWars.unit_mapper;

namespace CrusaderWars.client
{
    /// <summary>
    /// Anonymous telemetry client for the Crusader Conflicts backend.
    /// Implements the contract in .cecli.plans.md Section 16 (Backend API Contract):
    /// hardware-fingerprint registration, bad-units (crashes) and good-units (successes).
    /// All calls are fire-and-forget and never throw into gameplay.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class TelemetryClient
    {
        private static string ClientVersion
        {
            get
            {
                // Read the real app version from app_version.txt (same source as the main menu label).
                // Falls back to the assembly version, then "1.0.0" only if both fail.
                try
                {
                    string version_path = Path.Combine(Directory.GetCurrentDirectory(), "app_version.txt");
                    if (File.Exists(version_path))
                    {
                        string fileContent = File.ReadAllText(version_path);
                        var match = System.Text.RegularExpressions.Regex.Match(fileContent, "\"(.+)\"");
                        if (match.Success)
                        {
                            return match.Groups[1].Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Telemetry: could not read app_version.txt: {ex.Message}");
                }

                try
                {
                    return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
                }
                catch
                {
                    return "1.0.0";
                }
            }
        }
        private const int MaxUnitKeys = 500;
        private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(10);

        private static readonly string CredentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CrusaderWars3", "client.json");

        private static readonly object _lock = new object();
        private static string? _clientId;
        private static string? _accessToken;
        private static string? _fingerprint;
        private static bool _loaded;

        private sealed class Credentials
        {
            public string? hardware_fingerprint { get; set; }
            public string? client_id { get; set; }
            public string? access_token { get; set; }
            public string? client_version { get; set; }
        }

        private static string BaseUrl
        {
            get
            {
                string url = Environment.GetEnvironmentVariable("TELEMETRY_API_BASE_URL");
                if (string.IsNullOrWhiteSpace(url))
                {
                    url = "https://crusader-conflicts.vercel.app";
                }
                return url.TrimEnd('/');
            }
        }

        // ------------------------------------------------------------------
        // Registration
        // ------------------------------------------------------------------

        /// <summary>
        /// Registers this client with the backend if not already registered.
        /// Fire-and-forget safe: call and discard the returned task.
        /// </summary>
        public static Task RegisterIfNeeded()
        {
            if (!ModOptions.GetSendAnonymousTelemetry())
            {
                Program.Logger.Debug("Telemetry: registration skipped (anonymous telemetry disabled).");
                return Task.CompletedTask;
            }
            return Task.Run(async () =>
            {
                try
                {
                    LoadCredentials();
                    // Always register so the backend receives the current client version,
                    // even when cached credentials already exist (the backend upserts by
                    // hardware fingerprint, so this refreshes the stored client_version).
                    await RegisterAsync();
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Telemetry: registration failed: {ex.Message}");
                }
            });
        }

        private static async Task RegisterAsync()
        {
            string baseUrl = BaseUrl;

            string fingerprint = GetOrCreateFingerprint();
            Program.Logger.Debug($"Telemetry: registering client at {baseUrl}/api/v1/clients/register (client_version={ClientVersion}).");
            using var http = new HttpClient { Timeout = HttpTimeout };
            var payload = new { hardware_fingerprint = fingerprint, client_version = ClientVersion };
            using var resp = await http.PostAsJsonAsync($"{baseUrl}/api/v1/clients/register", payload);
            if (!resp.IsSuccessStatusCode)
            {
                Program.Logger.Debug($"Telemetry: register returned {(int)resp.StatusCode}.");
                return;
            }

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            _clientId = doc.RootElement.GetProperty("client_id").GetString();
            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
            SaveCredentials(fingerprint);
            Program.Logger.Debug("Telemetry: client registered successfully.");
        }

        // ------------------------------------------------------------------
        // Unit submissions
        // ------------------------------------------------------------------

        /// <summary>
        /// Submits all Attila unit keys from the given armies to the telemetry backend.
        /// kind: "bad" (crash) or "good" (successful battle). Fire-and-forget safe.
        /// </summary>
        public static void SubmitUnits(string kind, List<Army> attacker_armies, List<Army> defender_armies)
        {
            if (!ModOptions.GetSendAnonymousTelemetry())
            {
                Program.Logger.Debug($"Telemetry: {kind}-units submission skipped (anonymous telemetry disabled).");
                return;
            }

            var unitKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var army in (attacker_armies ?? new List<Army>()).Concat(defender_armies ?? new List<Army>()))
            {
                if (army?.Units == null) continue;
                foreach (var unit in army.Units)
                {
                    if (unit == null) continue;
                    string key = unit.GetAttilaUnitKey();
                    if (!string.IsNullOrEmpty(key) && key != UnitMappers_BETA.NOT_FOUND_KEY)
                        unitKeys.Add(key);
                }
            }

            // Garrison/levy compositions are expanded into their Attila unit keys.
            foreach (var army in (attacker_armies ?? new List<Army>()).Concat(defender_armies ?? new List<Army>()))
            {
                if (army == null) continue;
                try
                {
                    string faction = army.Units?.FirstOrDefault()?.GetAttilaFaction() ?? "";
                    if (string.IsNullOrEmpty(faction)) continue;

                    var levies = UnitMappers_BETA.GetFactionLevies(faction);
                    foreach (var levy in levies.Item1) unitKeys.Add(levy.unit_key);

                    if (BattleState.IsSiegeBattle)
                    {
                        int holdingLevel = Sieges.GetHoldingLevel();
                        foreach (var garrison in UnitMappers_BETA.GetFactionGarrison(faction, holdingLevel))
                            unitKeys.Add(garrison.unit_key);
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Telemetry: could not expand levy/garrison keys for faction: {ex.Message}");
                }
            }

            if (!unitKeys.Any())
            {
                Program.Logger.Debug("Telemetry: no unit keys collected, skipping submission.");
                return;
            }

            var keys = unitKeys.Take(MaxUnitKeys).ToList();
            Program.Logger.Debug($"Telemetry: collected {unitKeys.Count} unit keys for {kind}-units (submitting {keys.Count}, cap {MaxUnitKeys}).");
            string playthroughTag = GetPlaythroughTag();
            string startYear = UnitMappers_BETA.MapperStartDate;
            string endYear = UnitMappers_BETA.MapperEndDate;

            _ = Task.Run(async () =>
            {
                try
                {
                    await SubmitUnitsCoreAsync(kind, playthroughTag, startYear, endYear, keys);
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Telemetry: submit {kind}-units failed: {ex.Message}");
                }
            });
        }

        private static async Task SubmitUnitsCoreAsync(string kind, string playthroughTag, string startYear, string endYear, List<string> keys)
        {
            string baseUrl = BaseUrl;

            LoadCredentials();
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_accessToken))
            {
                await RegisterAsync();
                if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_accessToken)) return;
            }

            var payload = new
            {
                client_id = _clientId,
                playthrough_tag = playthroughTag,
                total_war_game = "attila",
                start_year = startYear,
                end_year = endYear,
                unit_keys = keys
            };

            bool reRegistered = false;
            int backoffMs = 1000;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                using var http = new HttpClient { Timeout = HttpTimeout };
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                if (!string.IsNullOrEmpty(_fingerprint))
                    http.DefaultRequestHeaders.Add("X-HW-Fingerprint", _fingerprint);

                Program.Logger.Debug($"Telemetry: POST {baseUrl}/api/v1/telemetry/{kind}-units (attempt {attempt + 1}/3, {keys.Count} unit keys, playthrough='{playthroughTag}', years={startYear}-{endYear}).");
                using var resp = await http.PostAsJsonAsync($"{baseUrl}/api/v1/telemetry/{kind}-units", payload);
                if (resp.IsSuccessStatusCode)
                {
                    Program.Logger.Debug($"Telemetry: {kind}-units submission succeeded ({(int)resp.StatusCode}).");
                    return;
                }

                int status = (int)resp.StatusCode;
                if (status == 401 && !reRegistered)
                {
                    // Token revoked/rotated: re-register once and retry.
                    Program.Logger.Debug("Telemetry: 401 from backend, re-registering once.");
                    await RegisterAsync();
                    reRegistered = true;
                    continue;
                }
                if (status == 429)
                {
                    // Rate limited: exponential backoff and retry.
                    Program.Logger.Debug($"Telemetry: 429 rate limited, backing off {backoffMs}ms.");
                    await Task.Delay(backoffMs);
                    backoffMs *= 2;
                    continue;
                }
                Program.Logger.Debug($"Telemetry: submit {kind}-units returned {status}.");
                return;
            }
            Program.Logger.Debug($"Telemetry: submit {kind}-units gave up after retries.");
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static string GetPlaythroughTag()
        {
            // Prefer the tag loaded by the unit mapper (tag.txt value).
            if (!string.IsNullOrEmpty(UnitMappers_BETA.ActivePlaythroughTag))
                return UnitMappers_BETA.ActivePlaythroughTag;
            // Custom playthroughs: use the selected custom mapper tag.
            return ModOptions.GetSelectedCustomMapper();
        }

        private static void LoadCredentials()
        {
            lock (_lock)
            {
                if (_loaded) return;
                _loaded = true;
                try
                {
                    if (!File.Exists(CredentialsPath)) return;
                    using var doc = JsonDocument.Parse(File.ReadAllText(CredentialsPath));
                    _fingerprint = doc.RootElement.TryGetProperty("hardware_fingerprint", out var fp) ? fp.GetString() : null;
                    _clientId = doc.RootElement.TryGetProperty("client_id", out var cid) ? cid.GetString() : null;
                    _accessToken = doc.RootElement.TryGetProperty("access_token", out var tok) ? tok.GetString() : null;
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Telemetry: failed to load credentials: {ex.Message}");
                }
            }
        }

        private static void SaveCredentials(string fingerprint)
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(CredentialsPath)!);
                    var creds = new Credentials
                    {
                        hardware_fingerprint = fingerprint,
                        client_id = _clientId,
                        access_token = _accessToken,
                        client_version = ClientVersion
                    };
                    File.WriteAllText(CredentialsPath, JsonSerializer.Serialize(creds, new JsonSerializerOptions { WriteIndented = true }));
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Telemetry: failed to save credentials: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Hardware fingerprint per plan: SHA256 of "MachineGuid|CpuName|MachineName".
        /// Fallback chain: MachineGuid -> SMBIOS UUID -> persisted random GUID.
        /// </summary>
        private static string GetOrCreateFingerprint()
        {
            if (!string.IsNullOrEmpty(_fingerprint)) return _fingerprint;

            string machineGuid = "";
            try
            {
                machineGuid = Microsoft.Win32.Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid", null)?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Telemetry: could not read MachineGuid: {ex.Message}");
            }

            string cpuName = "";
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                cpuName = searcher.Get().Cast<System.Management.ManagementObject>()
                    .Select(m => m["Name"]?.ToString() ?? "").FirstOrDefault() ?? "";
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Telemetry: could not read CPU name: {ex.Message}");
            }

            string smbiosUuid = "";
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
                smbiosUuid = searcher.Get().Cast<System.Management.ManagementObject>()
                    .Select(m => m["UUID"]?.ToString() ?? "").FirstOrDefault() ?? "";
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Telemetry: could not read SMBIOS UUID: {ex.Message}");
            }

            string raw = $"{machineGuid}|{cpuName}|{Environment.MachineName}";
            string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

            // If MachineGuid was missing, mix in the SMBIOS UUID (or a persisted random GUID)
            // so the fingerprint is still stable and unique per machine.
            if (string.IsNullOrEmpty(machineGuid))
            {
                string fallback = !string.IsNullOrEmpty(smbiosUuid) ? smbiosUuid : GetPersistedRandomGuid();
                fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{fallback}|{cpuName}|{Environment.MachineName}")));
            }

            _fingerprint = fingerprint;
            return fingerprint;
        }

        private static string GetPersistedRandomGuid()
        {
            string guidPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CrusaderWars3", "machine_guid.txt");
            try
            {
                if (File.Exists(guidPath)) return File.ReadAllText(guidPath).Trim();
                Directory.CreateDirectory(Path.GetDirectoryName(guidPath)!);
                string guid = Guid.NewGuid().ToString();
                File.WriteAllText(guidPath, guid);
                return guid;
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Telemetry: failed to persist random GUID: {ex.Message}");
                return Guid.NewGuid().ToString();
            }
        }
    }
}