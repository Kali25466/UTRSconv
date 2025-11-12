// WorldForge - TRS Transform Converter
// Optimized and fixed version

using System;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Windows.Forms;

namespace WorldForge
{
    public class MainForm : Form
    {
        // UI controls
        private RadioButton _rbLocalToWorld;
        private RadioButton _rbWorldToLocal;

        // Parent TRS
        private NumericUpDown _upParentPosX, _upParentPosY, _upParentPosZ;
        private NumericUpDown _upParentRotX, _upParentRotY, _upParentRotZ;
        private NumericUpDown _upParentScaleX, _upParentScaleY, _upParentScaleZ;

        // Input position
        private NumericUpDown _upInputX, _upInputY, _upInputZ;

        // Buttons and results
        private Button _btnConvert, _btnCopyResult, _btnPresetIdentity, _btnPresetSample;
        private Label _lblResult;
        private ComboBox _cbPrecision;

        public MainForm()
        {
            Text = "WorldForge — TRS Converter";
            ClientSize = new Size(544, 380);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;

            InitUI();
        }

        private void InitUI()
        {
            const int leftMargin = 16;
            int top = 12;

            // Mode section
            var lblMode = new Label { Text = "Mode:", Left = leftMargin, Top = top + 4, Width = 40, AutoSize = false };
            _rbLocalToWorld = new RadioButton { Text = "Local → World", Left = leftMargin + 46, Top = top, Checked = true, Width = 120 };
            _rbWorldToLocal = new RadioButton { Text = "World → Local", Left = leftMargin + 180, Top = top, Width = 120 };
            Controls.AddRange(new Control[] { lblMode, _rbLocalToWorld, _rbWorldToLocal });
            top += 36;

            // Parent TRS label
            var lblParent = new Label { Text = "Parent Transform (TRS)", Left = leftMargin, Top = top, Font = new Font(Font.FontFamily, 9, FontStyle.Bold), AutoSize = true };
            Controls.Add(lblParent);
            top += 22;

            // Parent Position
            var lblPP = new Label { Text = "Position (x, y, z):", Left = leftMargin, Top = top + 6, Width = 120, AutoSize = false };
            _upParentPosX = CreateNumeric(leftMargin + 128, top, 80, 0m);
            _upParentPosY = CreateNumeric(leftMargin + 216, top, 80, 0m);
            _upParentPosZ = CreateNumeric(leftMargin + 304, top, 80, 0m);
            Controls.AddRange(new Control[] { lblPP, _upParentPosX, _upParentPosY, _upParentPosZ });
            top += 36;

            // Parent Rotation (degrees)
            var lblPR = new Label { Text = "Rotation Euler ° (X, Y, Z):", Left = leftMargin, Top = top + 6, Width = 160, AutoSize = false };
            _upParentRotX = CreateNumeric(leftMargin + 160, top, 80, 0m, -360m, 360m);
            _upParentRotY = CreateNumeric(leftMargin + 248, top, 80, 0m, -360m, 360m);
            _upParentRotZ = CreateNumeric(leftMargin + 336, top, 80, 0m, -360m, 360m);
            Controls.AddRange(new Control[] { lblPR, _upParentRotX, _upParentRotY, _upParentRotZ });
            top += 36;

            // Parent Scale
            var lblPS = new Label { Text = "Scale (x, y, z):", Left = leftMargin, Top = top + 6, Width = 120, AutoSize = false };
            _upParentScaleX = CreateNumeric(leftMargin + 128, top, 80, 1m, -100000m, 100000m);
            _upParentScaleY = CreateNumeric(leftMargin + 216, top, 80, 1m, -100000m, 100000m);
            _upParentScaleZ = CreateNumeric(leftMargin + 304, top, 80, 1m, -100000m, 100000m);
            Controls.AddRange(new Control[] { lblPS, _upParentScaleX, _upParentScaleY, _upParentScaleZ });
            top += 40;

            // Input label
            var lblInput = new Label { Text = "Input Position", Left = leftMargin, Top = top, Font = new Font(Font.FontFamily, 9, FontStyle.Bold), AutoSize = true };
            Controls.Add(lblInput);
            top += 22;

            // Input Position fields
            var lblIn = new Label { Text = "X, Y, Z:", Left = leftMargin, Top = top + 6, Width = 80, AutoSize = false };
            _upInputX = CreateNumeric(leftMargin + 84, top, 96, 0m);
            _upInputY = CreateNumeric(leftMargin + 188, top, 96, 0m);
            _upInputZ = CreateNumeric(leftMargin + 292, top, 96, 0m);
            Controls.AddRange(new Control[] { lblIn, _upInputX, _upInputY, _upInputZ });
            top += 44;

            // Convert Button
            _btnConvert = new Button { Text = "Convert", Left = leftMargin, Top = top, Width = 220, Height = 36 };
            _btnConvert.Click += BtnConvert_Click;
            Controls.Add(_btnConvert);

            // Copy result
            _btnCopyResult = new Button { Text = "Copy Result", Left = leftMargin + 236, Top = top, Width = 120, Height = 36 };
            _btnCopyResult.Click += BtnCopyResult_Click;
            Controls.Add(_btnCopyResult);

            // Presets
            _btnPresetIdentity = new Button { Text = "Preset: Identity", Left = leftMargin + 366, Top = top, Width = 160, Height = 18 };
            _btnPresetIdentity.Click += (s, e) => ApplyPresetIdentity();
            Controls.Add(_btnPresetIdentity);

            _btnPresetSample = new Button { Text = "Preset: Sample", Left = leftMargin + 366, Top = top + 20, Width = 160, Height = 18 };
            _btnPresetSample.Click += (s, e) => ApplyPresetSample();
            Controls.Add(_btnPresetSample);

            top += 56;

            // Result label
            _lblResult = new Label
            {
                Text = "Result: (---, ---, ---)",
                Left = leftMargin,
                Top = top,
                Width = 512,
                Height = 40,
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4)
            };
            Controls.Add(_lblResult);

