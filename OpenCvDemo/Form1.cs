using Accord.Video.FFMPEG;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace OpenCvDemo
{
    public partial class Form1 : Form
    {
        private Accord.Video.ScreenCaptureStream screenCaptureStream = new Accord.Video.ScreenCaptureStream(Screen.AllScreens[2].Bounds);

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
            Show2(new Demo { Image = resized, Blur = 7, ThresholdLow = 10, ThresholdHigh = 30 });
        }

        private void GlobalMouseHandler_MouseMovedEvent(object sender, MouseEventArgs e)
        {
            label1.Text = string.Format("X: {0}, Y: {1}", e.X, e.Y);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            using (var vFReader = new VideoFileReader())
            {
                vFReader.Open(@"C:\Users\delta\Downloads\video.mp4");
                for (int i = 0; i < vFReader.FrameCount; i++)
                {
                    Bitmap bmpBaseOriginal = vFReader.ReadVideoFrame();
                    Bitmap resized = new Bitmap(bmpBaseOriginal, new System.Drawing.Size(bmpBaseOriginal.Width / 3, bmpBaseOriginal.Height / 3));
                    Show(new Demo { Image = resized, Blur = 7, ThresholdLow = 10, ThresholdHigh = 30 });
                }
                vFReader.Close();
            }
        }

        private void Show(Demo demo)
        {
            Mat img = demo.Image.ToMat();

            #region blur
            var blur = new Mat();
            Cv2.GaussianBlur(img, blur, new OpenCvSharp.Size(demo.Blur, demo.Blur), 0);
            #endregion

            #region canny
            var canny = new Mat();
            Cv2.Canny(blur, canny, demo.ThresholdLow, demo.ThresholdHigh);
            Cv2.Dilate(canny, canny, new Mat());
            #endregion

            #region contours
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchyIndexes;
            Cv2.FindContours(
                canny,
                out contours,
                out hierarchyIndexes,
                mode: RetrievalModes.External,
                method: ContourApproximationModes.ApproxSimple);
            #endregion

            if (demo.Filter) contours = contours.Where(x => x.Length > 50).ToArray();
            Cv2.DrawContours(img, contours, -1, Scalar.Red, thickness: 1);

            pictureBox1.Image = img.ToBitmap();
            pictureBox1.Invoke(new MethodInvoker(Refresh));

            Vec3b bgrPixel = img.Get<Vec3b>(0, 0);
            pictureBox3.BackColor = Color.FromArgb(bgrPixel.Item0, bgrPixel.Item1, bgrPixel.Item2);
            pictureBox3.Invoke(new MethodInvoker(Refresh));
        }

        private void Show2(Demo demo)
        {
            var srcImage = demo.Image.ToMat();
            var binaryImage = new Mat(srcImage.Size(), MatType.CV_8UC1);

            Cv2.CvtColor(srcImage, binaryImage, ColorConversionCodes.BGRA2GRAY);
            Cv2.Threshold(binaryImage, binaryImage, thresh: 100, maxval: 255, type: ThresholdTypes.Binary);

            var detectorParams = new SimpleBlobDetector.Params
            {
                //MinDistBetweenBlobs = 10, // 10 pixels between blobs
                //MinRepeatability = 1,

                //MinThreshold = 100,
                //MaxThreshold = 255,
                //ThresholdStep = 5,

                FilterByArea = false,
                //FilterByArea = true,
                //MinArea = 0.001f, // 10 pixels squared
                //MaxArea = 500,

                FilterByCircularity = false,
                //FilterByCircularity = true,
                //MinCircularity = 0.001f,

                FilterByConvexity = false,
                //FilterByConvexity = true,
                //MinConvexity = 0.001f,
                //MaxConvexity = 10,

                FilterByInertia = false,
                //FilterByInertia = true,
                //MinInertiaRatio = 0.001f,

                FilterByColor = false
                //FilterByColor = true,
                //BlobColor = 255 // to extract light blobs
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

            pictureBox1.Image = imageWithKeyPoints.ToBitmap();
            pictureBox1.Invoke(new MethodInvoker(Refresh));

            Vec3b bgrPixel = imageWithKeyPoints.Get<Vec3b>(0, 0);
            pictureBox3.BackColor = Color.FromArgb(bgrPixel.Item0, bgrPixel.Item1, bgrPixel.Item2);
            pictureBox3.Invoke(new MethodInvoker(Refresh));
        }
    }
}
