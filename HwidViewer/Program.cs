using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Net.NetworkInformation;

namespace HwidViewer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly ListView _listView;
        private readonly TextBox _fingerprintBox;
        private readonly Button _refreshButton;
        private readonly Button _copyButton;
        private readonly Button _saveBaselineButton;
        private readonly Button _compareButton;
        private readonly Button _historyButton;
        private readonly Label _statusLabel;
        private List<KeyValuePair<string, string>> _currentEntries;

        public MainForm()
        {
            Text = "HWID Viewer";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(780, 520);
            Size = new Size(920, 620);
            BackColor = Color.WhiteSmoke;

            var titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 62,
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
                Text = "HWID Viewer",
                Padding = new Padding(18, 16, 18, 0)
            };

            var subtitleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 9.5f),
                Text = "Menampilkan identifier perangkat milik Anda sendiri tanpa mengubah sistem.",
                Padding = new Padding(20, 0, 18, 8),
                ForeColor = Color.DimGray
            };

            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HideSelection = false,
                Font = new Font("Consolas", 10f),
                BackColor = Color.White
            };
            _listView.Columns.Add("Field", 220);
            _listView.Columns.Add("Value", 620);

            _fingerprintBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 34,
                ReadOnly = true,
                Font = new Font("Consolas", 10f),
                Margin = new Padding(0, 8, 0, 8)
            };

            var fingerprintLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Text = "Local Fingerprint (SHA-256)",
                Padding = new Padding(0, 2, 0, 0)
            };

            _refreshButton = new Button
            {
                Text = "Refresh",
                Width = 110,
                Height = 34,
                BackColor = Color.FromArgb(31, 87, 169),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _refreshButton.FlatAppearance.BorderSize = 0;
            _refreshButton.Click += delegate { LoadHardwareInfo(); };

            _copyButton = new Button
            {
                Text = "Copy All",
                Width = 110,
                Height = 34,
                BackColor = Color.FromArgb(41, 128, 99),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _copyButton.FlatAppearance.BorderSize = 0;
            _copyButton.Click += delegate { CopyAll(); };

            _saveBaselineButton = new Button
            {
                Text = "Save Baseline",
                Width = 130,
                Height = 34,
                BackColor = Color.FromArgb(193, 103, 29),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _saveBaselineButton.FlatAppearance.BorderSize = 0;
            _saveBaselineButton.Click += delegate { SaveBaseline(); };

            _compareButton = new Button
            {
                Text = "Compare",
                Width = 110,
                Height = 34,
                BackColor = Color.FromArgb(109, 77, 147),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _compareButton.FlatAppearance.BorderSize = 0;
            _compareButton.Click += delegate { CompareWithBaseline(true); };

            _historyButton = new Button
            {
                Text = "History",
                Width = 110,
                Height = 34,
                BackColor = Color.FromArgb(63, 63, 63),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _historyButton.FlatAppearance.BorderSize = 0;
            _historyButton.Click += delegate { ShowHistory(); };

            _statusLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.DimGray,
                Padding = new Padding(0, 8, 0, 0)
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            buttonPanel.Controls.Add(_refreshButton);
            buttonPanel.Controls.Add(_copyButton);
            buttonPanel.Controls.Add(_saveBaselineButton);
            buttonPanel.Controls.Add(_compareButton);
            buttonPanel.Controls.Add(_historyButton);
            buttonPanel.Controls.Add(_statusLabel);

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                Padding = new Padding(18, 10, 18, 18)
            };
            bottomPanel.Controls.Add(buttonPanel);
            bottomPanel.Controls.Add(_fingerprintBox);
            bottomPanel.Controls.Add(fingerprintLabel);

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 10, 18, 0)
            };
            contentPanel.Controls.Add(_listView);

            Controls.Add(contentPanel);
            Controls.Add(bottomPanel);
            Controls.Add(subtitleLabel);
            Controls.Add(titleLabel);

            Load += delegate { LoadHardwareInfo(); };
        }

        private void LoadHardwareInfo()
        {
            _refreshButton.Enabled = false;
            _copyButton.Enabled = false;
            _saveBaselineButton.Enabled = false;
            _compareButton.Enabled = false;
            _historyButton.Enabled = false;
            _statusLabel.Text = "Reading system identifiers...";
            _listView.BeginUpdate();
            _listView.Items.Clear();

            var entries = new List<KeyValuePair<string, string>>
            {
                Entry("Machine GUID", ReadMachineGuid()),
                Entry("System UUID", ReadWmiProperty("Win32_ComputerSystemProduct", "UUID")),
                Entry("BIOS Serial", ReadWmiProperty("Win32_BIOS", "SerialNumber")),
                Entry("Baseboard Serial", ReadWmiProperty("Win32_BaseBoard", "SerialNumber")),
                Entry("CPU Processor ID", ReadWmiProperty("Win32_Processor", "ProcessorId")),
                Entry("CPU Name", ReadWmiProperty("Win32_Processor", "Name")),
                Entry("Disk Model", ReadWmiProperty("Win32_DiskDrive", "Model")),
                Entry("Disk Serial", ReadWmiProperty("Win32_PhysicalMedia", "SerialNumber")),
                Entry("Primary MAC", ReadPrimaryMacAddress()),
                Entry("Computer Name", Environment.MachineName),
                Entry("User Name", Environment.UserName),
                Entry("OS Version", Environment.OSVersion.VersionString)
            };

            foreach (var entry in entries)
            {
                var item = new ListViewItem(entry.Key);
                item.SubItems.Add(entry.Value);
                item.BackColor = Color.White;
                _listView.Items.Add(item);
            }

            _currentEntries = entries;
            _fingerprintBox.Text = BuildFingerprint(entries);
            _listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            if (_listView.Columns.Count > 0)
            {
                _listView.Columns[0].Width = 220;
                _listView.Columns[1].Width = Math.Max(620, _listView.Columns[1].Width);
            }

            _listView.EndUpdate();
            _statusLabel.Text = "Last updated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _refreshButton.Enabled = true;
            _copyButton.Enabled = true;
            _saveBaselineButton.Enabled = true;
            _compareButton.Enabled = true;
            _historyButton.Enabled = true;
            CompareWithBaseline(false);
        }

        private void CopyAll()
        {
            var builder = new StringBuilder();
            foreach (ListViewItem item in _listView.Items)
            {
                builder.AppendLine(item.Text + ": " + item.SubItems[1].Text);
            }
            builder.AppendLine();
            builder.AppendLine("Local Fingerprint (SHA-256): " + _fingerprintBox.Text);

            Clipboard.SetText(builder.ToString());
            _statusLabel.Text = "Copied to clipboard.";
        }

        private static string DataDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HwidViewer"); }
        }

        private static string BaselinePath
        {
            get { return Path.Combine(DataDir, "baseline.txt"); }
        }

        private static string HistoryPath
        {
            get { return Path.Combine(DataDir, "history.log"); }
        }

        private void SaveBaseline()
        {
            if (_currentEntries == null || _currentEntries.Count == 0)
            {
                _statusLabel.Text = "No data to save.";
                return;
            }

            try
            {
                EnsureDataDir();
                var lines = new List<string>
                {
                    "SavedAt=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                foreach (var entry in _currentEntries)
                {
                    lines.Add(EncodeLine(entry.Key, entry.Value));
                }

                File.WriteAllLines(BaselinePath, lines.ToArray(), Encoding.UTF8);
                AppendHistory("BASELINE_SAVED", "Fingerprint=" + _fingerprintBox.Text);
                _statusLabel.Text = "Baseline saved.";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Save failed: " + ex.Message;
            }
        }

        private void CompareWithBaseline(bool showDialog)
        {
            if (_currentEntries == null || _currentEntries.Count == 0)
            {
                return;
            }

            if (!File.Exists(BaselinePath))
            {
                if (showDialog)
                {
                    MessageBox.Show(this, "Baseline belum ada. Klik 'Save Baseline' terlebih dahulu.", "Compare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                _statusLabel.Text = "No baseline found.";
                return;
            }

            try
            {
                string savedAt;
                var baseline = LoadBaseline(out savedAt);
                var changed = new List<string>();
                var currentMap = _currentEntries.ToDictionary(entry => entry.Key, entry => entry.Value);

                foreach (ListViewItem item in _listView.Items)
                {
                    item.BackColor = Color.White;
                    string key = item.Text;
                    string currentValue = item.SubItems[1].Text;
                    string baselineValue;
                    if (!baseline.TryGetValue(key, out baselineValue))
                    {
                        changed.Add(key + " (new field)");
                        item.BackColor = Color.MistyRose;
                        continue;
                    }

                    if (!string.Equals(currentValue, baselineValue, StringComparison.Ordinal))
                    {
                        changed.Add(key);
                        item.BackColor = Color.MistyRose;
                    }
                }

                foreach (var key in baseline.Keys)
                {
                    if (!currentMap.ContainsKey(key))
                    {
                        changed.Add(key + " (missing now)");
                    }
                }

                if (changed.Count == 0)
                {
                    _statusLabel.Text = "Compare OK: no change (baseline " + savedAt + ")";
                    AppendHistory("COMPARE_NO_CHANGE", "Baseline=" + savedAt);
                    if (showDialog)
                    {
                        MessageBox.Show(this, "Tidak ada perubahan dibanding baseline (" + savedAt + ").", "Compare Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }

                string summary = string.Join(", ", changed.Take(6).ToArray());
                if (changed.Count > 6)
                {
                    summary += ", +" + (changed.Count - 6) + " more";
                }

                _statusLabel.Text = "Compare changed: " + changed.Count + " field(s)";
                AppendHistory("COMPARE_CHANGED", "Count=" + changed.Count + "; Fields=" + summary);
                if (showDialog)
                {
                    MessageBox.Show(this, "Terdeteksi " + changed.Count + " perubahan.\r\n\r\n" + summary, "Compare Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Compare failed: " + ex.Message;
            }
        }

        private void ShowHistory()
        {
            try
            {
                if (!File.Exists(HistoryPath))
                {
                    MessageBox.Show(this, "Belum ada history.", "History", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var lines = File.ReadAllLines(HistoryPath);
                var recent = lines.Reverse().Take(25).Reverse().ToArray();
                MessageBox.Show(this, string.Join("\r\n", recent), "History (last 25)", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "History failed: " + ex.Message;
            }
        }

        private static string EncodeLine(string key, string value)
        {
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
            return key + "=" + encoded;
        }

        private static Dictionary<string, string> LoadBaseline(out string savedAt)
        {
            savedAt = "unknown";
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in File.ReadAllLines(BaselinePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("SavedAt=", StringComparison.Ordinal))
                {
                    savedAt = line.Substring("SavedAt=".Length);
                    continue;
                }

                int idx = line.IndexOf('=');
                if (idx <= 0 || idx >= line.Length - 1)
                {
                    continue;
                }

                string key = line.Substring(0, idx);
                string encoded = line.Substring(idx + 1);
                string value = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                map[key] = value;
            }

            return map;
        }

        private static void AppendHistory(string action, string details)
        {
            EnsureDataDir();
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + action + " | " + details;
            File.AppendAllLines(HistoryPath, new[] { line }, Encoding.UTF8);
        }

        private static void EnsureDataDir()
        {
            if (!Directory.Exists(DataDir))
            {
                Directory.CreateDirectory(DataDir);
            }
        }

        private static KeyValuePair<string, string> Entry(string key, string value)
        {
            return new KeyValuePair<string, string>(key, NormalizeValue(value));
        }

        private static string NormalizeValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Unavailable" : value.Trim();
        }

        private static string ReadMachineGuid()
        {
            try
            {
                using (RegistryKey key = RegistryLocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    return key == null ? null : key.GetValue("MachineGuid") as string;
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        private static RegistryKey RegistryLocalMachine
        {
            get { return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64); }
        }

        private static string ReadWmiProperty(string className, string propertyName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT " + propertyName + " FROM " + className))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject obj in collection)
                    {
                        var value = obj[propertyName];
                        if (value != null)
                        {
                            return value.ToString();
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        private static string ReadPrimaryMacAddress()
        {
            try
            {
                var nic = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up)
                    .Where(adapter => adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .OrderByDescending(adapter => adapter.Speed)
                    .FirstOrDefault();

                if (nic == null)
                {
                    return null;
                }

                var mac = nic.GetPhysicalAddress().ToString();
                if (string.IsNullOrWhiteSpace(mac))
                {
                    return null;
                }

                return string.Join(":", Enumerable.Range(0, mac.Length / 2).Select(i => mac.Substring(i * 2, 2)));
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        private static string BuildFingerprint(IEnumerable<KeyValuePair<string, string>> entries)
        {
            string payload = string.Join("|", entries.Select(entry => entry.Key + "=" + entry.Value));
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}
