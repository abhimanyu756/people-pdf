using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace people_pdf
{
    public class FingerprintHandler : IDisposable
    {
        private object m_FPM; // Changed to object to avoid initialization issues
        private bool m_bInit = false;
        private bool m_bSecuGenDeviceOpened = false;
        private bool m_bSDKAvailable = false;
        private Int32 m_ImageWidth = 320; // Default values
        private Int32 m_ImageHeight = 240;
        private Int32 m_ImageDPI = 500;
        private Int32 mMaxTemplateSize = 1024;
        private Byte[] m_RegMin;
        private Byte[] m_VrfMin;

        public FingerprintHandler()
        {
            // Don't initialize SDK in constructor to avoid startup crashes
            m_bInit = false;
            m_bSecuGenDeviceOpened = false;
            m_bSDKAvailable = false;
        }

        public bool InitializeDevice()
        {
            try
            {
                // Try to check if SecuGen assemblies are available
                
                System.Diagnostics.Debug.WriteLine("Starting fingerprint device initialization...");
                System.Diagnostics.Debug.WriteLine($"Current directory: {System.IO.Directory.GetCurrentDirectory()}");

                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                bool secuGenAssemblyFound = false;

                foreach (var assembly in assemblies)
                {
                    System.Diagnostics.Debug.WriteLine($"Loaded assembly: {assembly.FullName}");
                    if (assembly.FullName.Contains("SecuGen"))
                    {
                        secuGenAssemblyFound = true;
                        System.Diagnostics.Debug.WriteLine("Found SecuGen assembly!");
                        break;
                    }
                }

                // Try to create SecuGen fingerprint manager using reflection to avoid compile-time dependency
                var secuGenType = Type.GetType("SecuGen.FDxSDKPro.Windows.SGFingerPrintManager, SecuGen.FDxSDKPro.Windows");
                if (secuGenType == null)
                {
                    System.Diagnostics.Debug.WriteLine("SecuGen SDK not found - fingerprint scanning disabled");
                    return false;
                }

                // Create instance using reflection
                m_FPM = Activator.CreateInstance(secuGenType);
                if (m_FPM == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to create SecuGen manager instance");
                    return false;
                }

                // Try to call Init method using reflection
                var initMethod = secuGenType.GetMethod("Init");
                if (initMethod == null)
                {
                    System.Diagnostics.Debug.WriteLine("SecuGen Init method not found");
                    return false;
                }

                // Get the device name enum value (DEV_AUTO = 0)
                var result = initMethod.Invoke(m_FPM, new object[] { 0 });
                int errorCode = Convert.ToInt32(result);

                if (errorCode != 0) // SGFPM_OK = 0
                {
                    System.Diagnostics.Debug.WriteLine($"SecuGen initialization failed with error: {errorCode}");
                    return false;
                }

                m_bInit = true;

                // Try to get device count
                var getDeviceCountMethod = secuGenType.GetMethod("GetDeviceCount");
                if (getDeviceCountMethod != null)
                {
                    object[] parameters = new object[] { 0 };
                    var deviceCountResult = getDeviceCountMethod.Invoke(m_FPM, parameters);
                    int deviceCountError = Convert.ToInt32(deviceCountResult);
                    int deviceCount = (int)parameters[0];

                    if (deviceCountError != 0 || deviceCount == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("No SecuGen devices found");
                        return false;
                    }

                    // Try to open first device
                    var openDeviceMethod = secuGenType.GetMethod("OpenDevice");
                    if (openDeviceMethod != null)
                    {
                        var openResult = openDeviceMethod.Invoke(m_FPM, new object[] { 0 });
                        int openError = Convert.ToInt32(openResult);

                        if (openError != 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to open SecuGen device: {openError}");
                            return false;
                        }

                        m_bSecuGenDeviceOpened = true;

                        // Try to get device info
                        var getDeviceInfoMethod = secuGenType.GetMethod("GetDeviceInfo");
                        if (getDeviceInfoMethod != null)
                        {
                            // Create device info structure
                            var deviceInfoType = Type.GetType("SecuGen.FDxSDKPro.Windows.SGFPMDeviceInfoParam, SecuGen.FDxSDKPro.Windows");
                            if (deviceInfoType != null)
                            {
                                var deviceInfo = Activator.CreateInstance(deviceInfoType);
                                object[] infoParams = new object[] { deviceInfo };
                                var infoResult = getDeviceInfoMethod.Invoke(m_FPM, infoParams);
                                int infoError = Convert.ToInt32(infoResult);

                                if (infoError == 0)
                                {
                                    // Get device info properties using reflection
                                    var updatedDeviceInfo = infoParams[0];
                                    var widthField = deviceInfoType.GetField("ImageWidth");
                                    var heightField = deviceInfoType.GetField("ImageHeight");
                                    var dpiField = deviceInfoType.GetField("ImageDPI");
                                    var templateSizeField = deviceInfoType.GetField("MaxTemplateSize");

                                    if (widthField != null) m_ImageWidth = (int)widthField.GetValue(updatedDeviceInfo);
                                    if (heightField != null) m_ImageHeight = (int)heightField.GetValue(updatedDeviceInfo);
                                    if (dpiField != null) m_ImageDPI = (int)dpiField.GetValue(updatedDeviceInfo);
                                    if (templateSizeField != null) mMaxTemplateSize = (int)templateSizeField.GetValue(updatedDeviceInfo);

                                    // Initialize template arrays
                                    m_RegMin = new Byte[mMaxTemplateSize];
                                    m_VrfMin = new Byte[mMaxTemplateSize];
                                }
                            }
                        }

                        m_bSDKAvailable = true;
                        System.Diagnostics.Debug.WriteLine("SecuGen device initialized successfully");
                        return true;
                    }
                }

                return false;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecuGen DLL not found: {ex.Message}");
                return false;
            }
            catch (System.BadImageFormatException ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecuGen DLL architecture mismatch: {ex.Message}");
                return false;
            }
            catch (System.TypeLoadException ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecuGen SDK types not available: {ex.Message}");
                return false;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecuGen SDK invocation error: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General SecuGen initialization error: {ex.Message}");
                return false;
            }
        }

        public bool CaptureFingerprint(PictureBox pictureBox)
        {
            if (!m_bInit || !m_bSecuGenDeviceOpened || !m_bSDKAvailable || m_FPM == null)
            {
                MessageBox.Show("Fingerprint scanner is not available.\n\nPlease use the 'Upload' button to load a fingerprint image file instead.",
                    "Scanner Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            try
            {
                // Show capture dialog
                using (Form captureForm = CreateCaptureForm())
                {
                    if (captureForm.ShowDialog() == DialogResult.OK)
                    {
                        // Try to capture using reflection
                        var fpManagerType = m_FPM.GetType();
                        var getImageMethod = fpManagerType.GetMethod("GetImage");

                        if (getImageMethod != null)
                        {
                            // Create image buffer
                            Byte[] fp_image = new Byte[m_ImageWidth * m_ImageHeight];

                            var result = getImageMethod.Invoke(m_FPM, new object[] { fp_image });
                            int errorCode = Convert.ToInt32(result);

                            if (errorCode != 0) // SGFPM_OK = 0
                            {
                                MessageBox.Show($"Failed to capture fingerprint image. Error: {errorCode}",
                                    "Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }

                            // Try to get image quality
                            var getQualityMethod = fpManagerType.GetMethod("GetImageQuality");
                            int img_qlty = 100; // Default quality

                            if (getQualityMethod != null)
                            {
                                object[] qualityParams = new object[] { m_ImageWidth, m_ImageHeight, fp_image, 0 };
                                var qualityResult = getQualityMethod.Invoke(m_FPM, qualityParams);
                                int qualityError = Convert.ToInt32(qualityResult);

                                if (qualityError == 0)
                                {
                                    img_qlty = (int)qualityParams[3];
                                }
                            }

                            // Check quality threshold
                            if (img_qlty < 50)
                            {
                                MessageBox.Show($"Fingerprint quality too low ({img_qlty}%). Please try again.",
                                    "Quality Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return false;
                            }

                            // Convert to bitmap and display
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
                        else
                        {
                            MessageBox.Show("SecuGen GetImage method not available.",
                                "Method Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Size = new Size(450, 250),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label instructionLabel = new Label
            {
                Text = "Please place your finger on the scanner and click 'Capture Fingerprint'",
                Location = new Point(20, 30),
                Size = new Size(400, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            Label statusLabel = new Label
            {
                Text = "Scanner Status: Ready",
                Location = new Point(20, 80),
                Size = new Size(400, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Green
            };

            Button captureButton = new Button
            {
                Text = "Capture Fingerprint",
                Location = new Point(120, 120),
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
                Location = new Point(280, 120),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(128, 128, 128),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                DialogResult = DialogResult.Cancel
            };
            cancelButton.FlatAppearance.BorderSize = 0;

            captureForm.Controls.Add(instructionLabel);
            captureForm.Controls.Add(statusLabel);
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
                System.Diagnostics.Debug.WriteLine($"Error converting fingerprint image: {ex.Message}");
                return null;
            }
        }

        public void CloseDevice()
        {
            try
            {
                if (m_bSecuGenDeviceOpened && m_FPM != null)
                {
                    var fpManagerType = m_FPM.GetType();
                    var closeDeviceMethod = fpManagerType.GetMethod("CloseDevice");
                    closeDeviceMethod?.Invoke(m_FPM, null);
                    m_bSecuGenDeviceOpened = false;
                }

                if (m_bInit && m_FPM != null)
                {
                    var fpManagerType = m_FPM.GetType();
                    var closeMethod = fpManagerType.GetMethod("Close");
                    closeMethod?.Invoke(m_FPM, null);
                    m_bInit = false;
                }

                m_bSDKAvailable = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing fingerprint device: {ex.Message}");
            }
        }

        public bool IsDeviceReady()
        {
            return m_bInit && m_bSecuGenDeviceOpened && m_bSDKAvailable;
        }

        public void Dispose()
        {
            CloseDevice();

            try
            {
                if (m_FPM != null)
                {
                    var fpManagerType = m_FPM.GetType();
                    var disposeMethod = fpManagerType.GetMethod("Dispose");
                    disposeMethod?.Invoke(m_FPM, null);
                    m_FPM = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing fingerprint manager: {ex.Message}");
            }
        }
    }
}
