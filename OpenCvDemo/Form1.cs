using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Accord.Video;
using MathNet.Numerics.Statistics;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace OpenCvDemo
{
    public partial class Form1 : Form
    {
        private static Rectangle screenBounds = Screen.AllScreens[2].Bounds;
        private ScreenCaptureStream screenCaptureStream = new ScreenCaptureStream(new Rectangle(screenBounds.X + 250, screenBounds.Y + 100, screenBounds.Width - 500, screenBounds.Height - 200));
        private List<double> angleList = new List<double>();

        public Form1()
        {
            InitializeComponent();
            screenCaptureStream.NewFrame += ScreenCaptureStream_NewFrame;
            screenCaptureStream.Start();
        }

        private void Form1_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            screenCaptureStream.Stop();
        }

        private void ScreenCaptureStream_NewFrame(object sender, Accord.Video.NewFrameEventArgs eventArgs)
        {
            Bitmap bmpBaseOriginal = eventArgs.Frame;
            Bitmap resized = new Bitmap(bmpBaseOriginal, new System.Drawing.Size(bmpBaseOriginal.Width / 3, bmpBaseOriginal.Height / 3));
            Show(resized);
        }

        private void Show(Bitmap bmp)
        {
            var srcImage = bmp.ToMat();
            var binaryImage = new Mat(srcImage.Size(), MatType.CV_8UC1);

            Cv2.CvtColor(srcImage, binaryImage, ColorConversionCodes.BGRA2GRAY);
            Cv2.Threshold(binaryImage, binaryImage, thresh: 100, maxval: 255, type: ThresholdTypes.Binary);
            Cv2.GaussianBlur(binaryImage, binaryImage, new OpenCvSharp.Size(7, 7), 0);

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
                    image: srcImage,
                    keypoints: keyPoints,
                    outImage: imageWithKeyPoints,
                    color: Scalar.Green);

            pictureBox1.Image = imageWithKeyPoints.ToBitmap();

            binaryImage.SaveImage(@"C:\Users\delta\Desktop\test.png");

            Deskew deskew = new Deskew(binaryImage.ToBitmap());
            angleList.Add(deskew.GetSkewAngle());
            MovingStatistics movingStatistics = new MovingStatistics(10, angleList);
            label1.Invoke((MethodInvoker)delegate ()
            {
                label1.Text = string.Format("NumPosts = {0}, Angle = {1}", keyPoints.Length, Math.Round(movingStatistics.Mean, 3)); ;
            });
        }
    }
}
