using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace people_pdf
{
    public partial class Form1 : Form
    {
        private SaveFileDialog saveFileDialogPdf;   
        private ComboBox comboBoxAgreementType;         
        private FingerprintHandler fingerprintHandler;

        // Party 1
        private TextBox textBoxParty1Name;
        private TextBox textBoxParty1Aadhar;
        private TextBox textBoxParty1Address;
        private TextBox textBoxParty1Pan;
        private TextBox textBoxParty1Mobile;
        private PictureBox pictureBoxParty1Image;
        private PictureBox pictureBoxParty1Fingerprint;
        private Button buttonUploadParty1Image;
        private Button buttonUploadParty1Fingerprint;
        private Button buttonCaptureParty1Fingerprint;
        private Button buttonCameraParty1Image; // NEW: Camera button

        // Party 2
        private TextBox textBoxParty2Name;
        private TextBox textBoxParty2Aadhar;
        private TextBox textBoxParty2Address;
        private TextBox textBoxParty2Pan;
        private TextBox textBoxParty2Mobile;
        private PictureBox pictureBoxParty2Image;
        private PictureBox pictureBoxParty2Fingerprint;
        private Button buttonUploadParty2Image;
        private Button buttonUploadParty2Fingerprint;
        private Button buttonCaptureParty2Fingerprint;
        private Button buttonCameraParty2Image; // NEW: Camera button

        // Party 3
        private TextBox textBoxParty3Name;
        private TextBox textBoxParty3Aadhar;
        private TextBox textBoxParty3Address;
        private TextBox textBoxParty3Pan;
        private TextBox textBoxParty3Mobile;
        private PictureBox pictureBoxParty3Image;
        private PictureBox pictureBoxParty3Fingerprint;
        private Button buttonUploadParty3Image;
        private Button buttonUploadParty3Fingerprint;
        private Button buttonCaptureParty3Fingerprint;
        private Button buttonCameraParty3Image; // NEW: Camera button

        // Party 4
        private TextBox textBoxParty4Name;
        private TextBox textBoxParty4Aadhar;
        private TextBox textBoxParty4Address;
        private TextBox textBoxParty4Pan;
        private TextBox textBoxParty4Mobile;
        private PictureBox pictureBoxParty4Image;
        private PictureBox pictureBoxParty4Fingerprint;
        private Button buttonUploadParty4Image;
        private Button buttonUploadParty4Fingerprint;
        private Button buttonCaptureParty4Fingerprint;
        private Button buttonCameraParty4Image; // NEW: Camera button

        // Party 5
        private TextBox textBoxParty5Name;
        private TextBox textBoxParty5Aadhar;
        private TextBox textBoxParty5Address;
        private TextBox textBoxParty5Pan;
        private TextBox textBoxParty5Mobile;
        private PictureBox pictureBoxParty5Image;
        private PictureBox pictureBoxParty5Fingerprint;
        private Button buttonUploadParty5Image;
        private Button buttonUploadParty5Fingerprint;
        private Button buttonCaptureParty5Fingerprint;
        private Button buttonCameraParty5Image; // NEW: Camera button

        // Party 6
        private TextBox textBoxParty6Name;
        private TextBox textBoxParty6Aadhar;
        private TextBox textBoxParty6Address;
        private TextBox textBoxParty6Pan;
        private TextBox textBoxParty6Mobile;
        private PictureBox pictureBoxParty6Image;
        private PictureBox pictureBoxParty6Fingerprint;
        private Button buttonUploadParty6Image;
        private Button buttonUploadParty6Fingerprint;
        private Button buttonCaptureParty6Fingerprint;
        private Button buttonCameraParty6Image; // NEW: Camera button

        // Document number
        private TextBox textBoxDocumentNumber;
        private Button buttonGeneratePdf;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomLayout();
            InitializeFingerprintHandler();
        }

        private void InitializeFingerprintHandler()
        {
            try
            {
                fingerprintHandler = new FingerprintHandler();
                if (!fingerprintHandler.InitializeDevice())
                {
                    MessageBox.Show("Warning: Fingerprint device not available. You can still upload fingerprint images manually.",
                        "Device Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing fingerprint handler: {ex.Message}\n\nYou can still upload fingerprint images manually.",
                    "Initialization Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeCustomLayout()
        {
            this.Controls.Clear();
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Agreement PDF Generator with Fingerprint Scanner & Camera";
            this.MinimumSize = new Size(1400, 1000);
            this.BackColor = Color.FromArgb(248, 248, 248);
            this.AutoScroll = true;
            saveFileDialogPdf = new SaveFileDialog();
            InitializeLayoutWithGroupBoxes();
            SetupEventHandlers();
        }

        private void InitializeLayoutWithGroupBoxes()
        {
            int formWidth = this.ClientSize.Width;
            int formHeight = this.ClientSize.Height;

            // Header with Agreement Type dropdown
            Label lblAgreementType = new Label
            {
                Text = "Type of Agreement:",
                Location = new Point(20, 15),
                Size = new Size(150, 25),
                Font = new System.Drawing.Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            comboBoxAgreementType = new ComboBox
            {
                Location = new Point(170, 15),
                Size = new Size(200, 25),
                Font = new System.Drawing.Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxAgreementType.Items.AddRange(new string[] { "Affidavit", "Agreement" });
            comboBoxAgreementType.SelectedIndex = 0;
                
            // Add device status labels
            Label lblFingerprintStatus = new Label
            {
                Text = fingerprintHandler?.IsDeviceReady() == true ? "📱 Fingerprint Scanner: Ready" : "⚠️ Fingerprint Scanner: Not Available",
                Location = new Point(400, 15),
                Size = new Size(300, 25),
                Font = new System.Drawing.Font("Segoe UI", 9),
                ForeColor = fingerprintHandler?.IsDeviceReady() == true ? Color.Green : Color.Orange
            };

            // NEW: Add camera status
            Label lblCameraStatus = new Label
            {
                Text = WebcamHandler.IsCameraAvailable() ? "📷 Camera: Available" : "⚠️ Camera: Not Available",
                Location = new Point(720, 15),
                Size = new Size(200, 25),
                Font = new System.Drawing.Font("Segoe UI", 9),
                ForeColor = WebcamHandler.IsCameraAvailable() ? Color.Green : Color.Orange
            };

            // Calculate responsive dimensions for 6 parties (3x2 grid)
            int groupBoxWidth = Math.Min((formWidth - 80) / 3, 450);
            int groupBoxHeight = 420;

            // Create GroupBoxes for 6 parties in 3x2 grid
            GroupBox[] partyGroups = new GroupBox[6];
            for (int i = 0; i < 6; i++)
            {
                int row = i / 3;
                int col = i % 3;
                partyGroups[i] = new GroupBox
                {
                    Text = $"PARTY {i + 1} DETAILS",
                    Location = new Point(20 + col * (groupBoxWidth + 20), 60 + row * (groupBoxHeight + 20)),
                    Size = new Size(groupBoxWidth, groupBoxHeight),
                    Font = new System.Drawing.Font("Segoe UI", 10, FontStyle.Bold),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(64, 64, 64)
                };
            }

            // Initialize controls for each party
            CreatePartyControls(partyGroups[0], 1);
            CreatePartyControls(partyGroups[1], 2);
            CreatePartyControls(partyGroups[2], 3);
            CreatePartyControls(partyGroups[3], 4);
            CreatePartyControls(partyGroups[4], 5);
            CreatePartyControls(partyGroups[5], 6);

            // Position document controls below all party groupboxes
            int bottomControlsY = 60 + 2 * (groupBoxHeight + 20) + 30;

            Label lblDocNumber = new Label
            {
                Text = "Document Number:",
                Location = new Point(30, bottomControlsY),
                Size = new Size(150, 25),
                Font = new System.Drawing.Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            textBoxDocumentNumber = new TextBox
            {
                Location = new Point(190, bottomControlsY),
                Size = new Size(300, 25),
                Font = new System.Drawing.Font("Segoe UI", 10),
                PlaceholderText = "Enter document number",
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Generate PDF button
            buttonGeneratePdf = new Button
            {
                Text = "Generate PDF Document",
                Location = new Point((formWidth - 250) / 2, bottomControlsY + 50),
                Size = new Size(250, 45),
                Font = new System.Drawing.Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttonGeneratePdf.FlatAppearance.BorderSize = 0;

            // Add all controls to form
            this.Controls.Add(lblAgreementType);
            this.Controls.Add(comboBoxAgreementType);
            this.Controls.Add(lblFingerprintStatus);
            this.Controls.Add(lblCameraStatus); // NEW: Add camera status
            this.Controls.AddRange(partyGroups);
            this.Controls.Add(lblDocNumber);
            this.Controls.Add(textBoxDocumentNumber);
            this.Controls.Add(buttonGeneratePdf);

            // Set AutoScrollMinSize
            this.AutoScrollMinSize = new Size(formWidth, bottomControlsY + 120);
        }

        private void CreatePartyControls(GroupBox parentGroup, int partyNumber)
        {
            int groupWidth = parentGroup.Width;
            int leftColumnX = 15;
            int leftColumnWidth = 240;
            int rightColumnX = leftColumnX + leftColumnWidth + 15;
            int rightColumnWidth = groupWidth - rightColumnX - 15;
            int startY = 30;
            int textBoxHeight = 25;
            int spacing = 30;

            // Get references to the appropriate party controls
            TextBox nameBox, aadharBox, addressBox, panBox, mobileBox;
            PictureBox imageBox, fingerprintBox;
            Button imageButton, fingerprintButton, captureButton, cameraButton; // NEW: Add cameraButton

            GetPartyControls(partyNumber, out nameBox, out aadharBox, out addressBox, out panBox, out mobileBox,
                out imageBox, out fingerprintBox, out imageButton, out fingerprintButton, out captureButton, out cameraButton);

            // Left column - Text input controls
            nameBox = new TextBox
            {
                Location = new Point(leftColumnX, startY),
                Size = new Size(leftColumnWidth, textBoxHeight),
                PlaceholderText = "Full Name (Required)",
                Font = new System.Drawing.Font("Segoe UI", 9)
            };

            aadharBox = new TextBox
            {
                Location = new Point(leftColumnX, startY + spacing),
                Size = new Size(leftColumnWidth, textBoxHeight),
                PlaceholderText = "Aadhaar Number (Required)",
                Font = new System.Drawing.Font("Segoe UI", 9),
                MaxLength = 12
            };

            addressBox = new TextBox
            {
                Location = new Point(leftColumnX, startY + spacing * 2),
                Size = new Size(leftColumnWidth, textBoxHeight * 2),
                PlaceholderText = "Complete Address (Required)",
                Font = new System.Drawing.Font("Segoe UI", 9),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            panBox = new TextBox
            {
                Location = new Point(leftColumnX, startY + spacing * 3 + 25),
                Size = new Size(leftColumnWidth, textBoxHeight),
                PlaceholderText = "PAN Number (Optional)",
                Font = new System.Drawing.Font("Segoe UI", 9),
                MaxLength = 10
            };

            mobileBox = new TextBox
            {
                Location = new Point(leftColumnX, startY + spacing * 4 + 25),
                Size = new Size(leftColumnWidth, textBoxHeight),
                PlaceholderText = "Mobile Number (Optional)",
                Font = new System.Drawing.Font("Segoe UI", 9),
                MaxLength = 10
            };

            // Right column - Image controls
            Label photoLabel = new Label
            {
                Text = "Photo:",
                Location = new Point(rightColumnX, startY),
                Size = new Size(60, 20),
                Font = new System.Drawing.Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            int imageSize = Math.Min(rightColumnWidth - 10, 120);
            int photoHeight = 80;

            imageBox = new PictureBox
            {
                Location = new Point(rightColumnX, startY + 25),
                Size = new Size(imageSize, photoHeight),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // MODIFIED: Photo buttons - Upload and Camera
            int buttonWidth = Math.Max(50, (imageSize - 5) / 2);

            imageButton = new Button
            {
                Text = "📁 Upload",
                Location = new Point(rightColumnX, startY + photoHeight + 35),
                Size = new Size(buttonWidth, 25),
                Font = new System.Drawing.Font("Segoe UI", 8),
                BackColor = Color.FromArgb(224, 224, 224),
                FlatStyle = FlatStyle.Flat
            };
            imageButton.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);

            // NEW: Camera button for photos
            cameraButton = new Button
            {
                Text = "📷 Camera",
                Location = new Point(rightColumnX + buttonWidth + 5, startY + photoHeight + 35),
                Size = new Size(buttonWidth, 25),
                Font = new System.Drawing.Font("Segoe UI", 8),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = WebcamHandler.IsCameraAvailable()
            };
            cameraButton.FlatAppearance.BorderSize = 0;

            // Fingerprint controls
            int fingerprintY = startY + photoHeight + 70;

            Label thumbLabel = new Label
            {
                Text = "Fingerprint:",
                Location = new Point(rightColumnX, fingerprintY),
                Size = new Size(100, 20),
                Font = new System.Drawing.Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            int fingerprintHeight = 80;

            fingerprintBox = new PictureBox
            {
                Location = new Point(rightColumnX, fingerprintY + 25),
                Size = new Size(imageSize, fingerprintHeight),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // Fingerprint buttons - Upload and Scan
            fingerprintButton = new Button
            {
                Text = "📄 Upload",
                Location = new Point(rightColumnX, fingerprintY + fingerprintHeight + 30),
                Size = new Size(buttonWidth, 25),
                Font = new System.Drawing.Font("Segoe UI", 8),
                BackColor = Color.FromArgb(224, 224, 224),
                FlatStyle = FlatStyle.Flat
            };
            fingerprintButton.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);

            captureButton = new Button
            {
                Text = "🖐 Scan",
                Location = new Point(rightColumnX + buttonWidth + 5, fingerprintY + fingerprintHeight + 30),
                Size = new Size(buttonWidth, 25),
                Font = new System.Drawing.Font("Segoe UI", 8),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = fingerprintHandler?.IsDeviceReady() == true
            };
            captureButton.FlatAppearance.BorderSize = 0;

            // Store references based on party number
            SetPartyControls(partyNumber, nameBox, aadharBox, addressBox, panBox, mobileBox,
                imageBox, fingerprintBox, imageButton, fingerprintButton, captureButton, cameraButton);

            // Add all controls to the parent group
            parentGroup.Controls.AddRange(new Control[] {
                nameBox, aadharBox, addressBox, panBox, mobileBox,
                photoLabel, imageBox, imageButton, cameraButton, // NEW: Added cameraButton
                thumbLabel, fingerprintBox, fingerprintButton, captureButton
            });
        }

        // MODIFIED: Updated method signature to include camera button
        private void GetPartyControls(int partyNumber, out TextBox nameBox, out TextBox aadharBox,
            out TextBox addressBox, out TextBox panBox, out TextBox mobileBox,
            out PictureBox imageBox, out PictureBox fingerprintBox, out Button imageButton,
            out Button fingerprintButton, out Button captureButton, out Button cameraButton)
        {
            // Initialize all to null, will be created in CreatePartyControls
            nameBox = null; aadharBox = null; addressBox = null; panBox = null; mobileBox = null;
            imageBox = null; fingerprintBox = null; imageButton = null; fingerprintButton = null;
            captureButton = null; cameraButton = null; // NEW: Initialize cameraButton
        }

        // MODIFIED: Updated method signature to include camera button
        private void SetPartyControls(int partyNumber, TextBox nameBox, TextBox aadharBox,
            TextBox addressBox, TextBox panBox, TextBox mobileBox,
            PictureBox imageBox, PictureBox fingerprintBox,
            Button imageButton, Button fingerprintButton, Button captureButton, Button cameraButton)
        {
            switch (partyNumber)
            {
                case 1:
                    textBoxParty1Name = nameBox; textBoxParty1Aadhar = aadharBox;
                    textBoxParty1Address = addressBox; textBoxParty1Pan = panBox;
                    textBoxParty1Mobile = mobileBox;
                    pictureBoxParty1Image = imageBox; pictureBoxParty1Fingerprint = fingerprintBox;
                    buttonUploadParty1Image = imageButton; buttonUploadParty1Fingerprint = fingerprintButton;
                    buttonCaptureParty1Fingerprint = captureButton;
                    buttonCameraParty1Image = cameraButton; // NEW
                    break;
                case 2:
                    textBoxParty2Name = nameBox; textBoxParty2Aadhar = aadharBox;
                    textBoxParty2Address = addressBox; textBoxParty2Pan = panBox;
                    textBoxParty2Mobile = mobileBox;
                    pictureBoxParty2Image = imageBox; pictureBoxParty2Fingerprint = fingerprintBox;
                    buttonUploadParty2Image = imageButton; buttonUploadParty2Fingerprint = fingerprintButton;
                    buttonCaptureParty2Fingerprint = captureButton;
                    buttonCameraParty2Image = cameraButton; // NEW
                    break;
                case 3:
                    textBoxParty3Name = nameBox; textBoxParty3Aadhar = aadharBox;
                    textBoxParty3Address = addressBox; textBoxParty3Pan = panBox;
                    textBoxParty3Mobile = mobileBox;
                    pictureBoxParty3Image = imageBox; pictureBoxParty3Fingerprint = fingerprintBox;
                    buttonUploadParty3Image = imageButton; buttonUploadParty3Fingerprint = fingerprintButton;
                    buttonCaptureParty3Fingerprint = captureButton;
                    buttonCameraParty3Image = cameraButton; // NEW
                    break;
                case 4:
                    textBoxParty4Name = nameBox; textBoxParty4Aadhar = aadharBox;
                    textBoxParty4Address = addressBox; textBoxParty4Pan = panBox;
                    textBoxParty4Mobile = mobileBox;
                    pictureBoxParty4Image = imageBox; pictureBoxParty4Fingerprint = fingerprintBox;
                    buttonUploadParty4Image = imageButton; buttonUploadParty4Fingerprint = fingerprintButton;
                    buttonCaptureParty4Fingerprint = captureButton;
                    buttonCameraParty4Image = cameraButton; // NEW
                    break;
                case 5:
                    textBoxParty5Name = nameBox; textBoxParty5Aadhar = aadharBox;
                    textBoxParty5Address = addressBox; textBoxParty5Pan = panBox;
                    textBoxParty5Mobile = mobileBox;
                    pictureBoxParty5Image = imageBox; pictureBoxParty5Fingerprint = fingerprintBox;
                    buttonUploadParty5Image = imageButton; buttonUploadParty5Fingerprint = fingerprintButton;
                    buttonCaptureParty5Fingerprint = captureButton;
                    buttonCameraParty5Image = cameraButton; // NEW
                    break;
                case 6:
                    textBoxParty6Name = nameBox; textBoxParty6Aadhar = aadharBox;
                    textBoxParty6Address = addressBox; textBoxParty6Pan = panBox;
                    textBoxParty6Mobile = mobileBox;
                    pictureBoxParty6Image = imageBox; pictureBoxParty6Fingerprint = fingerprintBox;
                    buttonUploadParty6Image = imageButton; buttonUploadParty6Fingerprint = fingerprintButton;
                    buttonCaptureParty6Fingerprint = captureButton;
                    buttonCameraParty6Image = cameraButton; // NEW
                    break;
            }
        }

        private void SetupEventHandlers()
        {
            // Event handlers for image uploads
            buttonUploadParty1Image.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty1Image);
            buttonUploadParty1Fingerprint.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty1Fingerprint);
            buttonCaptureParty1Fingerprint.Click += (s, e) => CaptureFingerprint(pictureBoxParty1Fingerprint);

            buttonUploadParty2Image.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty2Image);
            buttonUploadParty2Fingerprint.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty2Fingerprint);
            buttonCaptureParty2Fingerprint.Click += (s, e) => CaptureFingerprint(pictureBoxParty2Fingerprint);

            buttonUploadParty3Image.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty3Image);
            buttonUploadParty3Fingerprint.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty3Fingerprint);
            buttonCaptureParty3Fingerprint.Click += (s, e) => CaptureFingerprint(pictureBoxParty3Fingerprint);

            buttonUploadParty4Image.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty4Image);
            buttonUploadParty4Fingerprint.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty4Fingerprint);
            buttonCaptureParty4Fingerprint.Click += (s, e) => CaptureFingerprint(pictureBoxParty4Fingerprint);

            buttonUploadParty5Image.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty5Image);
            buttonUploadParty5Fingerprint.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty5Fingerprint);
            buttonCaptureParty5Fingerprint.Click += (s, e) => CaptureFingerprint(pictureBoxParty5Fingerprint);

            buttonUploadParty6Image.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty6Image);
            buttonUploadParty6Fingerprint.Click += (s, e) => UploadImageToPictureBox(pictureBoxParty6Fingerprint);
            buttonCaptureParty6Fingerprint.Click += (s, e) => CaptureFingerprint(pictureBoxParty6Fingerprint);

            // NEW: Camera button event handlers
            buttonCameraParty1Image.Click += (s, e) => CapturePhotoFromCamera(pictureBoxParty1Image);
            buttonCameraParty2Image.Click += (s, e) => CapturePhotoFromCamera(pictureBoxParty2Image);
            buttonCameraParty3Image.Click += (s, e) => CapturePhotoFromCamera(pictureBoxParty3Image);
            buttonCameraParty4Image.Click += (s, e) => CapturePhotoFromCamera(pictureBoxParty4Image);
            buttonCameraParty5Image.Click += (s, e) => CapturePhotoFromCamera(pictureBoxParty5Image);
            buttonCameraParty6Image.Click += (s, e) => CapturePhotoFromCamera(pictureBoxParty6Image);

            buttonGeneratePdf.Click += ButtonGeneratePdf_Click;

            // Add Aadhaar number validation (digits only)
            textBoxParty1Aadhar.KeyPress += OnlyNumeric_KeyPress;
            textBoxParty2Aadhar.KeyPress += OnlyNumeric_KeyPress;
            textBoxParty3Aadhar.KeyPress += OnlyNumeric_KeyPress;
            textBoxParty4Aadhar.KeyPress += OnlyNumeric_KeyPress;
            textBoxParty5Aadhar.KeyPress += OnlyNumeric_KeyPress;
            textBoxParty6Aadhar.KeyPress += OnlyNumeric_KeyPress;

            // Add mobile number validation (digits only)
            textBoxParty1Mobile.KeyPress += OnlyNumeric_KeyPress;
            textBoxParty2Mobile.KeyPress += OnlyNumeric_KeyPress;
            textBoxParty3Mobile.KeyPress += OnlyNumeric_KeyPress;
            textBoxParty4Mobile.KeyPress += OnlyNumeric_KeyPress;
            textBoxParty5Mobile.KeyPress += OnlyNumeric_KeyPress;
            textBoxParty6Mobile.KeyPress += OnlyNumeric_KeyPress;

            // Form closing event
            this.FormClosing += Form1_FormClosing;
        }

        // NEW: Camera capture method
        private void CapturePhotoFromCamera(PictureBox targetPictureBox)
        {
            try
            {
                if (!WebcamHandler.IsCameraAvailable())
                {
                    MessageBox.Show("No camera detected. Please connect a camera and try again.\n\nYou can still use the '📁 Upload' button to select an image file.",
                        "Camera Not Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (CameraCaptureForm cameraForm = new CameraCaptureForm())
                {
                    if (cameraForm.ShowDialog() == DialogResult.OK && cameraForm.CapturedImage != null)
                    {
                        // Dispose existing image to free memory
                        targetPictureBox.Image?.Dispose();

                        // Set the captured image
                        targetPictureBox.Image = new Bitmap(cameraForm.CapturedImage);

                        MessageBox.Show("📷 Photo captured successfully!",
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing photo: {ex.Message}\n\nPlease try again or use the '📁 Upload' button instead.",
                    "Camera Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Only allow digits
        private void OnlyNumeric_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                fingerprintHandler?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing fingerprint handler: {ex.Message}");
            }
        }

        private void CaptureFingerprint(PictureBox targetPictureBox)
        {
            if (fingerprintHandler == null || !fingerprintHandler.IsDeviceReady())
            {
                MessageBox.Show("Fingerprint device is not available. Please use the 'Upload' button to load a fingerprint image file.",
                    "Device Not Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                fingerprintHandler.CaptureFingerprint(targetPictureBox);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing fingerprint: {ex.Message}",
                    "Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UploadImageToPictureBox(PictureBox targetPictureBox)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        targetPictureBox.Image = System.Drawing.Image.FromFile(openFileDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ButtonGeneratePdf_Click(object sender, EventArgs e)
        {
            try
            {
                // Validation
                if (!ValidateRequiredFields())
                {
                    MessageBox.Show("Please fill all required fields (Name, Aadhaar Number, and Address) for at least one party.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get first party details for filename
                string firstName = GetTextSafely(textBoxParty1Name);
                string firstAadhar = GetTextSafely(textBoxParty1Aadhar);
                string fileName = $"{firstName}_{firstAadhar}.pdf";
                // Remove invalid filename characters
                fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

                saveFileDialogPdf.Filter = "PDF Files|*.pdf";
                saveFileDialogPdf.DefaultExt = "pdf";
                saveFileDialogPdf.FileName = fileName;

                if (saveFileDialogPdf.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialogPdf.FileName;
                    GeneratePdfDocument(filePath);
                    MessageBox.Show("PDF saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateRequiredFields()
        {
            // Check if at least one party has all required fields filled
            return HasRequiredFields(textBoxParty1Name, textBoxParty1Aadhar, textBoxParty1Address) ||
                   HasRequiredFields(textBoxParty2Name, textBoxParty2Aadhar, textBoxParty2Address) ||
                   HasRequiredFields(textBoxParty3Name, textBoxParty3Aadhar, textBoxParty3Address) ||
                   HasRequiredFields(textBoxParty4Name, textBoxParty4Aadhar, textBoxParty4Address) ||
                   HasRequiredFields(textBoxParty5Name, textBoxParty5Aadhar, textBoxParty5Address) ||
                   HasRequiredFields(textBoxParty6Name, textBoxParty6Aadhar, textBoxParty6Address);
        }

        private bool HasRequiredFields(TextBox nameBox, TextBox aadharBox, TextBox addressBox)
        {
            return !string.IsNullOrWhiteSpace(GetTextSafely(nameBox)) &&
                   !string.IsNullOrWhiteSpace(GetTextSafely(aadharBox)) &&
                   !string.IsNullOrWhiteSpace(GetTextSafely(addressBox));
        }

        private void GeneratePdfDocument(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                Document document = new Document(PageSize.A4, 25, 25, 30, 60);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);
                writer.PageEvent = new PdfFooterEvent(GetTextSafely(textBoxDocumentNumber));
                document.Open();

                // Title
                string agreementType = comboBoxAgreementType.SelectedItem?.ToString() ?? "Agreement";
                iTextSharp.text.Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                Paragraph title = new Paragraph(agreementType.ToUpper(), titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                document.Add(title);
                document.Add(Chunk.NEWLINE);

                // Create table with 3 columns
                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 5f, 2f, 2f }); // Party Details, Photo, Fingerprint

                // Header row
                iTextSharp.text.Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                BaseColor headerColor = new BaseColor(224, 224, 224);

                PdfPCell headerCell1 = new PdfPCell(new Phrase("Party Details", headerFont));
                headerCell1.HorizontalAlignment = Element.ALIGN_CENTER;
                headerCell1.BackgroundColor = headerColor;
                headerCell1.Padding = 8;

                PdfPCell headerCell2 = new PdfPCell(new Phrase("Photo", headerFont));
                headerCell2.HorizontalAlignment = Element.ALIGN_CENTER;
                headerCell2.BackgroundColor = headerColor;
                headerCell2.Padding = 8;

                PdfPCell headerCell3 = new PdfPCell(new Phrase("Fingerprint", headerFont));
                headerCell3.HorizontalAlignment = Element.ALIGN_CENTER;
                headerCell3.BackgroundColor = headerColor;
                headerCell3.Padding = 8;

                table.AddCell(headerCell1);
                table.AddCell(headerCell2);
                table.AddCell(headerCell3);

                // Add party rows
                AddPartyRowToPdf(table, "PARTY 1", textBoxParty1Name, textBoxParty1Aadhar, textBoxParty1Address,
                    textBoxParty1Pan, textBoxParty1Mobile, pictureBoxParty1Image.Image, pictureBoxParty1Fingerprint.Image);
                AddPartyRowToPdf(table, "PARTY 2", textBoxParty2Name, textBoxParty2Aadhar, textBoxParty2Address,
                    textBoxParty2Pan, textBoxParty2Mobile, pictureBoxParty2Image.Image, pictureBoxParty2Fingerprint.Image);
                AddPartyRowToPdf(table, "PARTY 3", textBoxParty3Name, textBoxParty3Aadhar, textBoxParty3Address,
                    textBoxParty3Pan, textBoxParty3Mobile, pictureBoxParty3Image.Image, pictureBoxParty3Fingerprint.Image);
                AddPartyRowToPdf(table, "PARTY 4", textBoxParty4Name, textBoxParty4Aadhar, textBoxParty4Address,
                    textBoxParty4Pan, textBoxParty4Mobile, pictureBoxParty4Image.Image, pictureBoxParty4Fingerprint.Image);
                AddPartyRowToPdf(table, "PARTY 5", textBoxParty5Name, textBoxParty5Aadhar, textBoxParty5Address,
                    textBoxParty5Pan, textBoxParty5Mobile, pictureBoxParty5Image.Image, pictureBoxParty5Fingerprint.Image);
                AddPartyRowToPdf(table, "PARTY 6", textBoxParty6Name, textBoxParty6Aadhar, textBoxParty6Address,
                    textBoxParty6Pan, textBoxParty6Mobile, pictureBoxParty6Image.Image, pictureBoxParty6Fingerprint.Image);

                document.Add(table);
                document.Add(Chunk.NEWLINE);

                // Add admission paragraph
                iTextSharp.text.Font boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                iTextSharp.text.Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                Paragraph admissionTitle = new Paragraph("Declaration of Execution and Consent", boldFont);
                admissionTitle.Alignment = Element.ALIGN_CENTER;
                document.Add(admissionTitle);
                document.Add(Chunk.NEWLINE);

                string agreementTypeForText = agreementType.Replace(" Agreement", "").ToLower();
                string admissionText = $"The undersigned parties hereby admit that they have executed this Document of their own free will. The Identifiers confirm that they are well acquainted with the said parties. Furthermore, the parties have given their full and informed consent to Singh Enterprises Legal to collect and use their Aadhaar Number, Name, and Fingerprint for the purposes related to this Document. They acknowledge that they are fully aware of the nature, contents, and implications of this Document.";

                Paragraph admissionPara = new Paragraph(admissionText, normalFont);
                admissionPara.Alignment = Element.ALIGN_JUSTIFIED;
                document.Add(admissionPara);

                document.Close();
            }
        }

        private void AddPartyRowToPdf(PdfPTable table, string partyLabel, TextBox nameBox, TextBox aadharBox,
            TextBox addressBox, TextBox panBox, TextBox mobileBox,
            System.Drawing.Image image, System.Drawing.Image fingerprint)
        {
            string name = GetTextSafely(nameBox);
            string aadhar = GetTextSafely(aadharBox);
            string pan = GetTextSafely(panBox);
            string mobile = GetTextSafely(mobileBox);
            string address = GetTextSafely(addressBox);

            // Skip empty parties
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(aadhar))
                return;

            iTextSharp.text.Font partyFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            iTextSharp.text.Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);

            // Party details cell
            Phrase partyPhrase = new Phrase();
            partyPhrase.Add(new Chunk($"{partyLabel}\n", partyFont));
            partyPhrase.Add(new Chunk($"Name: {name}\n", normalFont));
            partyPhrase.Add(new Chunk($"Aadhaar: {aadhar}\n", normalFont));
            if (!string.IsNullOrWhiteSpace(pan))
                partyPhrase.Add(new Chunk($"PAN: {pan}\n", normalFont));
            if (!string.IsNullOrWhiteSpace(mobile))
                partyPhrase.Add(new Chunk($"Mobile: {mobile}\n", normalFont));
            partyPhrase.Add(new Chunk($"Address: {address}", normalFont));

            PdfPCell partyCell = new PdfPCell(partyPhrase);
            partyCell.VerticalAlignment = Element.ALIGN_TOP;
            partyCell.Padding = 6;
            table.AddCell(partyCell);

            // Photo cell
            if (image != null)
            {
                try
                {
                    using (var ms = new MemoryStream())
                    {
                        image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(ms.ToArray());
                        pdfImage.ScaleToFit(80, 100);
                        PdfPCell imgCell = new PdfPCell(pdfImage, true);
                        imgCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        imgCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        imgCell.Padding = 5;
                        table.AddCell(imgCell);
                    }
                }
                catch (Exception)
                {
                    PdfPCell errorCell = new PdfPCell(new Phrase("Image Error", normalFont));
                    errorCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    errorCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    errorCell.Padding = 5;
                    table.AddCell(errorCell);
                }
            }
            else
            {
                PdfPCell emptyCell = new PdfPCell(new Phrase("No Photo", normalFont));
                emptyCell.HorizontalAlignment = Element.ALIGN_CENTER;
                emptyCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                emptyCell.Padding = 5;
                emptyCell.BackgroundColor = new BaseColor(250, 250, 250);
                table.AddCell(emptyCell);
            }

            // Fingerprint cell
            if (fingerprint != null)
            {
                try
                {
                    using (var ms = new MemoryStream())
                    {
                        fingerprint.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(ms.ToArray());
                        pdfImage.ScaleToFit(80, 100);
                        PdfPCell imgCell = new PdfPCell(pdfImage, true);
                        imgCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        imgCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        imgCell.Padding = 5;
                        table.AddCell(imgCell);
                    }
                }
                catch (Exception)
                {
                    PdfPCell errorCell = new PdfPCell(new Phrase("Print Error", normalFont));
                    errorCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    errorCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    errorCell.Padding = 5;
                    table.AddCell(errorCell);
                }
            }
            else
            {
                PdfPCell emptyCell = new PdfPCell(new Phrase("No Print", normalFont));
                emptyCell.HorizontalAlignment = Element.ALIGN_CENTER;
                emptyCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                emptyCell.Padding = 5;
                emptyCell.BackgroundColor = new BaseColor(250, 250, 250);
                table.AddCell(emptyCell);
            }
        }

        private string GetTextSafely(TextBox textBox)
        {
            return textBox?.Text ?? "";
        }
    }

    // PDF Footer Event Handler
    public class PdfFooterEvent : PdfPageEventHelper
    {
        private readonly string docNumber;

        public PdfFooterEvent(string documentNumber)
        {
            docNumber = documentNumber;
        }

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            base.OnEndPage(writer, document);
            if (!string.IsNullOrWhiteSpace(docNumber))
            {
                PdfContentByte cb = writer.DirectContent;
                cb.BeginText();
                cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8);
                cb.SetTextMatrix(document.LeftMargin, document.BottomMargin - 20);
                cb.ShowText($"Registered as Document No. {docNumber} at the Joint S.R. Kurla 4 on date {DateTime.Now:dd/MM/yyyy}");
                cb.EndText();
            }
        }
    }
}
