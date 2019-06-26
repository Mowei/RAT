using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private const string magicString = "PTP-RAT-CHUNK";

        private byte[] magicHeader;

        private GroupBox groupBoxFunction;

        private RadioButton radioButtonReceiver;

        private RadioButton radioButtonSender;

        private Button buttonSend;

        private readonly int DisplayImageMillis = 1500;


        public FormRAT()
        {
            InitializeComponent();
            magicHeader = Encoding.ASCII.GetBytes(magicString);
        }

        private void ButtonSend_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select file to send...";
            openFileDialog.Filter = "All Files|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                SendFile(openFileDialog.FileName);
            }
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs ev)
        {
            if (buttonSend.Enabled != radioButtonSender.Checked)
            {
                buttonSend.Enabled = radioButtonSender.Checked;
                if (radioButtonReceiver.Enabled)
                {
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
                else
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
        }

        private void SendFile(string filename)
        {
            //取得螢幕尺寸
            Rectangle bounds = Screen.FromControl(this).Bounds;
            /*
            Rectangle bounds = new Rectangle();
            bounds.Width = 300;
            bounds.Height = 300;
            */
            List<Bitmap> bitmaps = FileToBitmaps(File.ReadAllBytes(filename), bounds.Width, bounds.Height);
            DisplayBitmaps(bitmaps);
        }

        private List<Bitmap> FileToBitmaps(byte[] file, int width, int height)
        {
            int colorBytes = 3;
            ushort sequenceNumber = 0;
            byte[] headerArray = Encode(CreateHeader(sequenceNumber));
            sequenceNumber++;
            int imageSize = width * height * colorBytes;
            byte[] fileArrary = PadFile(Encode(file), imageSize - headerArray.Length);
            List<Bitmap> list = new List<Bitmap>();
            for (int pos = 0; pos < fileArrary.Length; pos = pos + imageSize - headerArray.Length)
            {
                headerArray = Encode(CreateHeader(sequenceNumber));
                sequenceNumber++;
                byte[] imageArrary = new byte[imageSize];
                headerArray.CopyTo(imageArrary, 0);
                Array.Copy(fileArrary, pos, imageArrary, headerArray.Length, imageSize - headerArray.Length);
                Bitmap bitmap = new Bitmap(width, height, BytesToFormat(colorBytes));
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                Marshal.Copy(imageArrary, 0, bitmapData.Scan0, imageArrary.Length);
                bitmap.UnlockBits(bitmapData);
                list.Add(bitmap);
            }
            return list;
        }

        private byte[] CreateHeader(ushort sequenceNumber)
        {
            byte[] bytes = BitConverter.GetBytes(sequenceNumber);
            byte[] array = new byte[magicHeader.Length + bytes.Length];
            magicHeader.CopyTo(array, 0);
            bytes.CopyTo(array, magicHeader.Length);
            return array;
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
                    return PixelFormat.Format24bppRgb;
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

        private void DisplayBitmaps(List<Bitmap> bitmaps)
        {
            Cursor.Hide();
            ImageForm imageForm = new ImageForm();
            foreach (Bitmap bitmap in bitmaps)
            {
                imageForm.SetImage(bitmap);
                Thread.Sleep(DisplayImageMillis);
            }
            imageForm.Close();
            Cursor.Show();
        }

        private void CaptureScreens()
        {
            ushort num = 0;
            byte[] screenFlash = GetScreenFlash();
            byte[] first = SubArray(screenFlash, 0, magicHeader.Length);
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
            Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, BytesToFormat(Screen.PrimaryScreen.BitsPerPixel / 8));
            Graphics.FromImage(bitmap).CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
            return Decode(ImageToBytes(bitmap));
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

        private static byte[] ImageToBytes(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            IntPtr scan = bitmapData.Scan0;
            int num = Math.Abs(bitmapData.Stride) * bmp.Height;
            byte[] array = new byte[num];
            Marshal.Copy(scan, array, 0, num);
            bmp.UnlockBits(bitmapData);
            return array;
        }

        //抗失真1Bit用1Bytes表示
        private static byte[] Encode(byte[] input)
        {
            byte[] array = new byte[input.Length * 8];
            int num = 0;
            for (int i = 0; i < input.Length; i++)
            {
                //轉成二進制8位String(Ex: 01101111)
                string text = Convert.ToString(input[i], 2).PadLeft(8, '0');
                for (int j = 0; j < text.Length; j++)
                {
                    if (text[j] == '0')
                    {
                        array[num++] = 0;
                    }
                    else
                    {
                        array[num++] = byte.MaxValue;
                    }
                }
            }
            return array;
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
            this.groupBoxFunction = new System.Windows.Forms.GroupBox();
            this.radioButtonReceiver = new System.Windows.Forms.RadioButton();
            this.radioButtonSender = new System.Windows.Forms.RadioButton();
            this.buttonSend = new System.Windows.Forms.Button();
            this.groupBoxFunction.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxFunction
            // 
            this.groupBoxFunction.Controls.Add(this.radioButtonReceiver);
            this.groupBoxFunction.Controls.Add(this.radioButtonSender);
            this.groupBoxFunction.Location = new System.Drawing.Point(12, 11);
            this.groupBoxFunction.Name = "groupBoxFunction";
            this.groupBoxFunction.Size = new System.Drawing.Size(130, 77);
            this.groupBoxFunction.TabIndex = 1;
            this.groupBoxFunction.TabStop = false;
            this.groupBoxFunction.Text = "Function Selector";
            // 
            // radioButtonReceiver
            // 
            this.radioButtonReceiver.AutoSize = true;
            this.radioButtonReceiver.Location = new System.Drawing.Point(23, 41);
            this.radioButtonReceiver.Name = "radioButtonReceiver";
            this.radioButtonReceiver.Size = new System.Drawing.Size(64, 16);
            this.radioButtonReceiver.TabIndex = 1;
            this.radioButtonReceiver.Text = "Receiver";
            this.radioButtonReceiver.UseVisualStyleBackColor = true;
            this.radioButtonReceiver.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
            // 
            // radioButtonSender
            // 
            this.radioButtonSender.AutoSize = true;
            this.radioButtonSender.Checked = true;
            this.radioButtonSender.Location = new System.Drawing.Point(23, 18);
            this.radioButtonSender.Name = "radioButtonSender";
            this.radioButtonSender.Size = new System.Drawing.Size(55, 16);
            this.radioButtonSender.TabIndex = 0;
            this.radioButtonSender.TabStop = true;
            this.radioButtonSender.Text = "Sender";
            this.radioButtonSender.UseVisualStyleBackColor = true;
            this.radioButtonSender.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
            // 
            // buttonSend
            // 
            this.buttonSend.Location = new System.Drawing.Point(148, 24);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(75, 21);
            this.buttonSend.TabIndex = 3;
            this.buttonSend.Text = "Send file";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new System.EventHandler(this.ButtonSend_Click);
            // 
            // FormRAT
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(239, 100);
            this.Controls.Add(this.buttonSend);
            this.Controls.Add(this.groupBoxFunction);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormRAT";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "The Rat by Pen Test Partners";
            this.groupBoxFunction.ResumeLayout(false);
            this.groupBoxFunction.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
