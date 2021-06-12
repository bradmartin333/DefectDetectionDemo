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
        public Form1()
        {
            GlobalMouseHandler.MouseMovedEvent += GlobalMouseHandler_MouseMovedEvent;
            Application.AddMessageFilter(new GlobalMouseHandler());

            InitializeComponent();
            backgroundWorker1.RunWorkerAsync();
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
    }
}
