using System;
using System.Drawing;
using System.Windows.Forms;

namespace people_pdf
{
    public partial class CameraCaptureForm : Form
    {
        private WebcamHandler webcamHandler;
        private PictureBox previewBox;
        private Button captureButton;
        private Button cancelButton;
        private Button retakeButton;
        private Label statusLabel;
        private PictureBox capturedImageBox;
        private System.Windows.Forms.Timer previewTimer; // FIXED: Specify full namespace
        private bool imageCaptured = false;

        public Bitmap CapturedImage { get; private set; }

        public CameraCaptureForm()
        {
            InitializeComponent();
            InitializeCamera();
        }

        private void InitializeComponent()
        {
            this.Text = "Camera Capture";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            // Status label
            statusLabel = new Label
            {
                Text = "Initializing camera...",
                Location = new Point(20, 15),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            // Live preview box
            previewBox = new PictureBox
            {
                Location = new Point(20, 50),
                Size = new Size(320, 240),
                BorderStyle = BorderStyle.Fixed3D,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            // Captured image preview (initially hidden)
            capturedImageBox = new PictureBox
            {
                Location = new Point(360, 50),
                Size = new Size(320, 240),
                BorderStyle = BorderStyle.Fixed3D,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(245, 245, 245),
                Visible = false
            };

            // Capture button
            captureButton = new Button
            {
                Text = "📷 Capture Photo",
                Location = new Point(20, 320),
                Size = new Size(150, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            captureButton.FlatAppearance.BorderSize = 0;
            captureButton.Click += CaptureButton_Click;

            // Retake button (initially hidden)
            retakeButton = new Button
            {
                Text = "🔄 Retake",
                Location = new Point(180, 320),
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            retakeButton.FlatAppearance.BorderSize = 0;
            retakeButton.Click += RetakeButton_Click;

            // Use Photo button (initially hidden)
            Button usePhotoButton = new Button
            {
                Text = "✅ Use This Photo",
                Location = new Point(360, 320),
                Size = new Size(150, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(34, 139, 34),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Visible = false,
                DialogResult = DialogResult.OK
            };
            usePhotoButton.FlatAppearance.BorderSize = 0;

            // Cancel button
            cancelButton = new Button
            {
                Text = "❌ Cancel",
                Location = new Point(520, 320),
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(128, 128, 128),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            cancelButton.FlatAppearance.BorderSize = 0;

            // Add controls
            this.Controls.Add(statusLabel);
            this.Controls.Add(previewBox);
            this.Controls.Add(capturedImageBox);
            this.Controls.Add(captureButton);
            this.Controls.Add(retakeButton);
            this.Controls.Add(usePhotoButton);
            this.Controls.Add(cancelButton);

            // Store reference to use photo button for event handling
            usePhotoButton.Click += (s, e) => {
                if (CapturedImage != null)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };

            // FIXED: Setup preview timer with full namespace
            previewTimer = new System.Windows.Forms.Timer();
            previewTimer.Interval = 100; // Update every 100ms
            previewTimer.Tick += PreviewTimer_Tick;

            this.FormClosing += CameraCaptureForm_FormClosing;
        }

        private void InitializeCamera()
        {
            try
            {
                if (!WebcamHandler.IsCameraAvailable())
                {
                    statusLabel.Text = "❌ No camera found. Please connect a camera and try again.";
                    statusLabel.ForeColor = Color.Red;
                    return;
                }

                webcamHandler = new WebcamHandler();

                if (webcamHandler.InitializeCamera(previewBox))
                {
                    statusLabel.Text = "📹 Camera ready - Position yourself and click 'Capture Photo'";
                    statusLabel.ForeColor = Color.Green;
                    captureButton.Enabled = true;

                    // Start preview timer
                    previewTimer.Start();
                }
                else
                {
                    statusLabel.Text = "❌ Failed to initialize camera. Please check camera permissions.";
                    statusLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"❌ Camera error: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
        }

        private void PreviewTimer_Tick(object sender, EventArgs e)
        {
            if (!imageCaptured && webcamHandler != null)
            {
                try
                {
                    Bitmap frame = webcamHandler.CaptureImage();
                    if (frame != null)
                    {
                        previewBox.Image?.Dispose();
                        previewBox.Image = frame;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Preview error: {ex.Message}");
                }
            }
        }

        private void CaptureButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (webcamHandler != null)
                {
                    previewTimer.Stop();

                    Bitmap capturedBitmap = webcamHandler.CaptureImage();
                    if (capturedBitmap != null)
                    {
                        CapturedImage = new Bitmap(capturedBitmap);
                        capturedImageBox.Image?.Dispose();
                        capturedImageBox.Image = capturedBitmap;

                        // Show captured image and hide preview
                        capturedImageBox.Visible = true;
                        previewBox.Visible = false;

                        // Update UI
                        statusLabel.Text = "📸 Photo captured! Review and choose an option below.";
                        statusLabel.ForeColor = Color.Blue;

                        captureButton.Visible = false;
                        retakeButton.Visible = true;

                        // Show use photo button
                        foreach (Control control in this.Controls)
                        {
                            if (control is Button btn && btn.Text.Contains("Use This Photo"))
                            {
                                btn.Visible = true;
                                break;
                            }
                        }

                        imageCaptured = true;
                    }
                    else
                    {
                        MessageBox.Show("Failed to capture image. Please try again.",
                            "Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        previewTimer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing image: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                previewTimer.Start();
            }
        }

        private void RetakeButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Reset UI
                capturedImageBox.Visible = false;
                previewBox.Visible = true;
                captureButton.Visible = true;
                retakeButton.Visible = false;

                // Hide use photo button
                foreach (Control control in this.Controls)
                {
                    if (control is Button btn && btn.Text.Contains("Use This Photo"))
                    {
                        btn.Visible = false;
                        break;
                    }
                }

                statusLabel.Text = "📹 Camera ready - Position yourself and click 'Capture Photo'";
                statusLabel.ForeColor = Color.Green;
                captureButton.Enabled = true;

                // Clear captured image
                CapturedImage?.Dispose();
                CapturedImage = null;
                capturedImageBox.Image?.Dispose();
                capturedImageBox.Image = null;
                imageCaptured = false;

                // Restart preview
                previewTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error restarting camera: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CameraCaptureForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                previewTimer?.Stop();
                previewTimer?.Dispose();
                webcamHandler?.Dispose();
                previewBox.Image?.Dispose();
                capturedImageBox.Image?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing camera form: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                previewTimer?.Dispose();
                webcamHandler?.Dispose();
                CapturedImage?.Dispose();
                previewBox.Image?.Dispose();
                capturedImageBox.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
