using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CC.Services
{
    public class SmtpConfig
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "Sweet Story Cake Shop";
        public bool EnableSsl { get; set; } = true;
        public bool SimulationMode { get; set; } = true;

        public bool IsRealSmtpConfigured => !string.IsNullOrWhiteSpace(Host) &&
                                           !string.IsNullOrWhiteSpace(Username) &&
                                           !string.IsNullOrWhiteSpace(Password);

        public bool IsConfigured => IsRealSmtpConfigured || SimulationMode;
    }

    public static class EmailService
    {
        private static bool _envLoaded = false;
        private static readonly object _envLock = new();

        /// <summary>
        /// Loads .env configuration if present in app directories or repo root.
        /// </summary>
        public static void EnsureEnvironmentLoaded()
        {
            if (_envLoaded) return;
            lock (_envLock)
            {
                if (_envLoaded) return;
                _envLoaded = true;

                try
                {
                    string[] possiblePaths = new[]
                    {
                        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".env"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".env"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", ".env")
                    };

                    foreach (var path in possiblePaths)
                    {
                        try
                        {
                            string fullPath = Path.GetFullPath(path);
                            if (File.Exists(fullPath))
                            {
                                foreach (var line in File.ReadAllLines(fullPath))
                                {
                                    var trimmed = line.Trim();
                                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
                                    int eqIdx = trimmed.IndexOf('=');
                                    if (eqIdx > 0)
                                    {
                                        string key = trimmed.Substring(0, eqIdx).Trim();
                                        string val = trimmed.Substring(eqIdx + 1).Trim();
                                        if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length >= 2)
                                            val = val.Substring(1, val.Length - 2);

                                        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                                        {
                                            Environment.SetEnvironmentVariable(key, val);
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        catch
                        {
                            // Ignore path resolution errors
                        }
                    }
                }
                catch
                {
                    // Ignore env file errors
                }
            }
        }

        public static SmtpConfig GetSmtpConfig()
        {
            EnsureEnvironmentLoaded();

            string host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
            string portStr = Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587";
            int.TryParse(portStr, out int port);
            if (port <= 0) port = 587;

            string user = Environment.GetEnvironmentVariable("SMTP_USERNAME")
                       ?? Environment.GetEnvironmentVariable("SMTP_USER")
                       ?? string.Empty;

            string pass = Environment.GetEnvironmentVariable("SMTP_PASSWORD")
                       ?? Environment.GetEnvironmentVariable("SMTP_PASS")
                       ?? string.Empty;

            string fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL")
                            ?? Environment.GetEnvironmentVariable("SMTP_FROM")
                            ?? Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL")
                            ?? user;

            string fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
                           ?? Environment.GetEnvironmentVariable("SMTP_SENDER_NAME")
                           ?? "Sweet Story Cake Shop";

            string sslStr = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL")
                         ?? Environment.GetEnvironmentVariable("SMTP_USE_SSL")
                         ?? "true";
            bool enableSsl = !bool.TryParse(sslStr, out bool ssl) || ssl;

            string? mockStr = Environment.GetEnvironmentVariable("SMTP_MOCK")
                           ?? Environment.GetEnvironmentVariable("SMTP_SIMULATION");

            // Simulation mode is active if explicitly enabled, or if real credentials are not provided
            bool isSimulation = string.Equals(mockStr, "true", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(host, "mock", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(host, "test", StringComparison.OrdinalIgnoreCase)
                             || (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass));

            return new SmtpConfig
            {
                Host = host.Trim(),
                Port = port,
                Username = user.Trim(),
                Password = pass,
                FromEmail = fromEmail.Trim(),
                FromName = fromName.Trim(),
                EnableSsl = enableSsl,
                SimulationMode = isSimulation
            };
        }

        public static void SaveSmtpConfig(SmtpConfig config)
        {
            Environment.SetEnvironmentVariable("SMTP_HOST", config.Host);
            Environment.SetEnvironmentVariable("SMTP_PORT", config.Port.ToString());
            Environment.SetEnvironmentVariable("SMTP_USERNAME", config.Username);
            Environment.SetEnvironmentVariable("SMTP_PASSWORD", config.Password);
            Environment.SetEnvironmentVariable("SMTP_FROM_EMAIL", config.FromEmail);
            Environment.SetEnvironmentVariable("SMTP_FROM_NAME", config.FromName);
            Environment.SetEnvironmentVariable("SMTP_ENABLE_SSL", config.EnableSsl.ToString().ToLower());
            Environment.SetEnvironmentVariable("SMTP_MOCK", config.SimulationMode.ToString().ToLower());

            string envContent =
$@"# Custom Cake CRM - Email Configuration
# Updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
SMTP_MOCK={config.SimulationMode.ToString().ToLower()}
SMTP_HOST={config.Host}
SMTP_PORT={config.Port}
SMTP_USERNAME={config.Username}
SMTP_PASSWORD={config.Password}
SMTP_FROM_EMAIL={config.FromEmail}
SMTP_FROM_NAME=""{config.FromName}""
SMTP_ENABLE_SSL={config.EnableSsl.ToString().ToLower()}
";

            string[] pathsToSave = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".env")
            };

            foreach (var p in pathsToSave)
            {
                try
                {
                    string full = Path.GetFullPath(p);
                    string? dir = Path.GetDirectoryName(full);
                    if (dir != null && Directory.Exists(dir))
                    {
                        File.WriteAllText(full, envContent);
                    }
                }
                catch
                {
                    // Ignore write failures on non-existent parent paths
                }
            }
        }

        public static async Task<(bool success, string message)> TestSmtpConnectionAsync(SmtpConfig config)
        {
            if (config.SimulationMode)
            {
                await Task.Delay(150);
                return (true, "Simulation Mode is active. Emails are simulated and safely logged directly in CRM delivery history without requiring external mail server credentials.");
            }

            if (!config.IsRealSmtpConfigured)
            {
                return (false, "Please provide Host, Username, and Password to test real SMTP connection.");
            }

            try
            {
                string password = config.Password?.Trim() ?? string.Empty;
                if (config.Host.Contains("gmail", StringComparison.OrdinalIgnoreCase))
                {
                    password = password.Replace(" ", "");
                }

                using var client = new SmtpClient(config.Host, config.Port)
                {
                    Credentials = new NetworkCredential(config.Username, password),
                    EnableSsl = config.EnableSsl,
                    Timeout = 8000
                };

                using var msg = new MailMessage
                {
                    From = new MailAddress(string.IsNullOrWhiteSpace(config.FromEmail) ? config.Username : config.FromEmail, config.FromName),
                    Subject = "CRM Email Connection Test",
                    Body = "This is a connection verification test from your Sweet Story CRM.",
                    IsBodyHtml = false
                };
                msg.To.Add(new MailAddress(config.Username));

                await client.SendMailAsync(msg);
                return (true, $"Success! A test verification email was sent to {config.Username}. Your SMTP configuration is verified and ready.");
            }
            catch (Exception ex)
            {
                return (false, $"Connection failed: {ex.Message}\n\nTip for Gmail: Ensure 2-Step Verification is turned on and use a 16-character App Password (not your regular account password).");
            }
        }

        public static bool ValidateEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Sends an email using configured SMTP settings or sandbox simulation mode.
        /// </summary>
        public static async Task SendEmailAsync(string toEmail, string toName, string subject, string body, bool isHtml = false)
        {
            if (!ValidateEmail(toEmail))
            {
                throw new ArgumentException($"Invalid recipient email address: '{toEmail}'");
            }

            var config = GetSmtpConfig();

            // Simulation / Sandbox Mode
            if (config.SimulationMode ||
                config.Host.Equals("mock", StringComparison.OrdinalIgnoreCase) ||
                config.Host.Equals("test", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Environment.GetEnvironmentVariable("SMTP_MOCK"), "true", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(150);
                return;
            }

            if (!config.IsRealSmtpConfigured)
            {
                throw new InvalidOperationException(
                    "Email could not be sent. SMTP configuration is missing.\n\n" +
                    "Please configure your email settings (Host, Port, Username, App Password) or enable Simulation Mode.");
            }

            string password = config.Password?.Trim() ?? string.Empty;
            if (config.Host.Contains("gmail", StringComparison.OrdinalIgnoreCase))
            {
                password = password.Replace(" ", "");
            }

            using var client = new SmtpClient(config.Host, config.Port)
            {
                Credentials = new NetworkCredential(config.Username, password),
                EnableSsl = config.EnableSsl,
                Timeout = 15000 // 15 seconds
            };

            using var message = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(config.FromEmail) ? config.Username : config.FromEmail, config.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            string cleanToName = string.IsNullOrWhiteSpace(toName) ? toEmail : toName.Trim();
            message.To.Add(new MailAddress(toEmail.Trim(), cleanToName));

            await client.SendMailAsync(message);
        }
    }
}
