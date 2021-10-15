using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Video;
using MathNet.Numerics.Statistics;

namespace PostFinder
{
    public partial class Form1 : Form
    {
        private readonly ScreenCaptureStream _ScreenCaptureStream = new ScreenCaptureStream(new Rectangle(150, 300, 500, 500)); // Watch a portion of screen
        private readonly List<double> _AngleList = new List<double>(); // Running angle stats
        private readonly int _DotSize = 5;

        public Form1()
        {
            InitializeComponent();
            FormClosing += Form1_FormClosing;
            _ScreenCaptureStream.NewFrame += ScreenCaptureStream_NewFrame;
            _ScreenCaptureStream.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _ScreenCaptureStream.Stop();
        }

        private void ScreenCaptureStream_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bmpBaseOriginal = eventArgs.Frame;
            Bitmap resized = new Bitmap(bmpBaseOriginal, new Size(bmpBaseOriginal.Width / 3, bmpBaseOriginal.Height / 3));
            Show(resized);
        }

        private void Show(Bitmap bmp)
        {
            // Setup Image
            Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
            bmp = filter.Apply(bmp);
            Threshold threshold = new Threshold(220);
            threshold.ApplyInPlace(bmp);
            Bitmap clone = (Bitmap)bmp.Clone();

            // Lock Image
            BitmapData bitmapData = bmp.LockBits(ImageLockMode.ReadWrite);

            // Find Blobs (with some params - there are a lot more)
            BlobCounter blobCounter = new BlobCounter
            {
                FilterBlobs = true,
                MinHeight = 2,
                MinWidth = 2
            };
            blobCounter.ProcessImage(bitmapData);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            bmp.UnlockBits(bitmapData);

            // Draw Dots
            Bitmap overlay = new Bitmap(bmp.Width, bmp.Height);
            using (Graphics g = Graphics.FromImage(overlay))
            {
                for (int i = 0; i < blobs.Length; i++)
                {
                    g.DrawEllipse(Pens.Red, new Rectangle(
                        (int)(blobs[i].CenterOfGravity.X - _DotSize), 
                        (int)(blobs[i].CenterOfGravity.Y - _DotSize), 
                        _DotSize * 2, _DotSize * 2));
                }
            }

            // Update Image
            pictureBox.BackgroundImage = bmp;
            pictureBox.Image = overlay;

            // Find Angle
            try
            {
                Deskew deskew = new Deskew(clone);
                _AngleList.Add(deskew.GetSkewAngle());
                MovingStatistics movingStatistics = new MovingStatistics(10, _AngleList);
                label.Invoke((MethodInvoker)delegate ()
                {
                    label.Text = string.Format("NumPosts = {0}, Angle = {1}", blobs.Length, Math.Round(movingStatistics.Mean, 3)); ;
                });
            }
            catch (Exception) { }
        }
    }
}
