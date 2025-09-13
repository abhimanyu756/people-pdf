using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace people_pdf
{
    public enum SGFPMError
    {
        SGFPM_OK = 0,
        SGFPM_ERROR_CREATION_FAILED = 1,
        SGFPM_ERROR_FUNCTION_FAILED = 2,
        SGFPM_ERROR_INVALID_PARAM = 3,
        SGFPM_ERROR_NOT_USED = 4,
        SGFPM_ERROR_DLLLOAD_FAILED = 5,
        SGFPM_ERROR_INVALID_DEVICE_NAME = 6,
        SGFPM_ERROR_UNSUPPORTED_DEV = 7,
        SGFPM_ERROR_INVALID_DEV_INDEX = 8
    }

    public enum SGFPMDeviceName
    {
        DEV_AUTO = 0,
        DEV_FDP02 = 1,
        DEV_FDU02 = 2,
        DEV_FDU03 = 3,
        DEV_FDU04 = 4,
        DEV_FDU05 = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SGFPMDeviceInfoParam
    {
        public int ImageWidth;
        public int ImageHeight;
        public int ImageDPI;
        public int MaxTemplateSize;
        public int DeviceId;
    }

    public class SecuGenPInvokeWrapper : IDisposable
    {
        // Import native DLL functions using P/Invoke
        [DllImport("sgfplib.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int SGFPM_Create(out IntPtr phFPM);

        [DllImport("sgfplib.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int SGFPM_Close(IntPtr hFPM);

        [DllImport("sgfplib.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int SGFPM_Init(IntPtr hFPM, int devname);

        [DllImport("sgfplib.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int SGFPM_GetDeviceCount(IntPtr hFPM, out int pnDevices);

        [DllImport("sgfplib.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int SGFPM_OpenDevice(IntPtr hFPM, int dev_index);

        [DllImport("sgfplib.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int SGFPM_CloseDevice(IntPtr hFPM);

        [DllImport("sgfplib.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int SGFPM_GetDeviceInfo(IntPtr hFPM, out SGFPMDeviceInfoParam pDeviceInfo);

        [DllImport("sgfplib.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int SGFPM_GetImage(IntPtr hFPM, byte[] pImage);

        [DllImport("sgfplib.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int SGFPM_GetImageQuality(IntPtr hFPM, int iWidth, int iHeight, byte[] pImage, out int pnQuality);

        private IntPtr m_hFPM = IntPtr.Zero;
        private bool m_bInit = false;
        private bool m_bDeviceOpened = false;
        private SGFPMDeviceInfoParam m_DeviceInfo;

        public bool InitializeDevice()
        {
            try
            {
                // Create fingerprint manager instance
                int result = SGFPM_Create(out m_hFPM);
                if (result != 0)
                {
                    MessageBox.Show($"Failed to create fingerprint manager. Error: {result}",
                                  "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Initialize the device
                result = SGFPM_Init(m_hFPM, (int)SGFPMDeviceName.DEV_AUTO);
                if (result != 0)
                {
                    MessageBox.Show($"Failed to initialize fingerprint device. Error: {result}",
                                  "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                m_bInit = true;

                // Get device count
                int deviceCount = 0;
                result = SGFPM_GetDeviceCount(m_hFPM, out deviceCount);
                if (result != 0 || deviceCount == 0)
                {
                    MessageBox.Show("No SecuGen fingerprint devices found.",
                                  "Device Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                // Open the first device
                result = SGFPM_OpenDevice(m_hFPM, 0);
                if (result != 0)
                {
                    MessageBox.Show($"Failed to open fingerprint device. Error: {result}",
                                  "Device Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                m_bDeviceOpened = true;

                // Get device info
                result = SGFPM_GetDeviceInfo(m_hFPM, out m_DeviceInfo);
                if (result != 0)
                {
                    MessageBox.Show("Failed to get device information.",
                                  "Device Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                return true;
            }
            catch (DllNotFoundException)
            {
                MessageBox.Show("SecuGen DLL files not found. Please ensure:\n" +
                              "1. Copy sgfplib.dll to your application folder\n" +
                              "2. Install SecuGen runtime dependencies\n" +
                              "3. Use correct architecture (x86/x64)",
                              "DLL Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
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
            if (!m_bInit || !m_bDeviceOpened)
            {
                MessageBox.Show("Fingerprint device not initialized or opened.",
                              "Device Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                // Create capture dialog
                using (Form captureDialog = CreateCaptureDialog())
                {
                    if (captureDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Create image buffer
                        byte[] imageBuffer = new byte[m_DeviceInfo.ImageWidth * m_DeviceInfo.ImageHeight];

                        // Capture fingerprint image
                        int result = SGFPM_GetImage(m_hFPM, imageBuffer);
                        if (result != 0)
                        {
                            MessageBox.Show($"Failed to capture fingerprint image. Error: {result}",
                                          "Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }

                        // Check image quality
                        int quality = 0;
                        result = SGFPM_GetImageQuality(m_hFPM, m_DeviceInfo.ImageWidth, m_DeviceInfo.ImageHeight, imageBuffer, out quality);

                        if (quality < 50) // Quality threshold
                        {
                            MessageBox.Show($"Fingerprint quality too low ({quality}%). Please try again.",
                                          "Quality Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }

                        // Convert to bitmap and display
                        Bitmap bitmap = ConvertToBitmap(imageBuffer, m_DeviceInfo.ImageWidth, m_DeviceInfo.ImageHeight);
                        if (bitmap != null)
                        {
                            pictureBox.Image = bitmap;
                            MessageBox.Show($"Fingerprint captured successfully! Quality: {quality}%",
                                          "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return true;
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

        private Form CreateCaptureDialog()
        {
            Form dialog = new Form
            {
                Text = "Fingerprint Capture",
                Size = new Size(400, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label label = new Label
            {
                Text = "Place your finger on the scanner and click Capture",
                Location = new Point(20, 30),
                Size = new Size(350, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Button captureBtn = new Button
            {
                Text = "Capture",
                Location = new Point(100, 80),
                Size = new Size(100, 30),
                DialogResult = DialogResult.OK
            };

            Button cancelBtn = new Button
            {
                Text = "Cancel",
                Location = new Point(210, 80),
                Size = new Size(100, 30),
                DialogResult = DialogResult.Cancel
            };

            dialog.Controls.AddRange(new Control[] { label, captureBtn, cancelBtn });
            return dialog;
        }

        private Bitmap ConvertToBitmap(byte[] imageData, int width, int height)
        {
            try
            {
                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

                // Set grayscale palette
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bitmap.Palette = palette;

                // Copy image data
                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                                   ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                Marshal.Copy(imageData, 0, bmpData.Scan0, imageData.Length);
                bitmap.UnlockBits(bmpData);

                return bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error converting image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public bool IsDeviceReady()
        {
            return m_bInit && m_bDeviceOpened;
        }

        public void Dispose()
        {
            try
            {
                if (m_bDeviceOpened)
                {
                    SGFPM_CloseDevice(m_hFPM);
                    m_bDeviceOpened = false;
                }

                if (m_bInit && m_hFPM != IntPtr.Zero)
                {
                    SGFPM_Close(m_hFPM);
                    m_bInit = false;
                    m_hFPM = IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing: {ex.Message}");
            }
        }
    }
}