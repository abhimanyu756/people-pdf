using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

namespace people_pdf
{
    public class WebcamHandler : IDisposable
    {
        private VideoCaptureDevice videoSource;
        private bool isConnected = false;
        private Bitmap currentFrame;
        private readonly object frameLock = new object();

        public static bool IsCameraAvailable()
        {
            try
            {
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                return videoDevices.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool InitializeCamera(Control previewControl)
        {
            try
            {
                // Get list of video devices
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No video devices found");
                    return false;
                }

                // FIXED: Access the MonikerString properly
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);

                // Set video resolution (try to get a decent resolution)
                if (videoSource.VideoCapabilities.Length > 0)
                {
                    VideoCapabilities bestCapability = videoSource.VideoCapabilities[0];

                    // Try to find a good resolution (prefer 640x480 or higher)
                    foreach (VideoCapabilities capability in videoSource.VideoCapabilities)
                    {
                        if (capability.FrameSize.Width >= 640 && capability.FrameSize.Height >= 480)
                        {
                            bestCapability = capability;
                            break;
                        }
                    }

                    videoSource.VideoResolution = bestCapability;
                }

                // Set event handler
                videoSource.NewFrame += VideoSource_NewFrame;

                // Start video source
                videoSource.Start();
                isConnected = true;

                System.Diagnostics.Debug.WriteLine("Camera initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Camera initialization error: {ex.Message}");
                return false;
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            lock (frameLock)
            {
                // Dispose old frame
                currentFrame?.Dispose();
                // Clone the new frame
                currentFrame = (Bitmap)eventArgs.Frame.Clone();
            }
        }

        public Bitmap CaptureImage()
        {
            if (!isConnected || videoSource == null)
                return null;

            try
            {
                lock (frameLock)
                {
                    if (currentFrame != null)
                    {
                        // Return a copy of the current frame
                        return new Bitmap(currentFrame);
                    }
                }

                // Wait a bit for a frame to be available
                System.Threading.Thread.Sleep(100);

                lock (frameLock)
                {
                    if (currentFrame != null)
                    {
                        return new Bitmap(currentFrame);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Capture error: {ex.Message}");
                return null;
            }
        }

        public void StopPreview()
        {
            // Preview control is automatic with AForge, no manual control needed
        }

        public void StartPreview()
        {
            // Preview control is automatic with AForge, no manual control needed
        }

        public void Disconnect()
        {
            try
            {
                if (videoSource != null && videoSource.IsRunning)
                {
                    videoSource.SignalToStop();
                    videoSource.WaitForStop();
                    isConnected = false;
                }

                lock (frameLock)
                {
                    currentFrame?.Dispose();
                    currentFrame = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Disconnect error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Disconnect();

            try
            {
                if (videoSource != null)
                {
                    videoSource.NewFrame -= VideoSource_NewFrame;
                    videoSource = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dispose error: {ex.Message}");
            }
        }
    }
}