            // Precision dropdown
            var lblPrec = new Label { Text = "Precision:", Left = leftMargin, Top = top + 46, Width = 64, AutoSize = false };
            _cbPrecision = new ComboBox { Left = leftMargin + 72, Top = top + 42, Width = 72, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbPrecision.Items.AddRange(new object[] { "2", "3", "4", "6", "8" });
            _cbPrecision.SelectedIndex = 2; // 4 decimals
            Controls.AddRange(new Control[] { lblPrec, _cbPrecision });

            // Helper label
            var lblHint = new Label { Text = "Euler order: Yaw (Y), Pitch (X), Roll (Z). Rotation in degrees.", Left = leftMargin + 160, Top = top + 46, Width = 360, AutoSize = false };
            Controls.Add(lblHint);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                BtnCopyResult_Click(null, null);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                BtnConvert_Click(null, null);
                e.Handled = true;
            }
        }

        private NumericUpDown CreateNumeric(int left, int top, int controlWidth, decimal initial, decimal min = -10000m, decimal max = 10000m)
        {
            return new NumericUpDown
            {
                Left = left,
                Top = top,
                Width = controlWidth,
                DecimalPlaces = 4,
                Minimum = min,
                Maximum = max,
                Value = initial,
                ThousandsSeparator = false,
                Increment = 0.1m
            };
        }

