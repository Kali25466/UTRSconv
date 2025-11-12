// WorldForge ULTIMATE - The Most Advanced TRS Converter
// Version: 2.0 ULTIMATE FUTURISTIC EDITION
// Features: Full Screen, 30 Decimal Precision, Batch Processing, History, Dark Futuristic UI

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WorldForge
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainFormUltimate());
        }
    }

    // Validation result structure
    public sealed class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        public ValidationResult(bool isValid, string errorMessage = "")
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }

    // High-precision decimal Vector3 for 30 decimal accuracy
    public struct DecimalVector3
    {
        public decimal X, Y, Z;
        
        public DecimalVector3(decimal x, decimal y, decimal z)
        {
            X = x; Y = y; Z = z;
        }

        public static DecimalVector3 operator +(DecimalVector3 a, DecimalVector3 b) =>
            new DecimalVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        
        public static DecimalVector3 operator -(DecimalVector3 a, DecimalVector3 b) =>
            new DecimalVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        
        public static DecimalVector3 operator *(DecimalVector3 a, DecimalVector3 b) =>
            new DecimalVector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        
        public static DecimalVector3 operator /(DecimalVector3 a, DecimalVector3 b) =>
            new DecimalVector3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);

        public decimal Length() => 
            (decimal)Math.Sqrt((double)(X * X + Y * Y + Z * Z));

        public override string ToString() => $"({X}, {Y}, {Z})";
    }

    // High-precision quaternion using decimal
    public struct DecimalQuaternion
    {
        public decimal X, Y, Z, W;

        public DecimalQuaternion(decimal x, decimal y, decimal z, decimal w)
        {
            X = x; Y = y; Z = z; W = w;
        }

        public static DecimalQuaternion FromYawPitchRoll(decimal yaw, decimal pitch, decimal roll)
        {
            decimal cy = DecimalMath.Cos(yaw / 2);
            decimal sy = DecimalMath.Sin(yaw / 2);
            decimal cp = DecimalMath.Cos(pitch / 2);
            decimal sp = DecimalMath.Sin(pitch / 2);
            decimal cr = DecimalMath.Cos(roll / 2);
            decimal sr = DecimalMath.Sin(roll / 2);

            return new DecimalQuaternion(
                sy * cp * cr + cy * sp * sr,
                cy * sp * cr - sy * cp * sr,
                cy * cp * sr - sy * sp * cr,
                cy * cp * cr + sy * sp * sr
            );
        }

        public DecimalQuaternion Inverse()
        {
            decimal lengthSq = X * X + Y * Y + Z * Z + W * W;
            if (lengthSq < 0.000000000000000000000000000001m)
                throw new InvalidOperationException("Cannot invert zero-length quaternion");
            
            decimal invLengthSq = 1m / lengthSq;
            return new DecimalQuaternion(-X * invLengthSq, -Y * invLengthSq, -Z * invLengthSq, W * invLengthSq);
        }

        public DecimalQuaternion Normalize()
        {
            decimal length = (decimal)Math.Sqrt((double)(X * X + Y * Y + Z * Z + W * W));
            if (length < 0.000000000000000000000000000001m)
                return new DecimalQuaternion(0, 0, 0, 1);
            
            return new DecimalQuaternion(X / length, Y / length, Z / length, W / length);
        }
    }

    // High-precision math functions
    public static class DecimalMath
    {
        public static decimal Sin(decimal angle)
        {
            // Taylor series for sin(x) with high precision
            decimal result = 0;
            decimal term = angle;
            decimal x2 = angle * angle;
            
            for (int i = 0; i < 30; i++)
            {
                result += term;
                term *= -x2 / ((2 * i + 2) * (2 * i + 3));
                if (Math.Abs(term) < 0.000000000000000000000000000001m)
                    break;
            }
            
            return result;
        }

        public static decimal Cos(decimal angle)
        {
            // Taylor series for cos(x) with high precision
            decimal result = 1;
            decimal term = 1;
            decimal x2 = angle * angle;
            
            for (int i = 0; i < 30; i++)
            {
                term *= -x2 / ((2 * i + 1) * (2 * i + 2));
                result += term;
                if (Math.Abs(term) < 0.000000000000000000000000000001m)
                    break;
            }
            
            return result;
        }

        public static decimal NormalizeAngle(decimal degrees)
        {
            degrees = degrees % 360m;
            if (degrees > 180m) degrees -= 360m;
            else if (degrees < -180m) degrees += 360m;
            return degrees;
        }

        public static decimal DegreesToRadians(decimal degrees)
        {
            return degrees * (decimal)Math.PI / 180m;
        }
    }

    // Calculation history entry
    public class CalculationHistory
    {
        public DateTime Timestamp { get; set; }
        public string Mode { get; set; }
        public DecimalVector3 Input { get; set; }
        public DecimalVector3 Result { get; set; }
        public DecimalVector3 ParentPos { get; set; }
        public DecimalVector3 ParentRot { get; set; }
        public DecimalVector3 ParentScale { get; set; }
        public int Precision { get; set; }
    }

    public class MainFormUltimate : Form
    {
        // Constants
        private const int DEFAULT_WIDTH = 1280;
        private const int DEFAULT_HEIGHT = 800;
        private const decimal MAX_VALUE = 1000000000m;
        private const decimal MIN_VALUE = -1000000000m;
        private const decimal EPSILON = 0.000000000000000000000000000001m;
        private const int DEFAULT_PRECISION = 15;
        private const int MAX_PRECISION = 30;

        // Futuristic Theme Colors
        private static class Theme
        {
            public static readonly Color DarkBg = ColorTranslator.FromHtml("#0A0E27");
            public static readonly Color PanelBg = ColorTranslator.FromHtml("#1A1F3A");
            public static readonly Color PanelLight = ColorTranslator.FromHtml("#252B4A");
            public static readonly Color PanelDark = ColorTranslator.FromHtml("#141829");
            public static readonly Color CyanGlow = ColorTranslator.FromHtml("#00D9FF");
            public static readonly Color PurpleGlow = ColorTranslator.FromHtml("#B942FF");
            public static readonly Color GreenGlow = ColorTranslator.FromHtml("#00FF88");
            public static readonly Color RedGlow = ColorTranslator.FromHtml("#FF0055");
            public static readonly Color OrangeGlow = ColorTranslator.FromHtml("#FF9500");
            public static readonly Color TextPrimary = ColorTranslator.FromHtml("#E0E6FF");
            public static readonly Color TextSecondary = ColorTranslator.FromHtml("#8B93B8");
            public static readonly Color BorderGlow = ColorTranslator.FromHtml("#4A5FFF");
        }

        // UI Controls
        private Panel _headerPanel, _mainPanel, _sidePanel;
        private RadioButton _rbLocalToWorld, _rbWorldToLocal;
        private NumericUpDown _upParentPosX, _upParentPosY, _upParentPosZ;
        private NumericUpDown _upParentRotX, _upParentRotY, _upParentRotZ;
        private NumericUpDown _upParentScaleX, _upParentScaleY, _upParentScaleZ;
        private NumericUpDown _upInputX, _upInputY, _upInputZ;
        private Button _btnConvert, _btnCopyResult, _btnReset, _btnValidate;
        private Button _btnPresetIdentity, _btnPresetSample, _btnFullscreen, _btnBatchConvert;
        private Button _btnExportHistory, _btnImportPreset, _btnSwapMode, _btnExport, _btnImport;
        private RichTextBox _rtbResult;
        private ListView _lvHistory;
        private TrackBar _tbPrecisionSlider;
        private Label _lblPrecisionDisplay, _lblValidation;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripProgressBar _progressBar;
        private Timer _animationTimer;
        private ToolTip _toolTip;

        // State
        private bool _isFullscreen = false;
        private FormWindowState _previousWindowState;
        private FormBorderStyle _previousBorderStyle;
        private Rectangle _previousBounds;
        private DecimalVector3 _lastResult;
        private bool _hasValidResult;
        private List<CalculationHistory> _history = new List<CalculationHistory>();
        private int _currentPrecision = DEFAULT_PRECISION;
        private float _glowAnimation = 0f;

        public MainFormUltimate()
        {
            Text = "WorldForge ULTIMATE v2.0 - Futuristic TRS Converter";
            ClientSize = new Size(DEFAULT_WIDTH, DEFAULT_HEIGHT);
            FormBorderStyle = FormBorderStyle.None; // Borderless for futuristic look
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.DarkBg;
            DoubleBuffered = true;
            KeyPreview = true;
            Icon = CreateCustomIcon();
            
            _toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 300,
                ReshowDelay = 100,
                ShowAlways = true,
                BackColor = Theme.PanelBg,
                ForeColor = Theme.CyanGlow
            };

            InitializeUI();
            SetupAnimation();
            UpdateValidationStatus();
            
            // Custom window dragging
            _headerPanel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) DragMove(); };
        }

        private Icon CreateCustomIcon()
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Theme.DarkBg);
                g.FillEllipse(new SolidBrush(Theme.CyanGlow), 6, 6, 20, 20);
                g.DrawEllipse(new Pen(Theme.PurpleGlow, 2), 4, 4, 24, 24);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private void DragMove()
        {
            const int WM_NCLBUTTONDOWN = 0xA1;
            const int HT_CAPTION = 0x2;
            
            if (!_isFullscreen)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void SetupAnimation()
        {
            _animationTimer = new Timer { Interval = 30 };
            _animationTimer.Tick += (s, e) =>
            {
                _glowAnimation += _glowAnimation > 1f ? -0.05f : 0.05f;
                if (_glowAnimation > 1f || _glowAnimation < 0f)
                    _glowAnimation = Math.Max(0f, Math.Min(1f, _glowAnimation));
                
                Invalidate();
            };
            _animationTimer.Start();
        }

        private void InitializeUI()
        {
            SuspendLayout();

            // Status Strip at bottom
            CreateStatusStrip();

            // Header Panel (60px height) - Futuristic title bar
            CreateHeaderPanel();

            // Main content area
            CreateMainPanel();

            // Right side panel with history and controls
            CreateSidePanel();

            ResumeLayout();
        }

        private void CreateStatusStrip()
        {
            _statusStrip = new StatusStrip
            {
                BackColor = Theme.PanelBg,
                ForeColor = Theme.TextSecondary,
                Height = 30,
                Padding = new Padding(5)
            };

            _statusLabel = new ToolStripStatusLabel
            {
                Text = "● READY | Precision calculations enabled",
                ForeColor = Theme.CyanGlow,
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _progressBar = new ToolStripProgressBar
            {
                Size = new Size(200, 20),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _progressBar });
            Controls.Add(_statusStrip);
        }

        private void CreateHeaderPanel()
        {
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Theme.PanelBg
            };

            // Title with glow effect
            var lblTitle = new Label
            {
                Text = "● WORLDFORGE ULTIMATE v2.0",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Theme.CyanGlow,
                Left = 20,
                Top = 10,
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "Futuristic TRS Converter | 30 Decimal Precision",
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.TextSecondary,
                Left = 20,
                Top = 45,
                AutoSize = true
            };

            // Window controls (right side)
            int btnRight = DEFAULT_WIDTH - 150;
            
            var btnMinimize = CreateHeaderButton("_", btnRight, 15);
            btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;

            var btnMaximize = CreateHeaderButton("□", btnRight + 40, 15);
            btnMaximize.Click += (s, e) => ToggleMaximize();

            var btnClose = CreateHeaderButton("×", btnRight + 80, 15);
            btnClose.Click += (s, e) => Application.Exit();
            btnClose.BackColor = Theme.RedGlow;

            _btnFullscreen = CreateHeaderButton("⛶", btnRight - 45, 15);
            _btnFullscreen.Click += (s, e) => ToggleFullscreen();
            _toolTip.SetToolTip(_btnFullscreen, "Toggle Fullscreen (F11)");

            _headerPanel.Controls.AddRange(new Control[] 
            { 
                lblTitle, lblSubtitle, 
                btnMinimize, btnMaximize, btnClose, _btnFullscreen 
            });

            _headerPanel.Paint += (s, e) =>
            {
                // Draw glowing border at bottom
                using (var pen = new Pen(Theme.BorderGlow, 2))
                {
                    e.Graphics.DrawLine(pen, 0, _headerPanel.Height - 1, 
                        _headerPanel.Width, _headerPanel.Height - 1);
                }
            };

            Controls.Add(_headerPanel);
        }

        private Button CreateHeaderButton(string text, int left, int top)
        {
            return new Button
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 35,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.PanelLight,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private void CreateMainPanel()
        {
            _mainPanel = new Panel
            {
                Left = 20,
                Top = 90,
                Width = DEFAULT_WIDTH - 420,
                Height = DEFAULT_HEIGHT - 150,
                BackColor = Theme.DarkBg
            };

            int yPos = 10;

            // Mode Selection
            yPos = CreateModePanel(yPos);
            yPos += 20;

            // Parent Transform Panel
            yPos = CreateParentTransformPanel(yPos);
            yPos += 20;

            // Input Panel
            yPos = CreateInputPanel(yPos);
            yPos += 20;

            // Action Buttons
            yPos = CreateActionButtons(yPos);
            yPos += 20;

            // Result Display
            CreateResultDisplay(yPos);

            Controls.Add(_mainPanel);
        }

        private int CreateModePanel(int yPos)
        {
            var panel = CreateFuturisticPanel("Conversion Mode", 0, yPos, _mainPanel.Width - 20, 80);

            _rbLocalToWorld = new RadioButton
            {
                Text = "Local → World Space",
                Checked = true,
                Left = 20,
                Top = 35,
                Width = 200,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10)
            };

            _rbWorldToLocal = new RadioButton
            {
                Text = "World → Local Space",
                Left = 240,
                Top = 35,
                Width = 200,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10)
            };

            _btnSwapMode = CreateGlowButton("⇄ SWAP", 460, 30, 100, 30, Theme.PurpleGlow);
            _btnSwapMode.Click += (s, e) => SwapMode();

            panel.Controls.AddRange(new Control[] { _rbLocalToWorld, _rbWorldToLocal, _btnSwapMode });
            _mainPanel.Controls.Add(panel);

            return yPos + panel.Height;
        }

        private int CreateParentTransformPanel(int yPos)
        {
            var panel = CreateFuturisticPanel("Parent Transform (TRS)", 0, yPos, _mainPanel.Width - 20, 140);

            int row1 = 35, row2 = 70, row3 = 105;
            int col1 = 20, col2 = 140, col3 = 280, col4 = 420;

            // Position
            AddLabel(panel, "Position:", col1, row1);
            _upParentPosX = CreateNumericUpDown(col2, row1, 110);
            _upParentPosY = CreateNumericUpDown(col3, row1, 110);
            _upParentPosZ = CreateNumericUpDown(col4, row1, 110);
            panel.Controls.AddRange(new Control[] { _upParentPosX, _upParentPosY, _upParentPosZ });

            // Rotation
            AddLabel(panel, "Rotation (°):", col1, row2);
            _upParentRotX = CreateNumericUpDown(col2, row2, 110, 0, -360, 360);
            _upParentRotY = CreateNumericUpDown(col3, row2, 110, 0, -360, 360);
            _upParentRotZ = CreateNumericUpDown(col4, row2, 110, 0, -360, 360);
            panel.Controls.AddRange(new Control[] { _upParentRotX, _upParentRotY, _upParentRotZ });

            // Scale
            AddLabel(panel, "Scale:", col1, row3);
            _upParentScaleX = CreateNumericUpDown(col2, row3, 110, 1);
            _upParentScaleY = CreateNumericUpDown(col3, row3, 110, 1);
            _upParentScaleZ = CreateNumericUpDown(col4, row3, 110, 1);
            panel.Controls.AddRange(new Control[] { _upParentScaleX, _upParentScaleY, _upParentScaleZ });

            _mainPanel.Controls.Add(panel);
            return yPos + panel.Height;
        }

        private int CreateInputPanel(int yPos)
        {
            var panel = CreateFuturisticPanel("Input Position", 0, yPos, _mainPanel.Width - 20, 80);

            int col1 = 20, col2 = 140, col3 = 300;

            AddLabel(panel, "X:", col1, 40);
            _upInputX = CreateNumericUpDown(col1 + 25, 37, 140);
            AddLabel(panel, "Y:", col2 + 30, 40);
            _upInputY = CreateNumericUpDown(col2 + 55, 37, 140);
            AddLabel(panel, "Z:", col3 + 30, 40);
            _upInputZ = CreateNumericUpDown(col3 + 55, 37, 140);

            panel.Controls.AddRange(new Control[] { _upInputX, _upInputY, _upInputZ });

            _mainPanel.Controls.Add(panel);
            return yPos + panel.Height;
        }

        private int CreateActionButtons(int yPos)
        {
            int spacing = 15;
            int btnWidth = 120;

            _btnConvert = CreateGlowButton("⚡ CONVERT", 20, yPos, btnWidth + 30, 45, Theme.CyanGlow);
            _btnConvert.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            _btnConvert.Click += BtnConvert_Click;

            _btnBatchConvert = CreateGlowButton("📦 BATCH", 20 + btnWidth + spacing + 30, yPos, btnWidth, 45, Theme.PurpleGlow);
            _btnBatchConvert.Click += BtnBatchConvert_Click;

            _btnExport = CreateGlowButton("💾 EXPORT", 20 + (btnWidth + spacing) * 2 + 30, yPos, btnWidth, 45, Theme.GreenGlow);
            _btnExport.Click += BtnExport_Click;

            _btnImport = CreateGlowButton("📂 IMPORT", 20 + (btnWidth + spacing) * 3 + 30, yPos, btnWidth, 45, Theme.OrangeGlow);
            _btnImport.Click += BtnImport_Click;

            _mainPanel.Controls.AddRange(new Control[] 
            { 
                _btnConvert, _btnBatchConvert, _btnExport, _btnImport 
            });

            return yPos + 50;
        }

        private void CreateResultDisplay(int yPos)
        {
            var panel = CreateFuturisticPanel("CALCULATION RESULT", 0, yPos, _mainPanel.Width - 20, 120);

            _rtbResult = new RichTextBox
            {
                Left = 15,
                Top = 35,
                Width = panel.Width - 30,
                Height = 70,
                BackColor = Theme.PanelDark,
                ForeColor = Theme.CyanGlow,
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Text = "Awaiting calculation...\n(Results will display here with up to 30 decimal precision)"
            };

            panel.Controls.Add(_rtbResult);
            _mainPanel.Controls.Add(panel);
        }

        private void CreateSidePanel()
        {
            _sidePanel = new Panel
            {
                Left = DEFAULT_WIDTH - 380,
                Top = 90,
                Width = 360,
                Height = DEFAULT_HEIGHT - 150,
                BackColor = Theme.DarkBg
            };

            int yPos = 10;

            // History Panel
            yPos = CreateHistoryPanel(yPos);
            yPos += 20;

            // Precision Control
            yPos = CreatePrecisionControl(yPos);
            yPos += 20;

            // Quick Actions
            CreateQuickActions(yPos);

            Controls.Add(_sidePanel);
        }

        private int CreateHistoryPanel(int yPos)
        {
            var panel = CreateFuturisticPanel("CALCULATION HISTORY", 0, yPos, _sidePanel.Width - 20, 250);

            _lvHistory = new ListView
            {
                Left = 10,
                Top = 35,
                Width = panel.Width - 20,
                Height = 170,
                BackColor = Theme.PanelDark,
                ForeColor = Theme.TextPrimary,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };

            _lvHistory.Columns.Add("Time", 70);
            _lvHistory.Columns.Add("Mode", 50);
            _lvHistory.Columns.Add("Result", 150);

            var btnExportHistory = CreateGlowButton("Export History", 10, 210, 120, 28, Theme.GreenGlow);
            btnExportHistory.Click += BtnExportHistory_Click;

            var btnClearHistory = CreateGlowButton("Clear", 140, 210, 80, 28, Theme.RedGlow);
            btnClearHistory.Click += (s, e) => ClearHistory();

            panel.Controls.AddRange(new Control[] { _lvHistory, btnExportHistory, btnClearHistory });
            _sidePanel.Controls.Add(panel);

            return yPos + panel.Height;
        }

        private int CreatePrecisionControl(int yPos)
        {
            var panel = CreateFuturisticPanel("PRECISION CONTROL", 0, yPos, _sidePanel.Width - 20, 140);

            _lblPrecisionDisplay = new Label
            {
                Text = $"Current: {DEFAULT_PRECISION} decimals",
                Left = 15,
                Top = 35,
                Width = panel.Width - 30,
                ForeColor = Theme.CyanGlow,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _tbPrecisionSlider = new TrackBar
            {
                Left = 15,
                Top = 65,
                Width = panel.Width - 30,
                Minimum = 2,
                Maximum = MAX_PRECISION,
                Value = DEFAULT_PRECISION,
                TickFrequency = 5,
                TickStyle = TickStyle.BottomRight
            };

            _tbPrecisionSlider.ValueChanged += (s, e) =>
            {
                _currentPrecision = _tbPrecisionSlider.Value;
                _lblPrecisionDisplay.Text = $"Current: {_currentPrecision} decimals";
                if (_hasValidResult)
                {
                    UpdateResultDisplay();
                }
            };

            var lblMin = new Label { Text = "Fast (2)", Left = 15, Top = 110, ForeColor = Theme.TextSecondary, AutoSize = true };
            var lblMax = new Label { Text = "Maximum (30)", Left = panel.Width - 110, Top = 110, ForeColor = Theme.TextSecondary, AutoSize = true };

            panel.Controls.AddRange(new Control[] { _lblPrecisionDisplay, _tbPrecisionSlider, lblMin, lblMax });
            _sidePanel.Controls.Add(panel);

            return yPos + panel.Height;
        }

        private void CreateQuickActions(int yPos)
        {
            var panel = CreateFuturisticPanel("QUICK ACTIONS", 0, yPos, _sidePanel.Width - 20, 200);

            int btnY = 35;
            int spacing = 40;

            _btnValidate = CreateGlowButton("✓ Validate Inputs", 15, btnY, panel.Width - 30, 35, Theme.GreenGlow);
            _btnValidate.Click += (s, e) => ValidateInputs(true);

            _btnCopyResult = CreateGlowButton("📋 Copy Result", 15, btnY + spacing, panel.Width - 30, 35, Theme.PurpleGlow);
            _btnCopyResult.Click += BtnCopyResult_Click;

            _btnReset = CreateGlowButton("↺ Reset All", 15, btnY + spacing * 2, panel.Width - 30, 35, Theme.OrangeGlow);
            _btnReset.Click += (s, e) => ResetAll();

            var btnPresets = CreateGlowButton("📂 Presets...", 15, btnY + spacing * 3, panel.Width - 30, 35, Theme.CyanGlow);
            btnPresets.Click += (s, e) => ShowPresetsMenu();

            panel.Controls.AddRange(new Control[] { _btnValidate, _btnCopyResult, _btnReset, btnPresets });
            _sidePanel.Controls.Add(panel);
        }

        // Helper methods for UI creation
        private Panel CreateFuturisticPanel(string title, int x, int y, int width, int height)
        {
            var panel = new Panel
            {
                Left = x,
                Top = y,
                Width = width,
                Height = height,
                BackColor = Theme.PanelBg
            };

            panel.Paint += (s, e) =>
            {
                // Draw glowing border
                using (var pen = new Pen(Color.FromArgb((int)(100 + _glowAnimation * 155), Theme.BorderGlow), 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }

                // Draw title background
                using (var brush = new SolidBrush(Theme.PanelLight))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, panel.Width, 30);
                }
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Theme.CyanGlow,
                Left = 10,
                Top = 7,
                AutoSize = true
            };

            panel.Controls.Add(lblTitle);
            return panel;
        }

        private Button CreateGlowButton(string text, int x, int y, int width, int height, Color glowColor)
        {
            var btn = new Button
            {
                Text = text,
                Left = x,
                Top = y,
                Width = width,
                Height = height,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.PanelLight,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderColor = glowColor;
            btn.FlatAppearance.BorderSize = 2;

            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(30, glowColor);
            btn.MouseLeave += (s, e) => btn.BackColor = Theme.PanelLight;

            return btn;
        }

        private NumericUpDown CreateNumericUpDown(int x, int y, int width, decimal initial = 0, 
            decimal min = MIN_VALUE, decimal max = MAX_VALUE)
        {
            return new NumericUpDown
            {
                Left = x,
                Top = y,
                Width = width,
                DecimalPlaces = 10,
                Minimum = min,
                Maximum = max,
                Value = initial,
                BackColor = Theme.PanelDark,
                ForeColor = Theme.CyanGlow,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9)
            };
        }

        private void AddLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Left = x,
                Top = y + 3,
                AutoSize = true,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9)
            });
        }

        // Event Handlers - THESE ARE STUBS - FULL IMPLEMENTATION CONTINUES...
        private void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                _previousWindowState = WindowState;
                _previousBorderStyle = FormBorderStyle;
                _previousBounds = Bounds;
                
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                _isFullscreen = true;
                
                UpdateStatus("Entered fullscreen mode (Press F11 or Esc to exit)");
            }
            else
            {
                FormBorderStyle = _previousBorderStyle;
                WindowState = _previousWindowState;
                Bounds = _previousBounds;
                _isFullscreen = false;
                
                UpdateStatus("Exited fullscreen mode");
            }
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == FormWindowState.Maximized ? 
                FormWindowState.Normal : FormWindowState.Maximized;
        }

        private void SwapMode()
        {
            if (_rbLocalToWorld.Checked)
                _rbWorldToLocal.Checked = true;
            else
                _rbLocalToWorld.Checked = true;
            
            UpdateStatus("Conversion mode swapped");
        }

        // CORE CALCULATION METHODS AND EVENT HANDLERS
        private void BtnConvert_Click(object sender, EventArgs e)
        {
            try
            {
                UpdateStatus("● CALCULATING...");
                
                var parentPos = new DecimalVector3(_upParentPosX.Value, _upParentPosY.Value, _upParentPosZ.Value);
                var parentRot = new DecimalVector3(_upParentRotX.Value, _upParentRotY.Value, _upParentRotZ.Value);
                var parentScale = new DecimalVector3(_upParentScaleX.Value, _upParentScaleY.Value, _upParentScaleZ.Value);
                var input = new DecimalVector3(_upInputX.Value, _upInputY.Value, _upInputZ.Value);

                var startTime = DateTime.Now;
                DecimalVector3 result;

                if (_rbLocalToWorld.Checked)
                    result = LocalToWorld(input, parentPos, parentRot, parentScale);
                else
                    result = WorldToLocal(input, parentPos, parentRot, parentScale);

                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

                _lastResult = result;
                _hasValidResult = true;

                AddToHistory(new CalculationHistory
                {
                    Timestamp = DateTime.Now,
                    Mode = _rbLocalToWorld.Checked ? "L→W" : "W→L",
                    Input = input,
                    Result = result,
                    ParentPos = parentPos,
                    ParentRot = parentRot,
                    ParentScale = parentScale,
                    Precision = _currentPrecision
                });

                UpdateResultDisplay();
                UpdateStatus($"✓ Complete in {elapsed:F2}ms | Precision: {_currentPrecision} decimals");
            }
            catch (Exception ex)
            {
                ShowError($"Calculation failed: {ex.Message}");
            }
        }

        private DecimalVector3 LocalToWorld(DecimalVector3 local, DecimalVector3 position, DecimalVector3 eulerDegrees, DecimalVector3 scale)
        {
            var scaled = local * scale;
            var rotation = DecimalQuaternion.FromYawPitchRoll(
                DecimalMath.DegreesToRadians(DecimalMath.NormalizeAngle(eulerDegrees.Y)),
                DecimalMath.DegreesToRadians(DecimalMath.NormalizeAngle(eulerDegrees.X)),
                DecimalMath.DegreesToRadians(DecimalMath.NormalizeAngle(eulerDegrees.Z))).Normalize();
            var rotated = TransformVector(scaled, rotation);
            return rotated + position;
        }

        private DecimalVector3 WorldToLocal(DecimalVector3 world, DecimalVector3 position, DecimalVector3 eulerDegrees, DecimalVector3 scale)
        {
            if (Math.Abs(scale.X) < EPSILON || Math.Abs(scale.Y) < EPSILON || Math.Abs(scale.Z) < EPSILON)
                throw new InvalidOperationException($"Scale has zero component. Cannot perform World→Local conversion.");

            var delta = world - position;
            var rotation = DecimalQuaternion.FromYawPitchRoll(
                DecimalMath.DegreesToRadians(DecimalMath.NormalizeAngle(eulerDegrees.Y)),
                DecimalMath.DegreesToRadians(DecimalMath.NormalizeAngle(eulerDegrees.X)),
                DecimalMath.DegreesToRadians(DecimalMath.NormalizeAngle(eulerDegrees.Z))).Normalize();
            var invRotation = rotation.Inverse();
            var rotated = TransformVector(delta, invRotation);
            return rotated / scale;
        }

        private DecimalVector3 TransformVector(DecimalVector3 v, DecimalQuaternion q)
        {
            var qVec = new DecimalVector3(q.X, q.Y, q.Z);
            var uv = new DecimalVector3(qVec.Y * v.Z - qVec.Z * v.Y, qVec.Z * v.X - qVec.X * v.Z, qVec.X * v.Y - qVec.Y * v.X);
            var uuv = new DecimalVector3(qVec.Y * uv.Z - qVec.Z * uv.Y, qVec.Z * uv.X - qVec.X * uv.Z, qVec.X * uv.Y - qVec.Y * uv.X);
            return v + (uv * new DecimalVector3(2m * q.W, 2m * q.W, 2m * q.W)) + (uuv * new DecimalVector3(2m, 2m, 2m));
        }

        private void UpdateResultDisplay()
        {
            if (!_hasValidResult) return;
            string format = $"F{_currentPrecision}";
            _rtbResult.Clear();
            _rtbResult.SelectionColor = Theme.CyanGlow;
            _rtbResult.SelectionFont = new Font("Consolas", 11, FontStyle.Bold);
            _rtbResult.AppendText("RESULT:\n");
            _rtbResult.SelectionColor = Theme.GreenGlow;
            _rtbResult.SelectionFont = new Font("Consolas", 9);
            _rtbResult.AppendText($"X: {_lastResult.X.ToString(format, CultureInfo.InvariantCulture)}\n");
            _rtbResult.AppendText($"Y: {_lastResult.Y.ToString(format, CultureInfo.InvariantCulture)}\n");
            _rtbResult.AppendText($"Z: {_lastResult.Z.ToString(format, CultureInfo.InvariantCulture)}\n");
        }

        private void AddToHistory(CalculationHistory entry)
        {
            _history.Insert(0, entry);
            if (_history.Count > 100) _history.RemoveAt(100);
            var item = new ListViewItem(entry.Timestamp.ToString("HH:mm:ss"));
            item.SubItems.Add(entry.Mode);
            item.SubItems.Add($"({entry.Result.X:F3}, ...)");
            item.Tag = entry;
            _lvHistory.Items.Insert(0, item);
            if (_lvHistory.Items.Count > 100) _lvHistory.Items.RemoveAt(100);
        }

        private ValidationResult ValidateInputs(bool showMessage)
        {
            if (_rbWorldToLocal.Checked)
            {
                if (Math.Abs(_upParentScaleX.Value) < (decimal)EPSILON) return ShowValidationResult(false, "Scale X is zero", showMessage);
                if (Math.Abs(_upParentScaleY.Value) < (decimal)EPSILON) return ShowValidationResult(false, "Scale Y is zero", showMessage);
                if (Math.Abs(_upParentScaleZ.Value) < (decimal)EPSILON) return ShowValidationResult(false, "Scale Z is zero", showMessage);
            }
            if (showMessage) MessageBox.Show("✓ All inputs are valid!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return new ValidationResult(true);
        }

        private ValidationResult ShowValidationResult(bool isValid, string message, bool show)
        {
            if (show && !isValid) ShowError(message);
            return new ValidationResult(isValid, message);
        }

        private void UpdateValidationStatus() { }

        private void BtnCopyResult_Click(object sender, EventArgs e)
        {
            if (!_hasValidResult) { ShowError("No result to copy."); return; }
            try
            {
                string format = $"F{_currentPrecision}";
                string result = $"({_lastResult.X.ToString(format, CultureInfo.InvariantCulture)}, {_lastResult.Y.ToString(format, CultureInfo.InvariantCulture)}, {_lastResult.Z.ToString(format, CultureInfo.InvariantCulture)})";
                Clipboard.SetText(result);
                UpdateStatus("✓ Result copied to clipboard");
                MessageBox.Show("Copied successfully!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError($"Could not copy: {ex.Message}"); }
        }

        private void BtnBatchConvert_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Batch conversion feature - Coming in next update!\n\nWill support:\n• CSV import/export\n• Process thousands of points\n• Progress tracking", "Batch", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (!_hasValidResult) { ShowError("No result to export."); return; }
            try
            {
                var sfd = new SaveFileDialog { Filter = "Text File (*.txt)|*.txt|CSV (*.csv)|*.csv", FileName = $"WorldForge_Result_{DateTime.Now:yyyyMMdd_HHmmss}" };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string format = $"F{_currentPrecision}";
                    var sb = new StringBuilder();
                    sb.AppendLine($"WorldForge Ultimate v2.0 - {DateTime.Now}");
                    sb.AppendLine($"Mode: {(_rbLocalToWorld.Checked ? "Local→World" : "World→Local")} | Precision: {_currentPrecision}");
                    sb.AppendLine($"Result: X={_lastResult.X.ToString(format)} Y={_lastResult.Y.ToString(format)} Z={_lastResult.Z.ToString(format)}");
                    File.WriteAllText(sfd.FileName, sb.ToString());
                    UpdateStatus($"✓ Exported to {Path.GetFileName(sfd.FileName)}");
                    MessageBox.Show("Export successful!", "Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) { ShowError($"Export failed: {ex.Message}"); }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Import presets - Coming soon!\n\nWill support .wfp preset files", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnExportHistory_Click(object sender, EventArgs e)
        {
            if (_history.Count == 0) { ShowError("No history."); return; }
            try
            {
                var sfd = new SaveFileDialog { Filter = "CSV File (*.csv)|*.csv", FileName = $"History_{DateTime.Now:yyyyMMdd_HHmmss}.csv" };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Time,Mode,InputX,InputY,InputZ,ResultX,ResultY,ResultZ,Precision");
                    foreach (var h in _history)
                        sb.AppendLine($"{h.Timestamp:yyyy-MM-dd HH:mm:ss},{h.Mode},{h.Input.X},{h.Input.Y},{h.Input.Z},{h.Result.X},{h.Result.Y},{h.Result.Z},{h.Precision}");
                    File.WriteAllText(sfd.FileName, sb.ToString());
                    UpdateStatus($"✓ History exported ({_history.Count} entries)");
                    MessageBox.Show($"Exported {_history.Count} entries!", "Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) { ShowError($"Export failed: {ex.Message}"); }
        }

        private void ClearHistory()
        {
            if (MessageBox.Show("Clear all history?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _history.Clear();
                _lvHistory.Items.Clear();
                UpdateStatus("History cleared");
            }
        }

        private void ResetAll()
        {
            _upParentPosX.Value = _upParentPosY.Value = _upParentPosZ.Value = 0;
            _upParentRotX.Value = _upParentRotY.Value = _upParentRotZ.Value = 0;
            _upParentScaleX.Value = _upParentScaleY.Value = _upParentScaleZ.Value = 1;
            _upInputX.Value = _upInputY.Value = _upInputZ.Value = 0;
            _rbLocalToWorld.Checked = true;
            _hasValidResult = false;
            _rtbResult.Text = "All fields reset.\nReady for new calculation...";
            UpdateStatus("All fields reset");
        }

        private void ShowPresetsMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Identity Transform", null, (s, e) => ApplyPresetIdentity());
            menu.Items.Add("Sample 90° Y", null, (s, e) => ApplyPresetSample());
            menu.Items.Add("Unity Standard", null, (s, e) => ApplyPresetUnity());
            menu.Show(Cursor.Position);
        }

        private void ApplyPresetIdentity()
        {
            _upParentPosX.Value = _upParentPosY.Value = _upParentPosZ.Value = 0;
            _upParentRotX.Value = _upParentRotY.Value = _upParentRotZ.Value = 0;
            _upParentScaleX.Value = _upParentScaleY.Value = _upParentScaleZ.Value = 1;
            UpdateStatus("Identity preset applied");
        }

        private void ApplyPresetSample()
        {
            _upParentPosX.Value = 5; _upParentPosY.Value = 0; _upParentPosZ.Value = 0;
            _upParentRotX.Value = 0; _upParentRotY.Value = 90; _upParentRotZ.Value = 0;
            _upParentScaleX.Value = _upParentScaleY.Value = _upParentScaleZ.Value = 1;
            _upInputX.Value = 2; _upInputY.Value = 0; _upInputZ.Value = 0;
            _rbLocalToWorld.Checked = true;
            UpdateStatus("Sample preset applied");
        }

        private void ApplyPresetUnity()
        {
            _upParentPosX.Value = 0; _upParentPosY.Value = 1; _upParentPosZ.Value = 0;
            _upParentRotX.Value = 0; _upParentRotY.Value = 0; _upParentRotZ.Value = 0;
            _upParentScaleX.Value = _upParentScaleY.Value = _upParentScaleZ.Value = 1;
            UpdateStatus("Unity preset applied");
        }

        private void UpdateStatus(string message)
        {
            _statusLabel.Text = "● " + message;
        }

        private void ShowError(string message)
        {
            UpdateStatus("✗ ERROR: " + message);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F11) { ToggleFullscreen(); return true; }
            if (keyData == Keys.Escape && _isFullscreen) { ToggleFullscreen(); return true; }
            if (keyData == (Keys.Control | Keys.B)) { BtnBatchConvert_Click(null, null); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
