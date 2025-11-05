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
        private Type fpManagerType = null;

        public FingerprintHandler()
        {
            System.Diagnostics.Debug.WriteLine("*** FingerprintHandler constructor called ***");
            m_bInit = false;
            m_bSecuGenDeviceOpened = false;
        }

        public bool InitializeDevice()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=====================================");
                System.Diagnostics.Debug.WriteLine("=== FINGERPRINT INITIALIZATION v3 ===");
                System.Diagnostics.Debug.WriteLine("=====================================");
                System.Diagnostics.Debug.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
                System.Diagnostics.Debug.WriteLine($"Running as: {(Environment.Is64BitProcess ? "x64 (64-bit)" : "x86 (32-bit)")}");

                // STEP 1: Load the SecuGen assembly
                string[] possiblePaths = new string[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "SecuGen.FDxSDKPro.DotNet.Windows.dll"),
                    Path.Combine(Directory.GetCurrentDirectory(), "lib", "SecuGen.FDxSDKPro.DotNet.Windows.dll"),
                };

                System.Diagnostics.Debug.WriteLine("\n=== Loading SecuGen DLL ===");
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found at: {path}");
                        try
                        {
                            secuGenAssembly = Assembly.LoadFrom(path);
                            System.Diagnostics.Debug.WriteLine($"✓ Loaded: {secuGenAssembly.FullName}");
                            break;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"✗ Load failed: {ex.Message}");
                        }
                    }
                }

                if (secuGenAssembly == null)
                {
                    System.Diagnostics.Debug.WriteLine("✗ SecuGen DLL not found");
                    return false;
                }

                // STEP 2: Find SGFingerPrintManager class
                System.Diagnostics.Debug.WriteLine("\n=== Finding SGFingerPrintManager ===");
                string[] possibleTypeNames = new string[]
                {
                    "SecuGen.FDxSDKPro.Windows.SGFingerPrintManager",
                    "SecuGen.FDxSDKPro.DotNet.Windows.SGFingerPrintManager"
                };

                foreach (string typeName in possibleTypeNames)
                {
                    fpManagerType = secuGenAssembly.GetType(typeName);
                    if (fpManagerType != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ Found: {typeName}");
                        break;
                    }
                }

                if (fpManagerType == null)
                {
                    System.Diagnostics.Debug.WriteLine("✗ SGFingerPrintManager not found");
                    return false;
                }

                // STEP 3: Create instance
                System.Diagnostics.Debug.WriteLine("\n=== Creating Instance ===");
                try
                {
                    m_FPM = Activator.CreateInstance(fpManagerType);
                    System.Diagnostics.Debug.WriteLine("✓ Instance created");
                }
                catch (System.Reflection.TargetInvocationException tie)
                {
                    System.Diagnostics.Debug.WriteLine("✗ Constructor threw exception:");
                    System.Diagnostics.Debug.WriteLine($"   Type: {tie.InnerException?.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"   Message: {tie.InnerException?.Message}");

                    // Common issues:
                    if (tie.InnerException?.Message.Contains("sgfplib") == true)
                    {
                        System.Diagnostics.Debug.WriteLine("\n   → Native DLL (sgfplib.dll) not found or wrong architecture");
                    }
                    else if (tie.InnerException?.Message.Contains("driver") == true)
                    {
                        System.Diagnostics.Debug.WriteLine("\n   → SecuGen USB driver not installed");
                    }

                    return false;
                }

                if (m_FPM == null)
                {
                    System.Diagnostics.Debug.WriteLine("✗ Instance is null");
                    return false;
                }

                // STEP 4: Find device name enum
                System.Diagnostics.Debug.WriteLine("\n=== Finding Device Enum ===");
                Type deviceNameEnumType = null;
                string[] possibleEnumNames = new string[]
                {
                    "SecuGen.FDxSDKPro.Windows.SGFPMDeviceName",
                    "SecuGen.FDxSDKPro.DotNet.Windows.SGFPMDeviceName"
                };

                foreach (string enumName in possibleEnumNames)
                {
                    deviceNameEnumType = secuGenAssembly.GetType(enumName);
                    if (deviceNameEnumType != null && deviceNameEnumType.IsEnum)
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ Found: {enumName}");
                        break;
                    }
                }

                if (deviceNameEnumType == null)
                {
                    System.Diagnostics.Debug.WriteLine("✗ Device enum not found");
                    return false;
                }

                // STEP 5: Call Init
                System.Diagnostics.Debug.WriteLine("\n=== Calling Init ===");
                MethodInfo initMethod = fpManagerType.GetMethod("Init", new Type[] { deviceNameEnumType });

                if (initMethod == null)
                {
                    System.Diagnostics.Debug.WriteLine("✗ Init method not found");
                    return false;
                }

                // Get device value
                object devValue = null;
                string[] devicePriority = new string[] { "DEV_AUTO", "DEV_FDU05" };

                foreach (string devName in devicePriority)
                {
                    try
                    {
                        devValue = Enum.Parse(deviceNameEnumType, devName);
                        System.Diagnostics.Debug.WriteLine($"Using device: {devName}");
                        break;
                    }
                    catch { }
                }

                if (devValue == null)
                {
                    var values = Enum.GetValues(deviceNameEnumType);
                    devValue = values.Length > 0 ? values.GetValue(0) : null;
                }

                if (devValue == null)
                {
                    System.Diagnostics.Debug.WriteLine("✗ No device enum value available");
                    return false;
                }

                try
                {
                    var initResult = initMethod.Invoke(m_FPM, new object[] { devValue });
                    int errorCode = Convert.ToInt32(initResult);
                    System.Diagnostics.Debug.WriteLine($"Init result: {errorCode} ({GetErrorDescription(errorCode)})");

                    if (errorCode != 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"✗ Init failed: {GetErrorDescription(errorCode)}");

                        if (errorCode == 55) // DEVICE_NOT_FOUND
                        {
                            System.Diagnostics.Debug.WriteLine("   → No SecuGen device connected to USB");
                        }
                        else if (errorCode == 5) // DLLLOAD_FAILED
                        {
                            System.Diagnostics.Debug.WriteLine("   → Native DLL load failed - check sgfplib.dll");
                        }

                        return false;
                    }

                    m_bInit = true;
                    System.Diagnostics.Debug.WriteLine("✓ Init successful");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Init threw exception: {ex.Message}");
                    return false;
                }

                // STEP 6: Open device
                System.Diagnostics.Debug.WriteLine("\n=== Opening Device ===");
                MethodInfo openDeviceMethod = fpManagerType.GetMethod("OpenDevice", new Type[] { typeof(Int32) });

                if (openDeviceMethod == null)
                {
                    System.Diagnostics.Debug.WriteLine("✗ OpenDevice method not found");
                    return false;
                }

                try
                {
                    var openResult = openDeviceMethod.Invoke(m_FPM, new object[] { 0xFF });
                    int openError = Convert.ToInt32(openResult);
                    System.Diagnostics.Debug.WriteLine($"OpenDevice result: {openError} ({GetErrorDescription(openError)})");

                    if (openError != 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"✗ Failed: {GetErrorDescription(openError)}");
                        return false;
                    }

                    m_bSecuGenDeviceOpened = true;
                    System.Diagnostics.Debug.WriteLine("✓ Device opened");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ OpenDevice threw exception: {ex.Message}");
                    return false;
                }

                // STEP 7: Get device info
                System.Diagnostics.Debug.WriteLine("\n=== Getting Device Info ===");
                GetDeviceInfo();

                System.Diagnostics.Debug.WriteLine("\n=====================================");
                System.Diagnostics.Debug.WriteLine("✓✓✓ FULLY INITIALIZED ✓✓✓");
                System.Diagnostics.Debug.WriteLine("=====================================\n");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"\n✗ UNEXPECTED ERROR: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return false;
            }
        }

        private void GetDeviceInfo()
        {
            try
            {
                Type deviceInfoType = null;
                string[] possibleNames = new string[]
                {
                    "SecuGen.FDxSDKPro.Windows.SGFPMDeviceInfoParam",
                    "SecuGen.FDxSDKPro.DotNet.Windows.SGFPMDeviceInfoParam"
                };

                foreach (string name in possibleNames)
                {
                    deviceInfoType = secuGenAssembly.GetType(name);
                    if (deviceInfoType != null) break;
                }

                if (deviceInfoType == null) return;

                MethodInfo getDeviceInfoMethod = fpManagerType.GetMethod("GetDeviceInfo");
                if (getDeviceInfoMethod == null) return;

                var deviceInfo = Activator.CreateInstance(deviceInfoType);
                object[] infoParams = new object[] { deviceInfo };
                var infoResult = getDeviceInfoMethod.Invoke(m_FPM, infoParams);
                int infoError = Convert.ToInt32(infoResult);

                if (infoError == 0)
                {
                    var updatedInfo = infoParams[0];
                    var widthField = deviceInfoType.GetField("ImageWidth");
                    var heightField = deviceInfoType.GetField("ImageHeight");
                    var dpiField = deviceInfoType.GetField("ImageDPI");

                    if (widthField != null) m_ImageWidth = (int)widthField.GetValue(updatedInfo);
                    if (heightField != null) m_ImageHeight = (int)heightField.GetValue(updatedInfo);
                    if (dpiField != null) m_ImageDPI = (int)dpiField.GetValue(updatedInfo);

                    System.Diagnostics.Debug.WriteLine($"✓ Device info: {m_ImageWidth}x{m_ImageHeight} @ {m_ImageDPI} DPI");

                    m_RegMin = new Byte[mMaxTemplateSize];
                    m_VrfMin = new Byte[mMaxTemplateSize];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDeviceInfo error: {ex.Message}");
            }
        }

        private string GetErrorDescription(int errorCode)
        {
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
                        var getImageMethod = fpManagerType.GetMethod("GetImage", new Type[] { typeof(Byte[]) });
                        if (getImageMethod == null) return false;

                        Byte[] fp_image = new Byte[m_ImageWidth * m_ImageHeight];
                        var result = getImageMethod.Invoke(m_FPM, new object[] { fp_image });
                        int errorCode = Convert.ToInt32(result);

                        if (errorCode != 0)
                        {
                            MessageBox.Show($"Failed to capture fingerprint.\n\nError: {GetErrorDescription(errorCode)}",
                                "Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }

                        // Check quality
                        var getQualityMethod = fpManagerType.GetMethod("GetImageQuality");
                        int img_qlty = 100;

                        if (getQualityMethod != null)
                        {
                            object[] qualityParams = new object[] { m_ImageWidth, m_ImageHeight, fp_image, 0 };
                            var qualityResult = getQualityMethod.Invoke(m_FPM, qualityParams);
                            int qualityError = Convert.ToInt32(qualityResult);
                            if (qualityError == 0) img_qlty = (int)qualityParams[3];
                        }

                        if (img_qlty < 50)
                        {
                            MessageBox.Show($"Fingerprint quality too low ({img_qlty}%).\n\nRecommended: 50% or higher\n\nPlease try again.",
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
                if (m_bSecuGenDeviceOpened && m_FPM != null && fpManagerType != null)
                {
                    fpManagerType.GetMethod("CloseDevice")?.Invoke(m_FPM, null);
                    m_bSecuGenDeviceOpened = false;
                }
                m_bInit = false;
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