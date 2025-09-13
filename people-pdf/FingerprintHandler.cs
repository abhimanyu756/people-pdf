using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using SecuGen.FDxSDKPro.Windows;

namespace people_pdf
{
    public class FingerprintHandler : IDisposable
    {
        private SGFingerPrintManager m_FPM;
        private bool m_bInit;
        private bool m_bSecuGenDeviceOpened;
        private Int32 m_ImageWidth;
        private Int32 m_ImageHeight;
        private Int32 m_ImageDPI;
        private Int32 mMaxTemplateSize;
        private Byte[] m_RegMin;
        private Byte[] m_VrfMin;

        public FingerprintHandler()
        {
            m_FPM = new SGFingerPrintManager();
            m_bInit = false;
            m_bSecuGenDeviceOpened = false;
        }

        public bool InitializeDevice()
        {
            try
            {
                // Initialize the fingerprint manager
                SGFPMError err = (SGFPMError)m_FPM.Init((SecuGen.FDxSDKPro.Windows.SGFPMDeviceName)SGFPMDeviceName.DEV_AUTO);
                if (err != SGFPMError.SGFPM_OK)
                {
                    MessageBox.Show($"Failed to initialize fingerprint device. Error: {err}",
                                  "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                m_bInit = true;

                // Get device count
                Int32 ndevices = 0;
                err = m_FPM.GetDeviceCount(ref ndevices);
                if (err != SGFPMError.SGFPM_OK || ndevices == 0)
                {
                    MessageBox.Show("No SecuGen fingerprint devices found.",
                                  "Device Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                // Open the first device
                err = m_FPM.OpenDevice(0);
                if (err != SGFPMError.SGFPM_OK)
                {
                    MessageBox.Show($"Failed to open fingerprint device. Error: {err}",
                                  "Device Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                m_bSecuGenDeviceOpened = true;

                // Get device info
                SGFPMDeviceInfoParam deviceInfo = new SGFPMDeviceInfoParam();
                err = m_FPM.GetDeviceInfo(ref deviceInfo);
                if (err == SGFPMError.SGFPM_OK)
                {
                    m_ImageWidth = deviceInfo.ImageWidth;
                    m_ImageHeight = deviceInfo.ImageHeight;
                    m_ImageDPI = deviceInfo.ImageDPI;
                    mMaxTemplateSize = deviceInfo.MaxTemplateSize;

                    // Initialize template arrays
                    m_RegMin = new Byte[mMaxTemplateSize];
                    m_VrfMin = new Byte[mMaxTemplateSize];
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing fingerprint device: {ex.Message}",
                              "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool CaptureFingerprint(PictureBox pictureBox)
        {
            if (!m_bInit || !m_bSecuGenDeviceOpened)
            {
                MessageBox.Show("Fingerprint device not initialized or opened.",
                              "Device Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                // Create image buffer
                Byte[] fp_image = new Byte[m_ImageWidth * m_ImageHeight];
                Int32 img_qlty = 0;
                Int32 minutiae_count = 0;

                // Show capture dialog
                using (Form captureForm = CreateCaptureForm())
                {
                    if (captureForm.ShowDialog() == DialogResult.OK)
                    {
                        // Capture fingerprint image
                        SGFPMError err = m_FPM.GetImage(fp_image);
                        if (err != SGFPMError.SGFPM_OK)
                        {
                            MessageBox.Show($"Failed to capture fingerprint image. Error: {err}",
                                          "Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }

                        // Get image quality
                        err = m_FPM.GetImageQuality(m_ImageWidth, m_ImageHeight, fp_image, ref img_qlty);
                        if (err != SGFPMError.SGFPM_OK)
                        {
                            MessageBox.Show("Failed to get image quality.",
                                          "Quality Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        // Check if quality is acceptable (you can adjust this threshold)
                        if (img_qlty < 50) // Quality threshold (0-100)
                        {
                            MessageBox.Show($"Fingerprint quality too low ({img_qlty}). Please try again.",
                                          "Quality Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }

                        // Convert to bitmap and display in PictureBox
                        Bitmap bitmap = ConvertRawImageToBitmap(fp_image, m_ImageWidth, m_ImageHeight);
                        if (bitmap != null)
                        {
                            pictureBox.Image = bitmap;
                            MessageBox.Show($"Fingerprint captured successfully! Quality: {img_qlty}%",
                                          "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return true;
                        }
                        else
                        {
                            MessageBox.Show("Failed to convert fingerprint image.",
                                          "Conversion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing fingerprint: {ex.Message}",
                              "Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        private Form CreateCaptureForm()
        {
            Form captureForm = new Form
            {
                Text = "Fingerprint Capture",
                Size = new Size(400, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label instructionLabel = new Label
            {
                Text = "Please place your finger on the scanner and click 'Capture'",
                Location = new Point(20, 30),
                Size = new Size(350, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            Button captureButton = new Button
            {
                Text = "Capture Fingerprint",
                Location = new Point(100, 80),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            captureButton.FlatAppearance.BorderSize = 0;

            Button cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(260, 80),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(128, 128, 128),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                DialogResult = DialogResult.Cancel
            };
            cancelButton.FlatAppearance.BorderSize = 0;

            captureForm.Controls.Add(instructionLabel);
            captureForm.Controls.Add(captureButton);
            captureForm.Controls.Add(cancelButton);

            return captureForm;
        }

        private Bitmap ConvertRawImageToBitmap(Byte[] rawImage, int width, int height)
        {
            try
            {
                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

                // Create grayscale palette
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bitmap.Palette = palette;

                // Lock bitmap data
                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                                   ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                // Copy raw data to bitmap
                System.Runtime.InteropServices.Marshal.Copy(rawImage, 0, bmpData.Scan0, rawImage.Length);

                // Unlock bitmap
                bitmap.UnlockBits(bmpData);

                return bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error converting image: {ex.Message}",
                              "Conversion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public void CloseDevice()
        {
            try
            {
                if (m_bSecuGenDeviceOpened)
                {
                    m_FPM.CloseDevice();
                    m_bSecuGenDeviceOpened = false;
                }

                if (m_bInit)
                {
                    m_FPM.Close();
                    m_bInit = false;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't show message box during cleanup
                System.Diagnostics.Debug.WriteLine($"Error closing fingerprint device: {ex.Message}");
            }
        }

        public bool IsDeviceReady()
        {
            return m_bInit && m_bSecuGenDeviceOpened;
        }

        public void Dispose()
        {
            CloseDevice();
            m_FPM?.Dispose();
        }
    }
}