        private void BtnCopyResult_Click(object sender, EventArgs e)
        {
            try
            {
                var text = _lblResult.Text.Replace("Result: ", "");
                Clipboard.SetText(text);
                MessageBox.Show("Result copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not copy to clipboard: {ex.Message}", "Clipboard Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplyPresetIdentity()
        {
            _upParentPosX.Value = 0; _upParentPosY.Value = 0; _upParentPosZ.Value = 0;
            _upParentRotX.Value = 0; _upParentRotY.Value = 0; _upParentRotZ.Value = 0;
            _upParentScaleX.Value = 1; _upParentScaleY.Value = 1; _upParentScaleZ.Value = 1;
        }

        private void ApplyPresetSample()
        {
            _upParentPosX.Value = 5; _upParentPosY.Value = 0; _upParentPosZ.Value = 0;
            _upParentRotX.Value = 0; _upParentRotY.Value = 90; _upParentRotZ.Value = 0;
            _upParentScaleX.Value = 1; _upParentScaleY.Value = 1; _upParentScaleZ.Value = 1;
            _upInputX.Value = 2; _upInputY.Value = 0; _upInputZ.Value = 0;
            _rbLocalToWorld.Checked = true;
        }

        private void BtnConvert_Click(object sender, EventArgs e)
        {
            try
            {
                var parentPos = new Vector3((float)_upParentPosX.Value, (float)_upParentPosY.Value, (float)_upParentPosZ.Value);
                var parentRotDegrees = new Vector3((float)_upParentRotX.Value, (float)_upParentRotY.Value, (float)_upParentRotZ.Value);
                var parentScale = new Vector3((float)_upParentScaleX.Value, (float)_upParentScaleY.Value, (float)_upParentScaleZ.Value);
                var input = new Vector3((float)_upInputX.Value, (float)_upInputY.Value, (float)_upInputZ.Value);

                Vector3 result = _rbLocalToWorld.Checked
                    ? LocalToWorld(input, parentPos, parentRotDegrees, parentScale)
                    : WorldToLocal(input, parentPos, parentRotDegrees, parentScale);

                int precision = int.Parse(_cbPrecision.SelectedItem.ToString(), CultureInfo.InvariantCulture);
                string fmt = $"F{precision}";
                _lblResult.Text = $"Result: ({result.X.ToString(fmt, CultureInfo.InvariantCulture)}, {result.Y.ToString(fmt, CultureInfo.InvariantCulture)}, {result.Z.ToString(fmt, CultureInfo.InvariantCulture)})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Conversion failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Vector3 LocalToWorld(Vector3 local, Vector3 parentPosition, Vector3 parentEulerDegrees, Vector3 parentScale)
        {
            var scaled = local * parentScale;
            var rotation = QuaternionFromEulerDegrees(parentEulerDegrees);
            var rotated = TransformVector(scaled, rotation);
            return rotated + parentPosition;
        }

        private static Vector3 WorldToLocal(Vector3 world, Vector3 parentPosition, Vector3 parentEulerDegrees, Vector3 parentScale)
        {
            const float epsilon = 1e-9f;
            if (Math.Abs(parentScale.X) < epsilon || Math.Abs(parentScale.Y) < epsilon || Math.Abs(parentScale.Z) < epsilon)
            {
                throw new InvalidOperationException("Parent scale has zero or near-zero component; cannot invert scale.");
            }

            var delta = world - parentPosition;
            var rotation = QuaternionFromEulerDegrees(parentEulerDegrees);
            var invRotation = Quaternion.Inverse(rotation);
            var rotatedBack = TransformVector(delta, invRotation);
            return rotatedBack / parentScale;
        }

        private static Quaternion QuaternionFromEulerDegrees(Vector3 eulerDegrees)
        {
            const float degToRad = (float)(Math.PI / 180.0);
            float pitch = eulerDegrees.X * degToRad;
            float yaw = eulerDegrees.Y * degToRad;
            float roll = eulerDegrees.Z * degToRad;
            return Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
        }

        private static Vector3 TransformVector(Vector3 v, Quaternion q)
        {
            var qVec = new Vector3(q.X, q.Y, q.Z);
            var uv = Vector3.Cross(qVec, v);
            var uuv = Vector3.Cross(qVec, uv);
            return v + (uv * (2.0f * q.W)) + (uuv * 2.0f);
        }
    }
}
