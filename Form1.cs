using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing.Imaging;
using System.Drawing;
using Emgu.CV.OCR;

namespace lab6
{
    public partial class Form1 : Form
    {
        Image<Gray, byte> bg = null;
        VideoCapture capture = new VideoCapture();
        private int framecount = 0;
        public int Area = 700;

        BackgroundSubtractorMOG2 subtractor = new BackgroundSubtractorMOG2(1000, 32, true);

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = ("Файлы видео | *.mp4; *.webm; *.avi; *.mpg; *.mp2; *.mpeg; *.mov; *.wmv");
            var result = openFileDialog.ShowDialog(); // открытие диалога выбора файла
            if (result == DialogResult.OK) // открытие выбранного файла
            {
                string fileName = openFileDialog.FileName;
                capture = new VideoCapture(fileName);
            }
            timer1.Interval = (int)Math.Round(capture.GetCaptureProperty(CapProp.Fps));
            timer1.Enabled = true;
            BG();
        }


        private void timer1_Tick_1(object sender, EventArgs e)
        {

            framecount++;
            if (framecount >= capture.GetCaptureProperty(CapProp.FrameCount))
            {
                framecount = 0;
                timer1.Stop();
            }
            else
            {
                var frame = capture.QueryFrame();

                imageBox2.Image = Process2(frame);
                imageBox1.Image = Process(frame);


            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            BG();
        }

        private void BG()
        {
            var frame = capture.QueryFrame();

            bg = frame.ToImage<Gray, byte>();
        }
        private void BG(Image<Gray, byte> cur)
        {
            bg = cur;
        }

        private Image<Gray, byte> FilterMask(Image<Gray, byte> mask)
        {            
            var anchor = new Point(-1, -1);
            var borderValue = new MCvScalar(1);
            // создание структурного элемента заданного размера и формы для морфологических операций
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(3, 3), anchor);
            // заполнение небольших тёмных областей
            var closing = mask.MorphologyEx(MorphOp.Close, kernel, anchor, 1, BorderType.Default,
            borderValue);
            // удаление шумов
            var opening = closing.MorphologyEx(MorphOp.Open, kernel, anchor, 1, BorderType.Default,
            borderValue);
            // расширение для слияния небольших смежных областей
            var dilation = opening.Dilate(7);
            // пороговое преобразование для удаления теней
            var threshold = dilation.ThresholdBinary(new Gray(240), new Gray(255));
            return threshold;
        }
        private Image<Bgr, byte> Process2(Mat frame)
        {
            
            Image<Gray, byte> cur = frame.ToImage<Gray, byte>();
            Image<Gray, byte> diff = bg.AbsDiff(cur);
                
                diff.Erode(4);
                diff.Dilate(6);

                diff._ThresholdBinary(new Gray (10), new Gray(255));

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(diff, contours, null, RetrType.External,
                ChainApproxMethod.ChainApproxTc89L1);
            var output = frame.ToImage<Bgr, byte>().Copy();

            for (int i = 0; i < contours.Size; i++)
            {
                if (CvInvoke.ContourArea(contours[i]) > Area)
                {
                    Rectangle boundingRect = CvInvoke.BoundingRectangle(contours[i]);
                    output.Draw(boundingRect, new Bgr(Color.GreenYellow), 2);

                }
            }
            //BG();
            return output;
        }
        private Image<Bgr, byte> Process(Mat frame)
        {
            Image<Gray, byte> cur = frame.ToImage<Gray, byte>();

            //Image<Gray, byte> diff = bg.AbsDiff(cur);

            var foregroundMask = cur.CopyBlank();
            foregroundMask = FilterMask(foregroundMask);

            subtractor.Apply(cur, foregroundMask);

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(foregroundMask, contours, null, RetrType.External,
                ChainApproxMethod.ChainApproxTc89L1);
            var output = frame.ToImage<Bgr, byte>().Copy();

            for (int i = 0; i < contours.Size; i++)
            {
                if (CvInvoke.ContourArea(contours[i]) > Area)
                {
                    Rectangle boundingRect = CvInvoke.BoundingRectangle(contours[i]);
                    output.Draw(boundingRect, new Bgr(Color.GreenYellow), 2);

                }
            }
            return output;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            capture = new VideoCapture();
            capture.ImageGrabbed += ProcessFrame;
            capture.Start(); // начало обработки видеопотока
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            var frame = new Mat();
            capture.Retrieve(frame);

            imageBox1.Image = Process(frame);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Area = Int32.Parse(numericUpDown1.Value.ToString());
        }
    }
}
