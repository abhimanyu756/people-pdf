using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace people_pdf
{
    public class FingerprintHandler : IDisposable
    {
        private object m_FPM;
        private bool m_bInit = false;
        private bool m_bSecuGenDeviceOpened = false;
        private Int32 m_ImageWidth = 300;
        private Int32 m_ImageHeight = 400;
        private Int32 m_ImageDPI = 500;
        private Int32 mMaxTemplateSize = 1024;
        private Byte[] m_RegMin;
        private Byte[] m_VrfMin;
        private Assembly secuGenAssembly = null;

        public FingerprintHandler()
        {
            m_bInit = false;
            m_bSecuGenDeviceOpened = false;
        }

        public bool InitializeDevice()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Starting fingerprint device initialization ===");
                System.Diagnostics.Debug.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");

                // STEP 1: Load the SecuGen assembly
                string[] possiblePaths = new string[]
                {
                    // Try .NET 6+ version first
                    Path.Combine(Directory.GetCurrentDirectory(), "SecuGen.FDxSDKPro.DotNet.Windows.dll"),
                    Path.Combine(Directory.GetCurrentDirectory(), "lib", "SecuGen.FDxSDKPro.DotNet.Windows.dll"),
                    // Try .NET Framework version
                    Path.Combine(Directory.GetCurrentDirectory(), "SecuGen.FDxSDKPro.Windows.dll"),
                    Path.Combine(Directory.GetCurrentDirectory(), "lib", "SecuGen.FDxSDKPro.Windows.dll"),
                };

                foreach (string path in possiblePaths)
                {
                    System.Diagnostics.Debug.WriteLine($"Checking: {path}");
                    if (File.Exists(path))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found DLL at: {path}");
                        try
                        {
                            secuGenAssembly = Assembly.LoadFrom(path);
                            System.Diagnostics.Debug.WriteLine($"Loaded: {secuGenAssembly.FullName}");
                            break;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load: {ex.Message}");
                        }
                    }
                }

                if (secuGenAssembly == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: SecuGen DLL not found");
                    return false;
                }

                // STEP 2: Find SGFingerPrintManager class (correct name from PDF)
                System.Diagnostics.Debug.WriteLine("\nLooking for SGFingerPrintManager class...");
                Type fpManagerType = null;

                // According to PDF: namespace is SecuGen.FDxSDKPro.Windows
                // Try both .NET 6+ and .NET Framework namespaces
                string[] possibleTypeNames = new string[]
                {
                    "SecuGen.FDxSDKPro.DotNet.Windows.SGFingerPrintManager",
                    "SecuGen.FDxSDKPro.Windows.SGFingerPrintManager"
                };

                foreach (string typeName in possibleTypeNames)
                {
                    fpManagerType = secuGenAssembly.GetType(typeName);
                    if (fpManagerType != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found: {typeName}");
                        break;
                    }
                }

                if (fpManagerType == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: SGFingerPrintManager not found");
                    System.Diagnostics.Debug.WriteLine("Available types:");
                    foreach (Type t in secuGenAssembly.GetTypes())
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {t.FullName}");
                    }
                    return false;
                }

                // STEP 3: Create instance - PDF shows constructor with no parameters
                System.Diagnostics.Debug.WriteLine("\nCreating SGFingerPrintManager instance...");
                m_FPM = Activator.CreateInstance(fpManagerType);
                if (m_FPM == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Failed to create instance");
                    return false;
                }
                System.Diagnostics.Debug.WriteLine("Instance created successfully");

                // STEP 4: Find SGFPMDeviceName enum
                System.Diagnostics.Debug.WriteLine("\nLooking for SGFPMDeviceName enum...");
                Type deviceNameEnumType = null;
                string[] possibleEnumNames = new string[]
                {
                    "SecuGen.FDxSDKPro.DotNet.Windows.SGFPMDeviceName",
                    "SecuGen.FDxSDKPro.Windows.SGFPMDeviceName"
                };

                foreach (string enumName in possibleEnumNames)
                {
                    deviceNameEnumType = secuGenAssembly.GetType(enumName);
                    if (deviceNameEnumType != null && deviceNameEnumType.IsEnum)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found enum: {enumName}");
                        break;
                    }
                }

                if (deviceNameEnumType == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: SGFPMDeviceName enum not found");
                    return false;
                }

                // List available device names from PDF
                System.Diagnostics.Debug.WriteLine("Available device names:");
                foreach (var value in Enum.GetValues(deviceNameEnumType))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {value} = {Convert.ToInt32(value)}");
                }

                // STEP 5: Call Init(SGFPMDeviceName) - According to PDF section 2.2
                System.Diagnostics.Debug.WriteLine("\nCalling Init method...");
                MethodInfo initMethod = fpManagerType.GetMethod("Init", new Type[] { deviceNameEnumType });

                if (initMethod == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Init method not found");
                    return false;
                }

                // Use DEV_FDU05 (U20 device) or DEV_AUTO if available
                // From PDF: DEV_FDU05 = 6, but let's try to find DEV_AUTO first
                object devValue = null;
                string[] devicePriority = new string[] { "DEV_AUTO", "DEV_FDU05", "DEV_FDU08", "DEV_FDU07" };

                foreach (string devName in devicePriority)
                {
                    try
                    {
                        devValue = Enum.Parse(deviceNameEnumType, devName);
                        System.Diagnostics.Debug.WriteLine($"Using device: {devName} = {devValue}");
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (devValue == null)
                {
                    // Fallback to first available value
                    var values = Enum.GetValues(deviceNameEnumType);
                    if (values.Length > 0)
                    {
                        devValue = values.GetValue(0);
                        System.Diagnostics.Debug.WriteLine($"Using fallback device: {devValue}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ERROR: No device values available");
                        return false;
                    }
                }

                var initResult = initMethod.Invoke(m_FPM, new object[] { devValue });
                int errorCode = Convert.ToInt32(initResult);
                System.Diagnostics.Debug.WriteLine($"Init result: {errorCode} ({GetErrorDescription(errorCode)})");

                // According to PDF: ERROR_NONE = 0
                if (errorCode != 0)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Init failed");
                    return false;
                }

                m_bInit = true;
                System.Diagnostics.Debug.WriteLine("Init successful");

                // STEP 6: Call OpenDevice(Int32) - According to PDF section 2.3
                System.Diagnostics.Debug.WriteLine("\nCalling OpenDevice...");
                MethodInfo openDeviceMethod = fpManagerType.GetMethod("OpenDevice", new Type[] { typeof(Int32) });

                if (openDeviceMethod == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: OpenDevice method not found");
                    return false;
                }

                // From PDF: USB_AUTO_DETECT = 0x255 (255)
                var openResult = openDeviceMethod.Invoke(m_FPM, new object[] { 0x255 });
                int openError = Convert.ToInt32(openResult);
                System.Diagnostics.Debug.WriteLine($"OpenDevice result: {openError} ({GetErrorDescription(openError)})");

                if (openError != 0)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: OpenDevice failed");
                    return false;
                }

                m_bSecuGenDeviceOpened = true;
                System.Diagnostics.Debug.WriteLine("Device opened successfully");

                // STEP 7: Get device info - According to PDF section 2.4
                System.Diagnostics.Debug.WriteLine("\nGetting device info...");

                // Find SGFPMDeviceInfoParam structure
                Type deviceInfoType = null;
                string[] possibleDeviceInfoNames = new string[]
                {
                    "SecuGen.FDxSDKPro.DotNet.Windows.SGFPMDeviceInfoParam",
                    "SecuGen.FDxSDKPro.Windows.SGFPMDeviceInfoParam"
                };

                foreach (string infoName in possibleDeviceInfoNames)
                {
                    deviceInfoType = secuGenAssembly.GetType(infoName);
                    if (deviceInfoType != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found device info type: {infoName}");
                        break;
                    }
                }

                if (deviceInfoType != null)
                {
                    MethodInfo getDeviceInfoMethod = fpManagerType.GetMethod("GetDeviceInfo");
                    if (getDeviceInfoMethod != null)
                    {
                        var deviceInfo = Activator.CreateInstance(deviceInfoType);
                        object[] infoParams = new object[] { deviceInfo };
                        var infoResult = getDeviceInfoMethod.Invoke(m_FPM, infoParams);
                        int infoError = Convert.ToInt32(infoResult);

                        System.Diagnostics.Debug.WriteLine($"GetDeviceInfo result: {infoError}");

                        if (infoError == 0)
                        {
                            var updatedDeviceInfo = infoParams[0];

                            // From PDF section 3.2: SGFPMDeviceInfoParam has fields ImageWidth, ImageHeight, ImageDPI
                            var widthField = deviceInfoType.GetField("ImageWidth");
                            var heightField = deviceInfoType.GetField("ImageHeight");
                            var dpiField = deviceInfoType.GetField("ImageDPI");

                            if (widthField != null) m_ImageWidth = (int)widthField.GetValue(updatedDeviceInfo);
                            if (heightField != null) m_ImageHeight = (int)heightField.GetValue(updatedDeviceInfo);
                            if (dpiField != null) m_ImageDPI = (int)dpiField.GetValue(updatedDeviceInfo);

                            System.Diagnostics.Debug.WriteLine($"Device info - Width: {m_ImageWidth}, Height: {m_ImageHeight}, DPI: {m_ImageDPI}");

                            // Initialize template arrays
                            m_RegMin = new Byte[mMaxTemplateSize];
                            m_VrfMin = new Byte[mMaxTemplateSize];
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("\n=== Fingerprint device fully initialized ===");
                return true;
            }
            catch (FileNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: File not found: {ex.FileName}");
                return false;
            }
            catch (BadImageFormatException ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: DLL architecture mismatch: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("Make sure you're using x86 build with x86 DLLs");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return false;
            }
        }

        private string GetErrorDescription(int errorCode)
        {
            // From PDF section 3.11: SGFPMError enumeration
            return errorCode switch
            {
                0 => "SUCCESS",
                1 => "CREATION_FAILED",
                2 => "FUNCTION_FAILED",
                3 => "INVALID_PARAM",
                5 => "DLLLOAD_FAILED",
                51 => "SYSLOAD_FAILED",
                52 => "INITIALIZE_FAILED",
                55 => "DEVICE_NOT_FOUND",
                58 => "LACK_OF_BANDWIDTH",
                59 => "DEV_ALREADY_OPEN",
                61 => "UNSUPPORTED_DEV",
                _ => $"Unknown({errorCode})"
            };
        }

        public bool CaptureFingerprint(PictureBox pictureBox)
        {
            if (!m_bInit || !m_bSecuGenDeviceOpened || m_FPM == null)
            {
                MessageBox.Show("Fingerprint scanner is not available.\n\nPlease use the 'Upload' button to load a fingerprint image file instead.",
                    "Scanner Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            try
            {
                using (Form captureForm = CreateCaptureForm())
                {
                    if (captureForm.ShowDialog() == DialogResult.OK)
                    {
                        var fpManagerType = m_FPM.GetType();

                        // According to PDF section 2.5: GetImage(Byte buffer[])
                        var getImageMethod = fpManagerType.GetMethod("GetImage", new Type[] { typeof(Byte[]) });

                        if (getImageMethod != null)
                        {
                            Byte[] fp_image = new Byte[m_ImageWidth * m_ImageHeight];
                            var result = getImageMethod.Invoke(m_FPM, new object[] { fp_image });
                            int errorCode = Convert.ToInt32(result);

                            if (errorCode != 0)
                            {
                                MessageBox.Show($"Failed to capture fingerprint.\n\nError: {GetErrorDescription(errorCode)}",
                                    "Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }

                            // According to PDF section 2.6: GetImageQuality
                            var getQualityMethod = fpManagerType.GetMethod("GetImageQuality");
                            int img_qlty = 100;

                            if (getQualityMethod != null)
                            {
                                object[] qualityParams = new object[] { m_ImageWidth, m_ImageHeight, fp_image, 0 };
                                var qualityResult = getQualityMethod.Invoke(m_FPM, qualityParams);
                                int qualityError = Convert.ToInt32(qualityResult);
                                if (qualityError == 0) img_qlty = (int)qualityParams[3];
                            }

                            // From PDF: Quality >= 50 recommended for registration
                            if (img_qlty < 50)
                            {
                                MessageBox.Show($"Fingerprint quality too low ({img_qlty}%).\n\nRecommended: 50% or higher\n\nPlease try again with better finger placement.",
                                    "Quality Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return false;
                            }

                            Bitmap bitmap = ConvertRawImageToBitmap(fp_image, m_ImageWidth, m_ImageHeight);
                            if (bitmap != null)
                            {
                                pictureBox.Image?.Dispose();
                                pictureBox.Image = bitmap;
                                MessageBox.Show($"Fingerprint captured successfully!\n\nQuality: {img_qlty}%",
                                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Text = "Place your finger on the scanner and click 'Capture'",
                Location = new Point(20, 30),
                Size = new Size(400, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(64, 64, 64)
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
            captureForm.Controls.Add(captureButton);
            captureForm.Controls.Add(cancelButton);

            return captureForm;
        }

        private Bitmap ConvertRawImageToBitmap(Byte[] rawImage, int width, int height)
        {
            try
            {
                // According to PDF section 2.5: 256 gray-level image
                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < 256; i++) palette.Entries[i] = Color.FromArgb(i, i, i);
                bitmap.Palette = palette;

                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                System.Runtime.InteropServices.Marshal.Copy(rawImage, 0, bmpData.Scan0, rawImage.Length);
                bitmap.UnlockBits(bmpData);
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bitmap conversion error: {ex.Message}");
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
                    fpManagerType.GetMethod("CloseDevice")?.Invoke(m_FPM, null);
                    m_bSecuGenDeviceOpened = false;
                }
                if (m_bInit && m_FPM != null)
                {
                    m_bInit = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Close error: {ex.Message}");
            }
        }

        public bool IsDeviceReady() => m_bInit && m_bSecuGenDeviceOpened;

        public void Dispose()
        {
            CloseDevice();
            m_FPM = null;
        }
    }
}