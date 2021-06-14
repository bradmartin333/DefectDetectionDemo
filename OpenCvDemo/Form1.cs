using System.Drawing;
using System.Windows.Forms;
using Accord.Video;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace OpenCvDemo
{
    public partial class Form1 : Form
    {
        private static Rectangle screenBounds = Screen.AllScreens[2].Bounds;
        private ScreenCaptureStream screenCaptureStream = new ScreenCaptureStream(new Rectangle(screenBounds.X + 250, screenBounds.Y + 100, screenBounds.Width - 500, screenBounds.Height - 200));

        public Form1()
        {
            GlobalMouseHandler.MouseMovedEvent += GlobalMouseHandler_MouseMovedEvent;
            Application.AddMessageFilter(new GlobalMouseHandler());

            InitializeComponent();

            screenCaptureStream.NewFrame += ScreenCaptureStream_NewFrame;
            screenCaptureStream.Start();
        }

        private void ScreenCaptureStream_NewFrame(object sender, Accord.Video.NewFrameEventArgs eventArgs)
        {
            Bitmap bmpBaseOriginal = eventArgs.Frame;
            Bitmap resized = new Bitmap(bmpBaseOriginal, new System.Drawing.Size(bmpBaseOriginal.Width / 3, bmpBaseOriginal.Height / 3));
            Show(resized);
        }

        private void GlobalMouseHandler_MouseMovedEvent(object sender, MouseEventArgs e)
        {
            label1.Text = string.Format("X: {0}, Y: {1}", e.X, e.Y);
        }

        private void Show(Bitmap bmp)
        {
            var srcImage = bmp.ToMat();
            var binaryImage = new Mat(srcImage.Size(), MatType.CV_8UC1);

            Cv2.CvtColor(srcImage, binaryImage, ColorConversionCodes.BGRA2GRAY);
            Cv2.Threshold(binaryImage, binaryImage, thresh: 100, maxval: 255, type: ThresholdTypes.Binary);

            var detectorParams = new SimpleBlobDetector.Params
            {
                MinDistBetweenBlobs = 10,
                MinRepeatability = 1,

                FilterByArea = true,
                MinArea = 1000f,
                MaxArea = 10000f,

                FilterByCircularity = true,
                MinCircularity = 0.1f,
                MaxCircularity = 1f,

                FilterByConvexity = true,
                MinConvexity = 0.1f,
                MaxConvexity = 100,

                FilterByInertia = true,
                MinInertiaRatio = 0.1f,
            };
            var simpleBlobDetector = SimpleBlobDetector.Create(detectorParams);
            var keyPoints = simpleBlobDetector.Detect(binaryImage);

            var imageWithKeyPoints = new Mat();

            Cv2.DrawKeypoints(
                    image: binaryImage,
                    keypoints: keyPoints,
                    outImage: imageWithKeyPoints,
                    color: Scalar.FromRgb(255, 0, 0),
                    flags: DrawMatchesFlags.DrawRichKeypoints);

            try
            {
                pictureBox1.Image = imageWithKeyPoints.ToBitmap();
                pictureBox1.Invoke(new MethodInvoker(Refresh));
            }
            catch
            {
                screenCaptureStream.Stop();
            }
        }
    }
}
