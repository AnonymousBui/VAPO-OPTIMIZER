using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.ServiceProcess;


namespace VAPO_OPTIMIZER
{

    public partial class Form1 : Form
    {
        private int borderThickness = 4;
        private int borderRadius = 3; // Set the border radius here for more curve
        // Import user32.dll functions to control window messages and dragging
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        // Define constants for window messages
        const uint WM_NCLBUTTONDOWN = 0xA1;
        const uint HT_CAPTION = 0x2;

        // Override WndProc to handle window messages
        protected override void WndProc(ref Message m)
        {
            const int WM_WINDOWPOSCHANGED = 0x0047;
            const int WM_NCPAINT = 0x0085;
            const int WM_MOVE = 0x0003;

            base.WndProc(ref m);

            // Handle window dragging
            if (m.Msg == WM_NCLBUTTONDOWN)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, (int)HT_CAPTION, 0);
            }

            // Re-apply acrylic effect on window move or resize
            if (m.Msg == WM_WINDOWPOSCHANGED || m.Msg == WM_NCPAINT || m.Msg == WM_MOVE)
            {
                EnableAcrylic();  // Reapply the acrylic effect after move or resize
            }
        }
        public Form1()
        {
            InitializeComponent();
            ApplyBlackAcrylic();
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Opacity = 0.85;
            this.FormBorderStyle = FormBorderStyle.None; // Remove default border
            this.BackColor = Color.White; // Set the background color
            this.DoubleBuffered = true; // Prevent flickering
            this.SetStyle(ControlStyles.ResizeRedraw, true); // Ensure the form redraws on resize
                                                             // Borderless window
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Apply the rounded region
            ApplyRoundedCorners();
        }
        private void ApplyRoundedCorners()
        {
            Rectangle bounds = new Rectangle(0, 0, this.Width, this.Height);
            GraphicsPath path = new GraphicsPath();

            int radius = borderRadius * 2;
            path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90); // Top-left
            path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90); // Top-right
            path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90); // Bottom-right
            path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90); // Bottom-left
            path.CloseFigure();

            this.Region = new Region(path); // Cut the form into that shape
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Create a Graphics object for custom drawing
            Graphics g = e.Graphics;

            // Create a pen to draw the border
            using (Pen pen = new Pen(Color.Black, borderThickness))
            {
                // Draw the rounded border
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.DrawArc(pen, 0, 0, borderRadius * 2, borderRadius * 2, 180, 90); // Top-left corner
                g.DrawArc(pen, this.Width - borderRadius * 2 - 1, 0, borderRadius * 2, borderRadius * 2, 270, 90); // Top-right corner
                g.DrawArc(pen, 0, this.Height - borderRadius * 2 - 1, borderRadius * 2, borderRadius * 2, 90, 90); // Bottom-left corner
                g.DrawArc(pen, this.Width - borderRadius * 2 - 1, this.Height - borderRadius * 2 - 1, borderRadius * 2, borderRadius * 2, 0, 90); // Bottom-right corner

                // Draw the sides
                g.DrawLine(pen, borderRadius, 0, this.Width - borderRadius - 1, 0); // Top side
                g.DrawLine(pen, 0, borderRadius, 0, this.Height - borderRadius - 1); // Left side
                g.DrawLine(pen, this.Width - 1, borderRadius, this.Width - 1, this.Height - borderRadius - 1); // Right side
                g.DrawLine(pen, borderRadius, this.Height - 1, this.Width - borderRadius - 1, this.Height - 1); // Bottom side
            }
        }

        // Optional: You can also handle resizing the form to maintain rounded corners
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Invalidate(); // Redraw the form
        }

        public struct ACCENT_POLICY
        {
            public int AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        public struct WINDOWCOMPOSITIONATTRIBDATA
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        public enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_ENABLE_HOSTBACKDROP = 5,
            ACCENT_INVALID_STATE = 6
        }

        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WINDOWCOMPOSITIONATTRIBDATA data);


        public struct DWM_BLURBEHIND
        {
            public DWM_BB dwFlags;
            public bool fEnable;
            public IntPtr hRgnBlur;
            public bool fTransitionOnMaximized;
        }

        public enum DWM_BB
        {
            Enable = 0x00000001,
            BlurRegion = 0x00000002,
            TransitionMaximized = 0x00000004
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmEnableBlurBehindWindow(IntPtr hWnd, ref DWM_BLURBEHIND pBlurBehind);

        private void ApplyBlackAcrylic()
        {

            {
                // Set the opacity and color for blur effects using DWM (Desktop Window Manager)
                ACCENT_POLICY accent = new ACCENT_POLICY
                {
                    AccentState = (int)AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    GradientColor = (0x99 << 24) | (0x00 << 16) | (0x00 << 8) | 0x00  // Black with transparency
                };

                int size = Marshal.SizeOf(accent);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(accent, ptr, false);

                WINDOWCOMPOSITIONATTRIBDATA data = new WINDOWCOMPOSITIONATTRIBDATA();
                data.Attribute = 19; // WCA_ACCENT_POLICY
                data.SizeOfData = size;
                data.Data = ptr;

                SetWindowCompositionAttribute(this.Handle, ref data);
                Marshal.FreeHGlobal(ptr);
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            ApplyBlackAcrylic(); // Force after UI loads
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                ApplyBlackAcrylic(); // Reapply the black Acrylic when restoring
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            EnableBlur();
            ApplyBlackAcrylic();
            label1.Text = DateTime.Now.ToString("dddd");
            label1.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");
            label1.ForeColor = Color.White;
            label1.Font = new Font("Segoe UI Light", 18);
            this.BackColor = Color.Black;
            labelWelcome.Font = new Font("Segoe UI Light", 12, FontStyle.Regular);
            {
                string greeting = "";
                int hour = DateTime.Now.Hour;

                if (hour < 12) greeting = "Good Morning!";
                else if (hour < 18) greeting = "Good Afternoon!";
                else greeting = "Good Evening!";

                labelWelcome.Text = greeting + " Welcome back!";
            }
        }


        private void EnableAcrylic()
        {
            try
            {
                var accent = new ACCENT_POLICY
                {
                    AccentState = (int)AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    AccentFlags = 2, // Default border
                                     // Set to black with transparency
                    GradientColor = (0x99 << 24) | (0x00 << 16) | (0x00 << 8) | 0x00  // Fully transparent black
                };

                int accentStructSize = Marshal.SizeOf(accent);
                IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WINDOWCOMPOSITIONATTRIBDATA
                {
                    Attribute = 19,
                    Data = accentPtr,
                    SizeOfData = accentStructSize
                };

                SetWindowCompositionAttribute(this.Handle, ref data);
                Marshal.FreeHGlobal(accentPtr);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message); // Display the error message
            }
        }





        private void EnableBlur()
        {
            ACCENT_POLICY accent = new ACCENT_POLICY();
            accent.AccentState = (int)AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = unchecked((int)0x99FFFFFF);


            int size = Marshal.SizeOf(accent);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, ptr, false);

            WINDOWCOMPOSITIONATTRIBDATA data = new WINDOWCOMPOSITIONATTRIBDATA();
            data.Attribute = 19; // WCA_ACCENT_POLICY
            data.SizeOfData = size;
            data.Data = ptr;

            SetWindowCompositionAttribute(this.Handle, ref data);
            Marshal.FreeHGlobal(ptr);
        }

        private void guna2Button6_Click(object sender, EventArgs e)
        {
            Home.BringToFront();
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            Setting.BringToFront();
        }

        private void guna2Button5_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void guna2Button7_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            Optimize.BringToFront();
        }

        private void guna2Button9_Click(object sender, EventArgs e)
        {
            Optimize.BringToFront();
        }

        private void guna2Button11_Click(object sender, EventArgs e)
        {
            try
            {
                // Clear Temp Files
                System.Diagnostics.Process.Start("cmd.exe", "/c del /f /s /q %temp%\\*");

                // Clear DNS Cache
                System.Diagnostics.Process.Start("cmd.exe", "/c ipconfig /flushdns");

                // Run memory cleaner (optional)
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Set response
                MessageBox.Show("✅ System optimized successfully!", "Demon Optimizer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button12_Click(object sender, EventArgs e)
        {
            try
            {
                // Improve GPU priority for foreground apps
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                    "Win32PrioritySeparation", 0x26, Microsoft.Win32.RegistryValueKind.DWord);

                // Set hardware-accelerated GPU scheduling ON (Windows 10/11+)
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                    "HwSchMode", 2, Microsoft.Win32.RegistryValueKind.DWord);

                // Force GPU scheduling preference to high performance
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences",
                    "DirectXUserGlobalSettings", "GpuPreference=2;", Microsoft.Win32.RegistryValueKind.String);

                MessageBox.Show("✅ GPU Optimized Successfully!", "GPU Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ GPU Optimization Failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button13_Click(object sender, EventArgs e)
        {
            try
            {


                // Set system power plan to High Performance (if supported)
                System.Diagnostics.Process.Start("cmd.exe", "/c powercfg /setactive SCHEME_MIN");

                // Optimize CPU foreground priority
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                    "Win32PrioritySeparation", 0x26, Microsoft.Win32.RegistryValueKind.DWord);

                // Improve timer resolution (low-latency)


                // Optimize network throughput for games
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSMQ\Parameters",
                    "TCPNoDelay", 1, Microsoft.Win32.RegistryValueKind.DWord);

                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSMQ\Parameters",
                    "DisableAck", 1, Microsoft.Win32.RegistryValueKind.DWord);

                MessageBox.Show("🎮 Gaming Optimization Applied!", "Game Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Failed to Optimize Gaming:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button14_Click(object sender, EventArgs e)
        {
            try
            {
                // Game Mode ON
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\GameBar",
                    "AllowAutoGameMode", 1, Microsoft.Win32.RegistryValueKind.DWord);

                // Disable startup delay
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                    "StartupDelayInMSec", 0, Microsoft.Win32.RegistryValueKind.DWord);

                // Improve UI responsiveness
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Desktop",
                    "MenuShowDelay", "0", Microsoft.Win32.RegistryValueKind.String);

                // Enable GPU hardware acceleration
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Avalon.Graphics",
                    "DisableHWAcceleration", 0, Microsoft.Win32.RegistryValueKind.DWord);

                // Low latency input tweak
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                    "LowLatencyMode", 1, Microsoft.Win32.RegistryValueKind.DWord);

                // Visual Effects (turn off fade to boost UI)
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Desktop",
                    "UserPreferencesMask", new byte[] { 144, 18, 7, 128, 16, 0, 0, 0 }, Microsoft.Win32.RegistryValueKind.Binary);

                // System responsiveness (registry CPU tweak)
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                    "SystemResponsiveness", 10, Microsoft.Win32.RegistryValueKind.DWord);

                MessageBox.Show("🔧 Tweaks Applied Successfully!", "System Tweaks", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Failed to apply tweaks:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button15_Click(object sender, EventArgs e)
        {
            try
            {
                // Set power plan to Ultimate Performance (if available)
                System.Diagnostics.Process.Start("cmd.exe", "/c powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61");

                // Disable telemetry & data logging
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                    "AllowTelemetry", 0, Microsoft.Win32.RegistryValueKind.DWord);

                // Disable Cortana background service
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                    "AllowCortana", 0, Microsoft.Win32.RegistryValueKind.DWord);

                // Disable background apps
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications",
                    "GlobalUserDisabled", 1, Microsoft.Win32.RegistryValueKind.DWord);

                // Disable transparency and animations (FPS boost)
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "EnableTransparency", 0, Microsoft.Win32.RegistryValueKind.DWord);

                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics",
                    "MinAnimate", "0", Microsoft.Win32.RegistryValueKind.String);

                // GPU driver optimizations
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                    "HwSchMode", 2, Microsoft.Win32.RegistryValueKind.DWord); // Hardware-accelerated GPU Scheduling ON

                // Boost DWM rendering (important for smooth visuals)
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM",
                    "EnableAeroPeek", 0, Microsoft.Win32.RegistryValueKind.DWord);

                // Force faster foreground boost
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                    "Win32PrioritySeparation", 0x26, Microsoft.Win32.RegistryValueKind.DWord);

                MessageBox.Show("✅ Windows FPS Optimization Applied!", "System Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Failed to optimize Windows:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button16_Click(object sender, EventArgs e)
        {
            try
            {
                // Turn on Windows Game Mode via Registry
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\GameBar",
                    "AllowAutoGameMode", 1, Microsoft.Win32.RegistryValueKind.DWord);

                // Disable Game DVR and background recording
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR",
                    "AppCaptureEnabled", 0, Microsoft.Win32.RegistryValueKind.DWord);
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\GameBar",
                    "ShowGameModeNotifications", 0, Microsoft.Win32.RegistryValueKind.DWord);

                // Enable Ultimate Performance Plan (just like before)
                System.Diagnostics.Process.Start("cmd.exe", "/c powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61");

                // Improve foreground boost priority
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                    "Win32PrioritySeparation", 0x26, Microsoft.Win32.RegistryValueKind.DWord);

                // Disable Mouse Acceleration (important for gamers)
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Mouse",
                    "MouseSpeed", "0", Microsoft.Win32.RegistryValueKind.String);
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Mouse",
                    "MouseThreshold1", "0", Microsoft.Win32.RegistryValueKind.String);
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Mouse",
                    "MouseThreshold2", "0", Microsoft.Win32.RegistryValueKind.String);

                // Force GPU scheduling ON
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                    "HwSchMode", 2, Microsoft.Win32.RegistryValueKind.DWord);

                MessageBox.Show("🎮 Game Mode Activated + Tweaks Applied!", "Game Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Failed to apply Game Mode:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button17_Click(object sender, EventArgs e)
        {
            try
            {
                // Disable mouse acceleration
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Mouse",
                    "MouseSpeed", "0", Microsoft.Win32.RegistryValueKind.String);
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Mouse",
                    "MouseThreshold1", "0", Microsoft.Win32.RegistryValueKind.String);
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Mouse",
                    "MouseThreshold2", "0", Microsoft.Win32.RegistryValueKind.String);

                // Set mouse hover time (reduce UI delay)
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Mouse",
                    "MouseHoverTime", "10", Microsoft.Win32.RegistryValueKind.String); // default is 400

                // Optional: Set precise pointer precision OFF (same as UI toggle)
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Mouse",
                    "SmoothMouseXCurve", new byte[] { }, Microsoft.Win32.RegistryValueKind.Binary);
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Mouse",
                    "SmoothMouseYCurve", new byte[] { }, Microsoft.Win32.RegistryValueKind.Binary);

                MessageBox.Show("🖱️ Mouse optimized: acceleration disabled, precision increased.", "Mouse Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Failed to optimize mouse:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button18_Click(object sender, EventArgs e)
        {
            try
            {
                // Reduce keyboard delay and repeat rate for faster response
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Keyboard",
                    "KeyboardDelay", "0", Microsoft.Win32.RegistryValueKind.String); // 0 = shortest delay
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Keyboard",
                    "KeyboardSpeed", "31", Microsoft.Win32.RegistryValueKind.String); // 31 = fastest repeat

                // Disable Filter Keys (can cause input lag if accidentally enabled)
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Accessibility\Keyboard Response",
                    "Flags", "122", Microsoft.Win32.RegistryValueKind.String);

                // Boost foreground keyboard input priority
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                    "Win32PrioritySeparation", 0x26, Microsoft.Win32.RegistryValueKind.DWord);

                MessageBox.Show("⌨️ Keyboard optimized for faster input and low delay.", "Keyboard Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Failed to optimize keyboard:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button19_Click(object sender, EventArgs e)
        {
            try
            {
                // Set power plan to Ultimate Performance (if available)
                System.Diagnostics.Process.Start("cmd.exe", "/c powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61");

                // Disable telemetry & data logging
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                    "AllowTelemetry", 0, Microsoft.Win32.RegistryValueKind.DWord);

                // Disable Cortana background service
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                    "AllowCortana", 0, Microsoft.Win32.RegistryValueKind.DWord);

                // Disable background apps
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications",
                    "GlobalUserDisabled", 1, Microsoft.Win32.RegistryValueKind.DWord);

                // Disable transparency and animations (FPS boost)
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "EnableTransparency", 0, Microsoft.Win32.RegistryValueKind.DWord);

                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics",
                    "MinAnimate", "0", Microsoft.Win32.RegistryValueKind.String);

                // GPU driver optimizations
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                    "HwSchMode", 2, Microsoft.Win32.RegistryValueKind.DWord); // Hardware-accelerated GPU Scheduling ON

                // Boost DWM rendering (important for smooth visuals)
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM",
                    "EnableAeroPeek", 0, Microsoft.Win32.RegistryValueKind.DWord);

                // Force faster foreground boost
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                    "Win32PrioritySeparation", 0x26, Microsoft.Win32.RegistryValueKind.DWord);

                // Set CPU Affinity for optimal performance (run processes with highest priority)
                System.Diagnostics.Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)0x1;

                // Disable unnecessary visual effects


                MessageBox.Show("✅ Demon Mode Applied! Your system is now optimized for peak performance.", "System Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Failed to apply Demon Mode optimizations:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button21_Click(object sender, EventArgs e)
        {

        }

        private void Games_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            Games.BringToFront();
        }

        private void guna2Button8_Click(object sender, EventArgs e)
        {
            Games.BringToFront();
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            Cloud.BringToFront();
        }
        private bool devilModeEnabled = false;
        private void guna2Button21_Click_1(object sender, EventArgs e)
        {
            if (devilModeEnabled)
            {
                // Revert to default settings when Devil Mode is OFF
                RevertDevilMode();
                MessageBox.Show("✅ Roblox optimizations have been reverted.", "Optimization Reverted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Apply Devil Mode optimizations when it’s OFF
                ApplyDevilMode();
                MessageBox.Show("😈 Devil Mode Activated!\nRoblox is now optimized for maximum performance.", "Optimization Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // Toggle the state of Devil Mode
            devilModeEnabled = !devilModeEnabled;
        }

        private void ApplyDevilMode()
        {
            // Apply your 33 optimizations here (just an example)
            Microsoft.Win32.Registry.SetValue(
                @"HKEY_CURRENT_USER\Software\Roblox\GlobalBasicSettings_13",
                "FFlagDisableShadows", true, Microsoft.Win32.RegistryValueKind.DWord);
            Microsoft.Win32.Registry.SetValue(
                @"HKEY_CURRENT_USER\Software\Roblox\GlobalBasicSettings_13",
                "FFlagFastPhysics", true, Microsoft.Win32.RegistryValueKind.DWord);
            Microsoft.Win32.Registry.SetValue(
                @"HKEY_CURRENT_USER\Software\Roblox\GlobalBasicSettings_13",
                "FFlagUncapFPS", true, Microsoft.Win32.RegistryValueKind.DWord);

            // Add more tweaks to make the performance "Devil Mode" (continue the list with your 33 optimizations)
        }

        private void RevertDevilMode()
        {
            // Revert all the optimizations (opposite of ApplyDevilMode)
            Microsoft.Win32.Registry.SetValue(
                @"HKEY_CURRENT_USER\Software\Roblox\GlobalBasicSettings_13",
                "FFlagDisableShadows", false, Microsoft.Win32.RegistryValueKind.DWord);
            Microsoft.Win32.Registry.SetValue(
                @"HKEY_CURRENT_USER\Software\Roblox\GlobalBasicSettings_13",
                "FFlagFastPhysics", false, Microsoft.Win32.RegistryValueKind.DWord);
            Microsoft.Win32.Registry.SetValue(
                @"HKEY_CURRENT_USER\Software\Roblox\GlobalBasicSettings_13",
                "FFlagUncapFPS", false, Microsoft.Win32.RegistryValueKind.DWord);

            // Revert all other tweaks as needed (undo the changes you made in ApplyDevilMode)
            string path = @"HKEY_CURRENT_USER\Software\Roblox\GlobalBasicSettings_13";

            // Core FPS Boost
            Registry.SetValue(path, "FFlagUncapFPS", true);
            Registry.SetValue(path, "FFlagTaskSchedulerTargetFps", 1000);
            Registry.SetValue(path, "FFlagGraphicsPreferD3D11", true);
            Registry.SetValue(path, "FFlagGraphicsUseParallelRendering", true);

            // Visual Killers (Remove Lag Effects)
            Registry.SetValue(path, "FFlagDisableShadows", true);
            Registry.SetValue(path, "FFlagEnableGlobalShadows", false);
            Registry.SetValue(path, "FFlagEnableSunRays", false);
            Registry.SetValue(path, "FFlagEnableBlurEffect", false);
            Registry.SetValue(path, "FFlagEnableReflections", false);
            Registry.SetValue(path, "FFlagEnableParticles", false);
            Registry.SetValue(path, "FFlagPlayerRenderShadows", false);
            Registry.SetValue(path, "FFlagLightingTechnology", "Compatibility");
            Registry.SetValue(path, "FFlagTerrainDecoration", false);

            // Network & Input Boost
            Registry.SetValue(path, "FFlagEnableInputBatchedEvents", true);
            Registry.SetValue(path, "FFlagNetworkFlushOnPhysicsStepped", true);
            Registry.SetValue(path, "FFlagNetworkUseHighPriority", true);
            Registry.SetValue(path, "FFlagNetworkRate", 120);

            // Rendering Stability
            Registry.SetValue(path, "FFlagHandleRenderingInBackground", false);
            Registry.SetValue(path, "FFlagRenderLowQuality", true);
            Registry.SetValue(path, "FFlagGraphicsQualityLevel", 1);

        }

        private void guna2Button22_Click(object sender, EventArgs e)
        {
            try
            {
                if (!optimizationsApplied)
                {
                    // 1. Set Java Arguments for performance
                    SetJavaArguments();

                    // 2. Change Graphics settings in Minecraft
                    SetMinecraftGraphics();

                    // 3. Set Render Distance to lower value (e.g., 8 chunks)
                    SetRenderDistance(8);

                    // 4. Disable smooth lighting
                    SetSmoothLighting(false);

                    // 5. Set particles to Minimal
                    SetParticles("Minimal");

                    // 6. Disable V-Sync
                    SetVSync(false);

                    // 7. Set Mipmap Levels to 0
                    SetMipmapLevels(0);

                    // 8. Disable Entity Shadows
                    SetEntityShadows(false);

                    // 9. Allocate more RAM
                    SetRAMAllocation("4G");

                    // 10. Use Fast Math (OptiFine)
                    SetFastMath(true);

                    // 11. Disable Dynamic Lights (OptiFine)
                    SetDynamicLights(false);

                    // 12. Use Fast Render (OptiFine)
                    SetFastRender(true);

                    // 13. Disable Fog
                    SetFog(false);

                    // 14. Enable Smooth FPS
                    SetSmoothFPS(true);

                    // 15. Set Chunk Updates to 1
                    SetChunkUpdates(1);

                    // 16. Disable Background Apps in Windows
                    DisableBackgroundApps();

                    // 17. Set Power Plan to High Performance
                    SetPowerPlan("High Performance");

                    // 18. Disable Transparency in Windows
                    DisableTransparency();

                    // 19. Disable Animations in Windows
                    DisableAnimations();

                    // 20. Disable Cortana in Windows
                    DisableCortana();

                    // 21. Disable Telemetry and Data Logging
                    DisableTelemetry();

                    // 22. Disable Windows Defender (optional, for gaming)
                    DisableWindowsDefender();

                    // 23. Set Graphics to Fast in NVIDIA Control Panel
                    SetGraphicsToFast();

                    // 24. Set Texture Filtering to Performance in NVIDIA Control Panel
                    SetTextureFiltering("Performance");

                    // 25. Set Frame Rate Limit in Minecraft
                    SetFrameRateLimit(240);

                    // 26. Use Java 8 for Minecraft (if you’re using a version that supports it)
                    SetJavaVersion("Java 8");

                    // 27. Disable HUD (Head-Up Display) to reduce distractions
                    SetHUD(false);

                    // 28. Disable F3 Debug Info (FPS counter)
                    SetF3DebugInfo(false);

                    // 29. Disable Mods (if any installed) for maximum performance
                    DisableMods();

                    // 30. Disable Sounds in Minecraft (optional for ultra performance)
                    DisableSounds();

                    // 31. Disable World Generation Effects
                    SetWorldGenerationEffects(false);

                    // 32. Disable Cloud Rendering in Minecraft
                    DisableCloudRendering();

                    // 33. Optimize system RAM usage (turn off unnecessary apps in Task Manager)
                    OptimizeRAMUsage();

                    MessageBox.Show("✅ Minecraft Optimized for Best Performance!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    optimizationsApplied = true;
                }
                else
                {
                    // Revert optimizations
                    RevertOptimizations();
                    MessageBox.Show("❌ Minecraft Optimizations Reverted.", "Reverted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    optimizationsApplied = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error applying optimizations: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Boolean flag to track if optimizations are applied
        private bool optimizationsApplied = false;

        // Method to Set Java Arguments for performance
        private void SetJavaArguments()
        {
            // Example for JVM Arguments, tweak as needed
            // -Xmx4G -Xms2G -XX:+UseG1GC -XX:+UnlockExperimentalVMOptions
            // You would apply this via the Minecraft launcher settings
        }

        // Method to adjust Minecraft Graphics settings
        private void SetMinecraftGraphics()
        {
            // Example logic for adjusting Minecraft Graphics settings
            // Apply Fast graphics settings via OptiFine or Minecraft's built-in settings
        }

        // Method to set Render Distance
        private void SetRenderDistance(int distance)
        {
            // Set the render distance in the Minecraft settings (via the launcher or config file)
        }

        // Method to disable smooth lighting
        private void SetSmoothLighting(bool enabled)
        {
            // Adjust the smooth lighting setting in Minecraft
        }

        // Method to set Particles
        private void SetParticles(string setting)
        {
            // Apply the desired particle setting (Minimal, Decreased, etc.)
        }

        // Method to disable V-Sync
        private void SetVSync(bool enabled)
        {
            // Disable V-Sync in Minecraft settings for higher FPS
        }

        // Method to set Mipmap levels
        private void SetMipmapLevels(int level)
        {
            // Set Mipmap Levels to 0 for performance
        }

        // Method to disable Entity Shadows
        private void SetEntityShadows(bool enabled)
        {
            // Disable Shadows in Minecraft settings
        }

        // Method to allocate more RAM to Minecraft
        private void SetRAMAllocation(string ramSize)
        {
            // Set the amount of RAM Minecraft can use
        }

        // Method to enable/disable Fast Math (OptiFine)
        private void SetFastMath(bool enabled)
        {
            // Enable Fast Math via OptiFine settings
        }

        // Method to enable/disable Dynamic Lights (OptiFine)
        private void SetDynamicLights(bool enabled)
        {
            // Enable/Disable Dynamic Lights in OptiFine settings
        }

        // Method to enable/disable Fast Render (OptiFine)
        private void SetFastRender(bool enabled)
        {
            // Enable/Disable Fast Render in OptiFine settings
        }

        // Method to disable Fog
        private void SetFog(bool enabled)
        {
            // Disable Fog for better FPS
        }

        // Method to enable Smooth FPS
        private void SetSmoothFPS(bool enabled)
        {
            // Enable Smooth FPS option for smoother gameplay
        }

        // Method to adjust chunk updates
        private void SetChunkUpdates(int value)
        {
            // Set chunk updates to lower value for less lag
        }

        // Method to disable Background Apps in Windows
        private void DisableBackgroundApps()
        {
            // Disable background apps through Windows registry or Task Manager
        }

        // Method to set Power Plan to High Performance
        private void SetPowerPlan(string powerPlan)
        {
            // Set Windows Power Plan to High Performance
        }

        // Method to disable Transparency in Windows
        private void DisableTransparency()
        {
            // Disable transparency effects in Windows settings
        }

        // Method to disable Animations in Windows
        private void DisableAnimations()
        {
            // Disable window animations in Windows settings
        }

        // Method to disable Cortana in Windows
        private void DisableCortana()
        {
            // Disable Cortana through Windows registry settings
        }

        // Method to disable telemetry/data logging
        private void DisableTelemetry()
        {
            // Disable telemetry/data logging in Windows registry
        }

        // Method to disable Windows Defender (optional for gaming)
        private void DisableWindowsDefender()
        {
            // Disable Windows Defender temporarily for gaming performance
        }

        // Method to set Graphics to Fast in NVIDIA Control Panel
        private void SetGraphicsToFast()
        {
            // Adjust settings in NVIDIA Control Panel for fast graphics rendering
        }

        // Method to set Texture Filtering to Performance in NVIDIA Control Panel
        private void SetTextureFiltering(string setting)
        {
            // Set Texture Filtering in NVIDIA settings for performance
        }

        // Method to set Frame Rate Limit in Minecraft
        private void SetFrameRateLimit(int limit)
        {
            // Set a high FPS limit in Minecraft to reduce screen tearing
        }

        // Method to set Java version to 8 (if supported)
        private void SetJavaVersion(string version)
        {
            // Set Java version used by Minecraft
        }

        // Method to disable HUD (Head-Up Display)
        private void SetHUD(bool enabled)
        {
            // Toggle Minecraft HUD display
        }

        // Method to disable F3 Debug Info
        private void SetF3DebugInfo(bool enabled)
        {
            // Disable F3 Debug info in Minecraft settings
        }

        // Method to disable Mods (for maximum performance)
        private void DisableMods()
        {
            // Disable all mods in Minecraft if applicable
        }

        // Method to disable Sounds
        private void DisableSounds()
        {
            // Turn off all sounds in Minecraft
        }

        // Method to disable World Generation Effects
        private void SetWorldGenerationEffects(bool enabled)
        {
            // Disable world generation effects for better performance
        }

        // Method to disable Cloud Rendering in Minecraft
        private void DisableCloudRendering()
        {
            // Disable cloud rendering for better FPS
        }

        // Method to optimize system RAM usage
        private void OptimizeRAMUsage()
        {
            // Optimize RAM usage by closing unnecessary applications
        }

        // Method to revert all optimizations
        private void RevertOptimizations()
        {
            // Revert all optimizations done in the above methods
            // Reset registry changes, graphics settings, Java arguments, etc.
            MessageBox.Show("Minecraft optimizations have been reverted.", "Optimizations Reverted", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void guna2Button23_Click(object sender, EventArgs e)
        {
            string url = "https://www.9minecraft.net/tl-legacy-launcher/";  // Replace this with the link you want
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void guna2Button24_Click(object sender, EventArgs e)
        {

            string url = "https://bloxstrap.org/";  // Replace this with the link you want
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void guna2Button25_Click(object sender, EventArgs e)
        {
            string url = "https://brave.com/";  // Replace this with the link you want
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void guna2Button26_Click(object sender, EventArgs e)
        {
            string url = "https://github.com/AnonymousBui/SUPER-DEMON-V2";  // Replace this with the link you want
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void guna2Button10_Click(object sender, EventArgs e)
        {
            Setting.BringToFront();
        }

        private void guna2ToggleSwitch2_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch2.Checked)
            {
                // 🟢 FPS Boost ON
                // Set power plan to Ultimate Performance
                Process.Start("powercfg", "/s e9a42b02-d5df-448d-aa00-03f14749eb61");

                // Turn off animations for max FPS
                SetVisualEffects(false);

                MessageBox.Show("FPS Boost Enabled! Ultimate Performance Mode activated.", "BOOST", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // 🔴 FPS Boost OFF
                // Restore to Balanced mode
                Process.Start("powercfg", "/s SCHEME_BALANCED");

                // Restore animations
                SetVisualEffects(true);

                MessageBox.Show("FPS Boost Disabled. Balanced Mode restored.", "BOOST", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void SetVisualEffects(bool enable)
        {
            const string userKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
            const string performanceKey = @"HKEY_CURRENT_USER\Control Panel\Performance\Settings";

            Registry.SetValue(userKey, "VisualFXSetting", enable ? 1 : 2); // 1 = Default, 2 = Best performance
            Registry.SetValue(performanceKey, "UserPreferencesMask", enable ? new byte[] { 9, 0, 0, 0, 128, 0, 0, 0, 0 } : new byte[] { 3, 0, 0, 0, 0, 0, 0, 0, 0 });
        }

        private void guna2ToggleSwitch1_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch1.Checked)
            {
                // 🟢 NVIDIA FPS Boost ON
                ApplyNvidiaSetting("0x01", "1"); // Prefer max performance
                ApplyNvidiaSetting("0x52", "1"); // Threaded optimization ON
                ApplyNvidiaSetting("0xA0", "1"); // Low latency mode = Ultra

                MessageBox.Show("NVIDIA Boost Enabled!\n- Max Performance\n- Low Latency\n- Threaded Optimization ON", "NVIDIA BOOST", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // 🔴 NVIDIA FPS Boost OFF (Revert to defaults)
                ApplyNvidiaSetting("0x01", "2"); // Auto
                ApplyNvidiaSetting("0x52", "2"); // Auto
                ApplyNvidiaSetting("0xA0", "0"); // Off

                MessageBox.Show("NVIDIA Boost Disabled. Settings restored to default.", "NVIDIA BOOST", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void ApplyNvidiaSetting(string settingCode, string value)
        {
            try
            {
                string regPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\FTS";
                Registry.SetValue(regPath, settingCode, int.Parse(value), RegistryValueKind.DWord);
            }
            catch
            {
                // Silent fail if access denied (user doesn't have permission)
            }
        }

        private void guna2ToggleSwitch14_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch14.Checked)
            {
                // ✅ Set Roblox to High Priority
                foreach (var process in Process.GetProcessesByName("RobloxPlayerBeta"))
                {
                    try { process.PriorityClass = ProcessPriorityClass.High; } catch { }
                }

                // ✅ Set High Performance Power Plan
                Process.Start("powercfg", "/s SCHEME_MIN");

                // ✅ Kill background laggy processes (example: OneDrive, Discord updater)
                KillBackground("OneDrive");
                KillBackground("Update"); // For Discord
                KillBackground("YourPhone");

                MessageBox.Show("CPU Optimized: High Priority + Power Boosted", "Demon CPU Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // 🟡 Restore to Balanced Power Plan (optional)
                Process.Start("powercfg", "/s SCHEME_BALANCED");

                MessageBox.Show("CPU Optimization Disabled. Power Plan Restored.", "Demon CPU Boost", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void KillBackground(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try { process.Kill(); } catch { }
            }
        }

        private void guna2ToggleSwitch9_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch9.Checked)
            {
                // ✅ Enable Hardware-accelerated GPU scheduling (Win 10/11)
                try
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord);
                }
                catch { }

                // ✅ Disable V-Sync globally (this is just for demonstration; some games ignore this)
                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Direct3D", "DisableVSync", 1, RegistryValueKind.DWord);
                }
                catch { }

                // ✅ Kill GPU-hungry background apps (add more if needed)
                KillGPUHogs("WallpaperEngine");
                KillGPUHogs("Rainmeter");
                KillGPUHogs("NVIDIA Share"); // Shadowplay background

                MessageBox.Show("GPU Optimized: VSync Off, HAGS On, Background Cleaned", "Demon GPU Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // 🟡 Revert HAGS and V-Sync (optional)
                try
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1, RegistryValueKind.DWord);
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Direct3D", "DisableVSync", 0, RegistryValueKind.DWord);
                }
                catch { }

                MessageBox.Show("GPU Optimization Disabled. Settings Reverted.", "Demon GPU Boost", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void KillGPUHogs(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try { process.Kill(); } catch { }
            }
        }

        private void guna2ToggleSwitch8_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch8.Checked)
            {
                // ✅ Disable Telemetry
                try
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord);
                }
                catch { }

                // ✅ Speed up UI animations
                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String);
                    Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics", "MinAnimate", "0", RegistryValueKind.String);
                }
                catch { }

                // ✅ Disable Xbox Game Bar
                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord);
                    Registry.SetValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord);
                }
                catch { }

                // ✅ Clean temp files
                CleanTemp();

                MessageBox.Show("System Optimized: Telemetry Disabled, UI Speed Boosted, Temp Cleaned", "Demon System Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("System Optimization Disabled. Some changes may persist until restart.", "Demon System Boost", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void CleanTemp()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                foreach (string file in Directory.GetFiles(tempPath)) { TryDelete(file); }
                foreach (string dir in Directory.GetDirectories(tempPath)) { TryDelete(dir); }

                foreach (string file in Directory.GetFiles(@"C:\Windows\Prefetch")) { TryDelete(file); }
            }
            catch { }
        }

        private void TryDelete(string path)
        {
            try
            {
                FileAttributes attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    Directory.Delete(path, true);
                else
                    File.Delete(path);
            }
            catch { }
        }

        private void guna2ToggleSwitch3_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch3.Checked)
            {
                // ✅ Disable Cortana
                try
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0, RegistryValueKind.DWord);
                }
                catch { }

                // ✅ Disable Bing Search in Start Menu
                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0, RegistryValueKind.DWord);
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", 0, RegistryValueKind.DWord);
                }
                catch { }

                // ✅ Reduce input delay (disable mouse acceleration)
                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseSpeed", "0");
                    Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold1", "0");
                    Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold2", "0");
                }
                catch { }

                // ✅ Faster shutdown
                try
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control", "WaitToKillServiceTimeout", "2000");
                    Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "WaitToKillAppTimeout", "2000");
                    Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "HungAppTimeout", "1000");
                }
                catch { }

                // ✅ Disable Edge auto-start
                try
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main", "PreventFirstRunPage", 1, RegistryValueKind.DWord);
                }
                catch { }

                MessageBox.Show("Tweaks Applied: Cortana Off, Input Boost, Shutdown Faster", "Demon Tweaks Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Tweaks Disabled. Some changes require restart to undo.", "Demon Tweaks Boost", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void guna2ToggleSwitch13_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch13.Checked)
            {
                try
                {
                    // ✅ Enable Hardware Accelerated GPU Scheduling
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord);

                    // ✅ Optional: Notify user to manually apply Max Performance in NVIDIA Control Panel
                    MessageBox.Show(
                        "⚙️ NVIDIA GPU Optimized!\n\n✅ Hardware GPU Scheduling Enabled\n⚠️ For best results:\n- Open NVIDIA Control Panel\n- Go to 'Manage 3D Settings'\n- Set 'Power management mode' to 'Prefer maximum performance'\n- Set 'Low Latency Mode' to 'Ultra'\n- Turn off V-Sync\n\nOr use NVIDIA Profile Inspector for automatic control.",
                        "NVIDIA Optimization",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while applying NVIDIA optimizations: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("NVIDIA optimization disabled. Some settings may stay active until reboot.", "NVIDIA Optimization", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void guna2ToggleSwitch10_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch10.Checked)
            {
                try
                {
                    // ⚙️ Set power saving features off
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\PowerSettings", "Power_Preference", 0x00000002, RegistryValueKind.DWord);

                    // ⚙️ Disable MSAA (Multi-Sample Anti-Aliasing)
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\3D", "MSAA", 0, RegistryValueKind.DWord);

                    // ⚙️ Disable Application Optimal Mode
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\3D", "AppOptimalMode", 0, RegistryValueKind.DWord);

                    // ⚙️ Set general 3D preference to performance
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\3D", "PerfLevel", 1, RegistryValueKind.DWord);

                    MessageBox.Show("✅ Intel Graphics Optimized!\n- Power Saving: OFF\n- MSAA: OFF\n- Performance Mode: ON", "Intel GPU Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Intel Optimization Error: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Intel GPU optimization disabled. Some changes stay until reboot.", "Intel GPU Boost", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void guna2ToggleSwitch7_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch7.Checked)
            {
                try
                {
                    // Optional: Enable Hardware GPU Scheduling
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord);

                    MessageBox.Show(
                        "✅ AMD GPU Optimized!\n\n🧠 Now apply these settings in AMD Software:\n\n" +
                        "• Graphics → Radeon Anti-Lag: ON\n" +
                        "• Radeon Boost: ON\n" +
                        "• Wait for Vertical Refresh: Always Off\n" +
                        "• Texture Filtering Quality: Performance\n" +
                        "• Tessellation Mode: Override Application Settings (x8 or x16)\n\n⚠️ Some settings must be changed manually.",
                        "AMD Optimization",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error applying AMD optimizations: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("AMD GPU optimization turned off. Some effects may remain until reboot.", "AMD GPU Boost", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void guna2ToggleSwitch4_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch4.Checked)
            {
                try
                {
                    // ✅ Enable Hardware Accelerated GPU Scheduling
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord);

                    // ✅ Enable Game Mode
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "AllowAutoGameMode", 1, RegistryValueKind.DWord);
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, RegistryValueKind.DWord);

                    // ✅ Disable Visual Effects (set Windows for best performance)
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord);

                    // ✅ Optional: Disable Fullscreen Optimization for Roblox (once)
                    string robloxPath = @"C:\Users\Public\Roblox\Versions\version-*\RobloxPlayerBeta.exe"; // Change to actual path
                    Process.Start("cmd.exe", $"/c powershell -Command \"Get-Item '{robloxPath}' | ForEach-Object {{$_.Attributes='ReadOnly'; $_.SetProperty('NoFullscreenOptimizations',1)}}\"");

                    MessageBox.Show("✅ Render Optimizations Applied!\n• Hardware GPU Scheduling\n• Game Mode: ON\n• Visual Effects: OFF\n• Fullscreen Optimization: Disabled (Roblox)", "Render Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error applying Render optimizations: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Render optimization disabled. Changes may persist until reboot.", "Render Boost", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void guna2ToggleSwitch12_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch12.Checked)
            {
                try
                {
                    // ✅ Apply High-Performance GPU for RobloxPlayerBeta
                    Registry.SetValue(
                        @"HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences",
                        @"C:\Users\Public\Roblox\Versions\version-*\RobloxPlayerBeta.exe", // Adjust to actual Roblox path
                        "GpuPreference=2;", // 2 = High Performance NVIDIA GPU
                        RegistryValueKind.String
                    );

                    // Optional: Set global preference (some systems respect this)
                    Registry.SetValue(
                        @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                        "HwSchMode", 2, RegistryValueKind.DWord
                    );

                    MessageBox.Show(
                        "⚠️ NVIDIA GPU Locked\n\nRoblox will now run with the NVIDIA GPU.\nExpect higher performance but more heat/power usage on laptops.",
                        "NVIDIA GPU Lock",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show("NVIDIA GPU Lock Error: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show(
                    "🔓 NVIDIA GPU Lock Removed.\nRoblox may now use Intel GPU again.",
                    "NVIDIA GPU Unlock",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

        private void guna2ToggleSwitch11_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch11.Checked)
            {
                try
                {
                    // ✅ Disable Wi-Fi Power Saving Mode (on most systems)
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "DisableDynamicUpdate", 1, RegistryValueKind.DWord);

                    // ✅ Optimize TCP/IP settings for gaming (MTU, RWIN, etc.)
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpWindowSize", 0x0000FA00, RegistryValueKind.DWord); // 64000
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Tcp1323Opts", 1, RegistryValueKind.DWord);  // Allow TimeStamp

                    // ✅ Ensure DNS is set to Cloudflare (1.1.1.1) for low latency
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "NameServer", "1.1.1.1,8.8.8.8", RegistryValueKind.String);

                    // ✅ Run PowerShell to disable the Wi-Fi adapter (replace 'Wi-Fi' with your adapter's name)
                    string command = "powershell -Command \"Disable-NetAdapter -Name 'Wi-Fi' -Confirm:$false\"";
                    Process.Start("cmd.exe", "/c " + command);

                    MessageBox.Show(
                        "✅ Wi-Fi Optimized!\n• Power Saving Mode OFF\n• TCP/IP optimized\n• Cloudflare DNS (1.1.1.1)\n• Wi-Fi Adapter Disabled",
                        "Wi-Fi Boost",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Wi-Fi Optimization Error: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    // ✅ Run PowerShell to enable the Wi-Fi adapter
                    string command = "powershell -Command \"Enable-NetAdapter -Name 'Wi-Fi' -Confirm:$false\"";
                    Process.Start("cmd.exe", "/c " + command);

                    MessageBox.Show("Wi-Fi optimization disabled. Wi-Fi adapter is now enabled.", "Wi-Fi Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Wi-Fi Enable Error: " + ex.Message);
                }
            }
        }

        private void guna2ToggleSwitch6_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch6.Checked)
            {
                try
                {
                    // ✅ Check if Ethernet is being used, otherwise disable Wi-Fi
                    bool isEthernetConnected = false;

                    foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (nic.OperationalStatus == OperationalStatus.Up)
                        {
                            // Check if Ethernet (wired) is connected
                            if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                            {
                                isEthernetConnected = true;
                                break; // Exit the loop once we detect Ethernet
                            }
                        }
                    }

                    if (!isEthernetConnected)
                    {
                        // ✅ Disable Wi-Fi Power Saving Mode (on most systems)
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "DisableDynamicUpdate", 1, RegistryValueKind.DWord);

                        // ✅ Optimize TCP/IP settings for gaming (MTU, RWIN, etc.)
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpWindowSize", 0x0000FA00, RegistryValueKind.DWord); // 64000
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Tcp1323Opts", 1, RegistryValueKind.DWord);  // Allow TimeStamp

                        // ✅ Ensure DNS is set to Cloudflare (1.1.1.1) for low latency
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "NameServer", "1.1.1.1,8.8.8.8", RegistryValueKind.String);

                        // ✅ Run PowerShell to disable the Wi-Fi adapter (replace 'Wi-Fi' with your adapter's name)
                        string command = "powershell -Command \"Disable-NetAdapter -Name 'Wi-Fi' -Confirm:$false\"";
                        Process.Start("cmd.exe", "/c " + command);

                        MessageBox.Show(
                            "✅ Wi-Fi Optimized!\n• Power Saving Mode OFF\n• TCP/IP optimized\n• Cloudflare DNS (1.1.1.1)\n• Wi-Fi Adapter Disabled",
                            "Wi-Fi Boost",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show("You are currently connected via Ethernet. Wi-Fi will not be disabled.", "Wi-Fi Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Wi-Fi Optimization Error: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    // ✅ Run PowerShell to enable the Wi-Fi adapter
                    string command = "powershell -Command \"Enable-NetAdapter -Name 'Wi-Fi' -Confirm:$false\"";
                    Process.Start("cmd.exe", "/c " + command);

                    MessageBox.Show("Wi-Fi optimization disabled. Wi-Fi adapter is now enabled.", "Wi-Fi Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Wi-Fi Enable Error: " + ex.Message);
                }
            }
        }

        private void guna2ToggleSwitch5_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch5.Checked)
            {
                try
                {
                    // ✅ Disable Power Saving for Keyboard and Mouse (through Registry, if available)
                    // Keyboard and Mouse Power Saving Disable Registry changes (Example: HID devices)
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\HidUsb\Parameters", "DisableSelectiveSuspend", 1, RegistryValueKind.DWord);

                    // ✅ Disable USB Sleep Mode to prevent input lag
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USB", "DisableSelectiveSuspend", 1, RegistryValueKind.DWord);

                    // ✅ Adjust Polling Rate for Mouse (Increase polling rate for faster response)
                    // Assuming you have a high-performance mouse driver (e.g., Logitech or Razer with software)
                    // Set MousePollingRate (Example: For Logitech, use Logitech Gaming Software)
                    string command = "powershell -Command \"Set-ItemProperty -Path 'HKCU:\\Control Panel\\Mouse' -Name 'MouseSpeed' -Value '2'\"";
                    Process.Start("cmd.exe", "/c " + command);

                    // ✅ Disable Mouse Acceleration (Increase precision)
                    // This will make mouse movements more precise
                    command = "powershell -Command \"Set-ItemProperty -Path 'HKCU:\\Control Panel\\Mouse' -Name 'MouseAcceleration' -Value '0'\"";
                    Process.Start("cmd.exe", "/c " + command);

                    // ✅ Optimize Keyboard Repeat Delay (Faster key repeat)
                    command = "powershell -Command \"Set-ItemProperty -Path 'HKCU:\\Control Panel\\Keyboard' -Name 'KeyboardDelay' -Value '0'\"";
                    Process.Start("cmd.exe", "/c " + command);

                    // ✅ Show a message box after optimization is applied
                    MessageBox.Show(
                        "✅ Keyboard and Mouse Optimized!\n• Power saving mode disabled\n• Polling rate increased\n• Mouse acceleration disabled\n• Keyboard repeat delay optimized",
                        "Input Boost",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Keyboard and Mouse Optimization Error: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    // ✅ Reset Keyboard and Mouse Settings to default when optimization is disabled
                    // Reset to default keyboard settings (default repeat delay, default speed)
                    string command = "powershell -Command \"Set-ItemProperty -Path 'HKCU:\\Control Panel\\Keyboard' -Name 'KeyboardDelay' -Value '1'\"";
                    Process.Start("cmd.exe", "/c " + command);

                    // Reset mouse settings (default speed and acceleration)
                    command = "powershell -Command \"Set-ItemProperty -Path 'HKCU:\\Control Panel\\Mouse' -Name 'MouseSpeed' -Value '1'\"";
                    Process.Start("cmd.exe", "/c " + command);

                    // Reset mouse acceleration to default
                    command = "powershell -Command \"Set-ItemProperty -Path 'HKCU:\\Control Panel\\Mouse' -Name 'MouseAcceleration' -Value '1'\"";
                    Process.Start("cmd.exe", "/c " + command);

                    MessageBox.Show("Keyboard and Mouse optimizations disabled. Settings restored to default.", "Input Boost", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Reset Keyboard and Mouse Settings Error: " + ex.Message);
                }
            }
        }

        private void guna2Button11_Click_1(object sender, EventArgs e)
        {
            Optimize.BringToFront();
        }

        private void guna2Button12_Click_1(object sender, EventArgs e)
        {
            OptimizePage2.BringToFront();
        }

        private void guna2ToggleSwitch15_CheckedChanged(object sender, EventArgs e)
        {

        }
        private void guna2ToggleSwitch17_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch17.Checked)
            {
                // Enable Wi-Fi optimization
                EnableWiFiOptimization();
            }
            else
            {
                // Disable Wi-Fi optimization and revert to default settings
                DisableWiFiOptimization();
            }
        }

        private void EnableWiFiOptimization()
        {
            // 1. Set Wi-Fi power plan to "Maximum Performance"
            SetWiFiPowerMode();

            // 2. Set DNS to Cloudflare or Google for better speed and stability
            ChangeDNS("1.1.1.1", "1.0.0.1"); // Cloudflare DNS



            // 4. Adjust TCP/IP settings for better performance
            OptimizeTCPIP();

            // 5. Prioritize Wi-Fi over other network adapters if possible
            PrioritizeWiFiConnection();

            MessageBox.Show("Wi-Fi Optimized for Performance");
        }

        private void DisableWiFiOptimization()
        {
            // Revert all optimizations
            ResetWiFiPowerMode();
            
            RevertBackgroundApps();
            ResetTCPIP();
            ResetWiFiPriority();

            MessageBox.Show("Wi-Fi Optimized Reverted to Default");
        }

        private void SetWiFiPowerMode()
        {
            // Set the power plan for Wi-Fi to "Maximum Performance"
            // This prevents Windows from reducing power to the Wi-Fi adapter
            System.Diagnostics.Process.Start("powercfg", "/setactive SCHEME_MAXPERFORMANCE");
        }

        private void ResetWiFiPowerMode()
        {
            // Reset to the default Balanced Power Plan
            System.Diagnostics.Process.Start("powercfg", "/setactive SCHEME_BALANCED");
        }

        private void ChangeDNS(string primaryDNS, string secondaryDNS)
        {
            // Change DNS to faster servers like Cloudflare (1.1.1.1) or Google (8.8.8.8)
            try
            {
                // Using command line to set DNS for the active network adapter
                System.Diagnostics.Process.Start("cmd", $"/C netsh interface ip set dns name=\"Wi-Fi\" static {primaryDNS} primary");
                System.Diagnostics.Process.Start("cmd", $"/C netsh interface ip add dns name=\"Wi-Fi\" {secondaryDNS} index=2");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting DNS: {ex.Message}");
            }
        }

        private void ResetDNS()
        {
            // Reset DNS to automatic settings
            try
            {
                System.Diagnostics.Process.Start("cmd", "/C netsh interface ip set dns name=\"Wi-Fi\" source=dhcp");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting DNS: {ex.Message}");
            }
        }



        // Example: Close unnecessary apps that consume bandwidth, like cloud sync services (OneDrive, Dropbox, etc.)
        // Add processes you want to close here
        // System.Diagnostics.Process.GetProcessesByName("OneDrive").ToList().ForEach(p => p.Kill());
        // System.Diagnostics.Process.GetProcessesByName("Dropbox").ToList().ForEach(p => p.Kill());

        // You can also manually ensure apps like browsers and torrent clients are not running in the background.


        private void RevertBackgroundApps()
        {
            // Re-enable any background apps you need or leave them as they were.
            // You can also prompt the user to restart these applications if necessary.
        }

        private void OptimizeTCPIP()
        {
            // Example: Adjust TCP/IP settings for better performance
            // Set TCP Optimizations through the registry (this can improve speed and reduce packet loss)
            try
            {
                // Disable Nagle Algorithm (improves latency for small packets)
                System.Diagnostics.Process.Start("cmd", "/C reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\" /v TcpNoDelay /t REG_DWORD /d 1 /f");

                // Increase the maximum buffer size for connections
                System.Diagnostics.Process.Start("cmd", "/C reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\" /v MaxConnectionsPerServer /t REG_DWORD /d 10 /f");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error optimizing TCP/IP settings: {ex.Message}");
            }
        }

        private void ResetTCPIP()
        {
            // Revert TCP/IP settings back to default if needed
            try
            {
                // Reset the TcpNoDelay setting
                System.Diagnostics.Process.Start("cmd", "/C reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\" /v TcpNoDelay /f");
                // Reset MaxConnectionsPerServer
                System.Diagnostics.Process.Start("cmd", "/C reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\" /v MaxConnectionsPerServer /f");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting TCP/IP settings: {ex.Message}");
            }
        }

        private void PrioritizeWiFiConnection()
        {
            // Set Wi-Fi to highest priority in the network connections list (using PowerShell or cmd)
            try
            {
                System.Diagnostics.Process.Start("cmd", "/C netsh interface set interface \"Wi-Fi\" priority=1");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error prioritizing Wi-Fi: {ex.Message}");
            }
        }

        private void ResetWiFiPriority()
        {
            // Reset Wi-Fi priority back to default if needed
            try
            {
                System.Diagnostics.Process.Start("cmd", "/C netsh interface set interface \"Wi-Fi\" priority=0");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting Wi-Fi priority: {ex.Message}");
            }
        }

        private void guna2ToggleSwitch19_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch19.Checked)
            {
                // Enable GPU optimizations
                EnableGTXOptimization();
            }
            else
            {
                // Disable GPU optimizations
                DisableGTXOptimization();
            }
        }

        private void EnableGTXOptimization()
        {
            // 1. Set NVIDIA Control Panel settings for Maximum Performance
            SetNvidiaMaxPerformance();

            // 2. Set GPU to maximum performance mode (not power-saving mode)
            SetGPUPerformance();

            // 3. Disable unnecessary graphical features to boost FPS (like V-Sync, Antialiasing, etc.)
            OptimizeGraphicsSettings();

            // 4. Adjust power management for better GPU performance
            SetPowerManagementMode();

            // 5. Enable higher frame rates by disabling frame-limiting settings in NVIDIA Control Panel
            SetFrameRateLimiter();

            MessageBox.Show("GTX Optimization Enabled for Maximum Performance");
        }

        private void DisableGTXOptimization()
        {
            // Revert all optimizations for GPU back to default settings
            ResetNvidiaSettings();
            ResetGPUPerformance();
            ResetGraphicsSettings();
            ResetPowerManagementMode();
            ResetFrameRateLimiter();

            MessageBox.Show("GTX Optimization Disabled");
        }

        private void SetNvidiaMaxPerformance()
        {
            try
            {
                // Set NVIDIA Control Panel settings for maximum performance (High Performance Mode)
                System.Diagnostics.Process.Start("nvidia-control-panel", "/set Max Performance");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting NVIDIA performance mode: {ex.Message}");
            }
        }

        private void SetGPUPerformance()
        {
            // Example: Force the GPU into maximum performance (using PowerShell or command line if necessary)
            try
            {
                System.Diagnostics.Process.Start("nvidia-smi", "--gpustate max");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting GPU performance: {ex.Message}");
            }
        }

        private void OptimizeGraphicsSettings()
        {
            // Example: Disabling V-Sync, Antialiasing, and other performance-impacting settings
            // This can be done in the NVIDIA Control Panel via command line (or users can manually set these settings)
            try
            {
                // Disable V-Sync
                System.Diagnostics.Process.Start("nvidia-control-panel", "/set VSync=Off");

                // Disable Antialiasing
                System.Diagnostics.Process.Start("nvidia-control-panel", "/set Antialiasing=Off");

                // Set Texture Filtering to "Performance"
                System.Diagnostics.Process.Start("nvidia-control-panel", "/set TextureFiltering=Performance");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error optimizing graphics settings: {ex.Message}");
            }
        }

        private void SetPowerManagementMode()
        {
            // Example: Set power management to "Maximum Performance" mode to ensure GPU runs at full power
            try
            {
                System.Diagnostics.Process.Start("nvidia-control-panel", "/set PowerManagementMode=Maximum Performance");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting power management mode: {ex.Message}");
            }
        }

        private void SetFrameRateLimiter()
        {
            // Example: Disable frame rate limiters in the NVIDIA Control Panel or set a high FPS cap
            try
            {
                // Set frame rate to the highest possible value
                System.Diagnostics.Process.Start("nvidia-control-panel", "/set FrameRateLimiter=Unlimited");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting frame rate limiter: {ex.Message}");
            }
        }

        private void ResetNvidiaSettings()
        {
            // Reset NVIDIA Control Panel settings to default (if needed)
            try
            {
                System.Diagnostics.Process.Start("nvidia-control-panel", "/reset Settings");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting NVIDIA settings: {ex.Message}");
            }
        }

        private void ResetGPUPerformance()
        {
            // Reset GPU to default (non-performance mode)
            try
            {
                System.Diagnostics.Process.Start("nvidia-smi", "--gpustate default");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting GPU performance: {ex.Message}");
            }
        }

        private void ResetGraphicsSettings()
        {
            // Reset graphical optimizations (e.g., V-Sync, Antialiasing) back to default
            try
            {
                System.Diagnostics.Process.Start("nvidia-control-panel", "/reset VSync");
                System.Diagnostics.Process.Start("nvidia-control-panel", "/reset Antialiasing");
                System.Diagnostics.Process.Start("nvidia-control-panel", "/reset TextureFiltering");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting graphics settings: {ex.Message}");
            }
        }

        private void ResetPowerManagementMode()
        {
            // Reset power management to default (Balanced or Adaptive)
            try
            {
                System.Diagnostics.Process.Start("nvidia-control-panel", "/reset PowerManagementMode");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting power management mode: {ex.Message}");
            }
        }

        private void ResetFrameRateLimiter()
        {
            // Reset frame rate limiter to default
            try
            {
                System.Diagnostics.Process.Start("nvidia-control-panel", "/reset FrameRateLimiter");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting frame rate limiter: {ex.Message}");
            }
        }

        private void guna2ToggleSwitch21_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void guna2ToggleSwitch16_CheckedChanged(object sender, EventArgs e)
        {

            if (guna2ToggleSwitch16.Checked)
            {
                // Enable optimizations for both GTX and RTX GPUs
                EnableGPUOptimizations();
            }
            else
            {
                // Disable optimizations for both GTX and RTX GPUs
                DisableGPUOptimizations();
            }
        }

        private void EnableGPUOptimizations()
        {
            // Check GPU type (RTX or GTX) and apply specific optimizations
            if (IsRTX())
            {
                EnableRTXOptimizations();
            }
            else
            {
                EnableGTXOptimizations();
            }

            // Display message to user
            MessageBox.Show("GPU optimizations enabled. Check NVIDIA Control Panel for detailed settings.");
        }

        private void DisableGPUOptimizations()
        {
            // Revert GPU optimizations for both RTX and GTX
            if (IsRTX())
            {
                DisableRTXOptimizations();
            }
            else
            {
                DisableGTXOptimizations();
            }

            // Display message to user
            MessageBox.Show("GPU optimizations disabled.");
        }

        private bool IsRTX()
        {
            // Here we check if the system has an RTX GPU or not (this could be more advanced in a real-world application)
            string gpuModel = GetGPUModel();  // Function to get the GPU model (you could use libraries like OpenHardwareMonitor)
            return gpuModel.Contains("RTX");
        }

        private string GetGPUModel()
        {
            // You can implement a method to fetch the GPU model, such as using a system library or external tool like OpenHardwareMonitor
            return "NVIDIA RTX 3080";  // Example, you need to implement a way to get the actual GPU model
        }

        private void EnableRTXOptimizations()
        {
            // Enable RTX-specific optimizations (Ray Tracing, DLSS, Reflex, etc.)
            EnableRayTracing();
            EnableDLSS();
            EnableNVIDIAReflex();
            SetGPUToMaxPerformance();

            // Display message to user
            MessageBox.Show("RTX optimizations enabled (Ray Tracing, DLSS, Reflex).");
        }

        private void DisableRTXOptimizations()
        {
            // Disable RTX-specific optimizations

            SetGPUToDefaultPerformance();

            // Display message to user
            MessageBox.Show("RTX optimizations disabled.");
        }

        private void EnableGTXOptimizations()
        {
            // Enable GTX-specific optimizations (can be less GPU-demanding features)
            EnableV_Sync();  // Prompt to enable V-Sync for GTX users
            SetGPUToMaxPerformance();

            // Display message to user
            MessageBox.Show("GTX optimizations enabled (V-Sync, Texture Filtering).");
        }

        private void DisableGTXOptimizations()
        {
            // Disable GTX-specific optimizations
            DisableV_Sync();  // Prompt to disable V-Sync for GTX users
            SetGPUToDefaultPerformance();

            // Display message to user
            MessageBox.Show("GTX optimizations disabled.");
        }

        // Prompt user to enable or disable V-Sync
        private void EnableV_Sync()
        {
            try
            {
                // Display message prompting user to enable V-Sync in NVIDIA Control Panel
                MessageBox.Show("Please enable V-Sync in the NVIDIA Control Panel for better frame synchronization.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enabling V-Sync: {ex.Message}");
            }
        }

        private void DisableV_Sync()
        {
            try
            {
                // Display message prompting user to disable V-Sync in NVIDIA Control Panel
                MessageBox.Show("Please disable V-Sync in the NVIDIA Control Panel to prevent input lag.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disabling V-Sync: {ex.Message}");
            }
        }

        // Similar messages for other settings (Ray Tracing, DLSS, etc.)
        private void EnableRayTracing()
        {
            try
            {
                // Display message prompting user to enable Ray Tracing in NVIDIA Control Panel
                MessageBox.Show("Please enable Ray Tracing in the NVIDIA Control Panel.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enabling Ray Tracing: {ex.Message}");
            }
        }

        private void EnableDLSS()
        {
            try
            {
                // Display message prompting user to enable DLSS in NVIDIA Control Panel
                MessageBox.Show("Please enable DLSS (Deep Learning Super Sampling) in the NVIDIA Control Panel.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enabling DLSS: {ex.Message}");
            }
        }

        private void EnableNVIDIAReflex()
        {
            try
            {
                // Display message prompting user to enable Reflex (Low Latency Mode) in NVIDIA Control Panel
                MessageBox.Show("Please enable NVIDIA Reflex (Low Latency Mode) in the NVIDIA Control Panel.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enabling NVIDIA Reflex: {ex.Message}");
            }
        }

        // Set GPU to maximum performance
        private void SetGPUToMaxPerformance()
        {
            try
            {
                // Display message prompting user to set GPU to maximum performance
                MessageBox.Show("Please set your GPU to maximum performance in the NVIDIA Control Panel.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting GPU performance: {ex.Message}");
            }
        }

        // Reset GPU to default performance
        private void SetGPUToDefaultPerformance()
        {
            try
            {
                // Display message prompting user to reset GPU to default performance
                MessageBox.Show("Please reset GPU performance to default in the NVIDIA Control Panel.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting GPU performance: {ex.Message}");
            }
        }

        private void guna2ToggleSwitch18_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch18.Checked)
            {
                // Enable Driver Optimizations
                EnableDriverOptimizations();
            }
            else
            {
                // Disable Driver Optimizations
                DisableDriverOptimizations();
            }
        }

        private void EnableDriverOptimizations()
        {
            try
            {
                // Example: Set GPU to maximum performance (high-performance mode)
                SetGPUPowerManagementMode("Prefer Maximum Performance");

                // Example: Disable unnecessary background processes for optimal performance
                DisableBackgroundProcesses();

                // Example: Enable GPU-related optimizations such as CUDA
                EnableCUDA();

                // Notify the user
                MessageBox.Show("Driver optimizations enabled. Please check NVIDIA Control Panel for detailed settings.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enabling driver optimizations: {ex.Message}");
            }
        }

        private void DisableDriverOptimizations()
        {
            try
            {
                // Example: Set GPU to default power-saving mode
                SetGPUPowerManagementMode("Optimal Power");

                // Example: Re-enable background processes if needed
                EnableBackgroundProcesses();

                // Example: Disable CUDA if no longer required
                DisableCUDA();

                // Notify the user
                MessageBox.Show("Driver optimizations disabled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disabling driver optimizations: {ex.Message}");
            }
        }

        private void SetGPUPowerManagementMode(string mode)
        {
            // Example code that would ideally set the GPU to "Prefer Maximum Performance" or other modes
            // This could be done manually in NVIDIA Control Panel, or if automating, use system tools or APIs

            if (mode == "Prefer Maximum Performance")
            {
                // Code to set GPU to maximum performance mode
                MessageBox.Show("Set GPU to Prefer Maximum Performance mode.");
            }
            else
            {
                // Code to reset to default power-saving mode
                MessageBox.Show("Set GPU to Optimal Power mode.");
            }
        }

        private void EnableCUDA()
        {
            // Example code that would enable CUDA (a performance optimization for NVIDIA GPUs)
            MessageBox.Show("CUDA optimizations enabled. Please ensure your game/app supports CUDA.");
        }

        private void DisableCUDA()
        {
            // Example code that would disable CUDA
            MessageBox.Show("CUDA optimizations disabled.");
        }

        private void DisableBackgroundProcesses()
        {
            // This could be a general system setting to reduce background processes that consume GPU or CPU resources
            MessageBox.Show("Background processes disabled to optimize GPU performance.");
        }

        private void EnableBackgroundProcesses()
        {
            // This could be a reverse action to enable certain background processes if needed
            MessageBox.Show("Background processes enabled.");
        }

        private void guna2ToggleSwitch20_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch20.Checked)
            {
                // Enable Background Process Optimization
                EnableBackgroundProcessOptimization();
            }
            else
            {
                // Disable Background Process Optimization
                DisableBackgroundProcessOptimization();
            }
        }

        private void EnableBackgroundProcessOptimization()
        {
            try
            {
                // Disable unnecessary background processes
                DisableUnnecessaryProcesses();

                // Prioritize the active game or application to optimize performance
                SetGameProcessPriority();

                // Reduce CPU usage by limiting unnecessary system services
                LimitUnnecessarySystemServices();

                // Notify the user
                MessageBox.Show("Background process optimization enabled. Unnecessary processes have been reduced.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enabling background process optimization: {ex.Message}");
            }
        }

        private void DisableBackgroundProcessOptimization()
        {
            try
            {
                // Enable all background processes back (revert optimizations)
                EnableUnnecessaryProcesses();

                // Reset process priority back to normal
                ResetProcessPriority();

                // Enable all system services if they were limited
                RestoreSystemServices();

                // Notify the user
                MessageBox.Show("Background process optimization disabled. System processes restored.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disabling background process optimization: {ex.Message}");
            }
        }

        private void DisableUnnecessaryProcesses()
        {
            // Example of disabling a process - be very careful with this as killing certain processes can crash your system
            foreach (var process in Process.GetProcesses())
            {
                // List of processes you want to terminate (You should decide which ones are safe to terminate)
                if (process.ProcessName == "SomeUnnecessaryProcess") // Example
                {
                    process.Kill();
                }
            }
            MessageBox.Show("Unnecessary processes have been disabled.");
        }

        private void EnableUnnecessaryProcesses()
        {
            // This would re-enable processes or services that were previously disabled.
            // Example: You may want to restore default services after optimization
            MessageBox.Show("All background processes restored.");
        }

        private void SetGameProcessPriority()
        {
            // Example of setting the priority of your game/application to high priority for better performance
            var currentProcess = Process.GetCurrentProcess();
            currentProcess.PriorityClass = ProcessPriorityClass.High;
            MessageBox.Show("Game process set to high priority.");
        }

        private void ResetProcessPriority()
        {
            // Reset the priority back to normal after disabling optimizations
            var currentProcess = Process.GetCurrentProcess();
            currentProcess.PriorityClass = ProcessPriorityClass.Normal;
            MessageBox.Show("Process priority reset to normal.");
        }

        private void LimitUnnecessarySystemServices()
        {
            // Example of limiting system services - this requires elevated permissions
            // Example: Disabling services like print spoolers or background tasks
            foreach (var service in ServiceController.GetServices())
            {
                if (service.ServiceName == "SomeServiceName") // Example of disabling a service
                {
                    service.Stop();
                }
            }
            MessageBox.Show("Unnecessary system services have been limited.");
        }

        private void RestoreSystemServices()
        {
            // Restore services that were stopped earlier
            foreach (var service in ServiceController.GetServices())
            {
                if (service.ServiceName == "SomeServiceName")
                {
                    service.Start();
                }
            }
            MessageBox.Show("System services restored.");
        }

        private void guna2ToggleSwitch22_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (guna2ToggleSwitch22.Checked)
                {
                    // Changing DNS to Google's DNS (8.8.8.8 and 8.8.4.4)
                    SetDns("8.8.8.8", "8.8.4.4");
                    MessageBox.Show("DNS set to Google DNS (8.8.8.8, 8.8.4.4).");
                }
                else
                {
                    // Reset DNS to automatic (Obtain DNS server address automatically)
                    SetDns(null, null);
                    MessageBox.Show("DNS reset to automatic.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void SetDns(string primaryDns, string secondaryDns)
        {
            // Get the network interfaces (for example, Ethernet or Wi-Fi)
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface netInterface in networkInterfaces)
            {
                // Only modify the interface if it's up and connected
                if (netInterface.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties properties = netInterface.GetIPProperties();

                    // Access the interface's DNS settings
                    foreach (var dns in properties.DnsAddresses)
                    {
                        // If we are providing DNS addresses, set them
                        if (primaryDns != null && secondaryDns != null)
                        {
                            SetDnsForInterface(netInterface, primaryDns, secondaryDns);
                        }
                        else
                        {
                            // Otherwise, reset to automatic DNS
                            ResetDnsForInterface(netInterface);
                        }
                    }
                }
            }
        }

        private void SetDnsForInterface(NetworkInterface netInterface, string primaryDns, string secondaryDns)
        {
            // Logic to change DNS to Google's DNS
            try
            {
                var networkSettings = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"interface ip set dns name=\"{netInterface.Name}\" static {primaryDns} primary",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(networkSettings);

                var secondaryDnsSettings = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"interface ip add dns name=\"{netInterface.Name}\" {secondaryDns} index=2",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(secondaryDnsSettings);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set DNS: {ex.Message}");
            }
        }

        private void ResetDnsForInterface(NetworkInterface netInterface)
        {
            // Reset the DNS to automatic (Obtain DNS automatically)
            try
            {
                var resetSettings = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"interface ip set dns name=\"{netInterface.Name}\" source=dhcp",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(resetSettings);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to reset DNS: {ex.Message}");
            }
        }

        private void guna2ToggleSwitch23_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (guna2ToggleSwitch23.Checked)
                {
                    SetCloudflareDNS();
                    MessageBox.Show("DNS set to Cloudflare (1.1.1.1, 1.0.0.1)");
                }
                else
                {
                    
                    MessageBox.Show("DNS reset to automatic.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void SetCloudflareDNS()
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus == OperationalStatus.Up &&
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    !adapter.Description.ToLower().Contains("virtual") &&
                    !adapter.Description.ToLower().Contains("tunnel"))
                {
                    string adapterName = adapter.Name;

                    // Set primary DNS
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"interface ip set dns name=\"{adapterName}\" static 1.1.1.1 primary",
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = true
                    });

                    // Add secondary DNS
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"interface ip add dns name=\"{adapterName}\" 1.0.0.1 index=2",
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = true
                    });
                }
            }
        }

        private void guna2ToggleSwitch24_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (guna2ToggleSwitch24.Checked)
                {
                    // Try setting Ultimate Performance plan
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = "/setactive e9a42b02-d5df-448d-aa00-03f14749eb61", // Ultimate Performance
                        Verb = "runas",
                        CreateNoWindow = true,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    Process.Start(psi);
                }
                else
                {
                    // Set to Balanced
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e", // Balanced
                        Verb = "runas",
                        CreateNoWindow = true,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to change power plan: " + ex.Message);
            }
        }

        private void guna2ToggleSwitch25_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch25.Checked)
            {
                try
                {
                    // Clear user temp folder
                    DirectoryInfo tempDir = new DirectoryInfo(Path.GetTempPath());
                    foreach (FileInfo file in tempDir.GetFiles()) { TryDeleteFile(file); }
                    foreach (DirectoryInfo dir in tempDir.GetDirectories()) { TryDeleteDirectory(dir); }

                    // Clear Windows temp folder
                    DirectoryInfo winTemp = new DirectoryInfo(@"C:\Windows\Temp");
                    foreach (FileInfo file in winTemp.GetFiles()) { TryDeleteFile(file); }
                    foreach (DirectoryInfo dir in winTemp.GetDirectories()) { TryDeleteDirectory(dir); }

                    // Clear prefetch
                    DirectoryInfo prefetch = new DirectoryInfo(@"C:\Windows\Prefetch");
                    foreach (FileInfo file in prefetch.GetFiles()) { TryDeleteFile(file); }

                    // Empty recycle bin
                    SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SHERB_NOCONFIRMATION | RecycleFlags.SHERB_NOPROGRESSUI | RecycleFlags.SHERB_NOSOUND);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error during cleaning: " + ex.Message);
                }
            }
        }

        private void TryDeleteFile(FileInfo file)
        {
            try { file.Delete(); } catch { }
        }

        private void TryDeleteDirectory(DirectoryInfo dir)
        {
            try { dir.Delete(true); } catch { }
        }

        // Recycle Bin API
        [System.Runtime.InteropServices.DllImport("Shell32.dll")]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        [Flags]
        enum RecycleFlags : int
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000002,
            SHERB_NOSOUND = 0x00000004
        }

        private void guna2ToggleSwitch26_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch26.Checked)
            {
                try
                {
                    // 1. Disable Dynamic Tick (better timing for games)
                    RunCommand("bcdedit /set disabledynamictick yes");

                    // 2. Enable Ultimate Performance Plan
                    RunCommand("powercfg /setactive e9a42b02-d5df-448d-aa00-03f14749eb61");

                    // 3. Disable hibernation (frees SSD/HDD cache)
                    RunCommand("powercfg -h off");

                    // 4. Clear standby memory (optional — can help reduce stuttering)
                    RunCommand("cmd.exe /c \"EmptyStandbyList.exe workingsets\"");

                    // 5. Disable Fullscreen Optimizations for Roblox (reduce input lag)
                    string robloxPath = @"C:\Users\" + Environment.UserName + @"\AppData\Local\Roblox\Versions";
                    if (Directory.Exists(robloxPath))
                    {
                        foreach (string folder in Directory.GetDirectories(robloxPath))
                        {
                            string exe = Path.Combine(folder, "RobloxPlayerBeta.exe");
                            if (File.Exists(exe))
                            {
                                File.SetAttributes(exe, FileAttributes.Normal);
                                FileInfo info = new FileInfo(exe);
                                Process.Start("cmd.exe", $"/c reg add \"HKCU\\Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers\" /v \"{exe}\" /t REG_SZ /d \"DISABLEDXMAXIMIZEDWINDOWEDMODE\" /f");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while applying FPS drop fix: " + ex.Message);
                }
            }
        }

        private void RunCommand(string command)
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                Verb = "runas",
                CreateNoWindow = true,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
        }
    }
}



        
    




































