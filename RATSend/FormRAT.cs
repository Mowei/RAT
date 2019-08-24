using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RAT
{
    public class FormRAT : Form
    {
        private byte[] HeaderString;

        private Button buttonSend;
        private TextBox HeaderStringTextBox;
        private Label label1;
        private Label label2;
        private TextBox DisplayImageMillisTextBox;
        private GroupBox groupBox1;
        private RadioButton radioFullScreenSize;
        private RadioButton radioScreen900;
        private RadioButton radioScreen300;
        private int DisplayImageMillis = 3000;
        private Rectangle bounds;

        public FormRAT()
        {
            InitializeComponent();
        }

        private void ButtonSend_Click(object sender, EventArgs e)
        {
            bounds = new Rectangle();
            if (radioScreen300.Checked)
            {
                bounds.Width = 300;
                bounds.Height = 300;
            }
            if (radioScreen900.Checked)
            {
                bounds.Width = 900;
                bounds.Height = 900;
            }
            if (radioFullScreenSize.Checked)
            {
                //取得螢幕尺寸
                bounds = Screen.FromControl(this).Bounds;
            }

            DisplayImageMillis = int.Parse(DisplayImageMillisTextBox.Text);
            HeaderString = Encoding.ASCII.GetBytes(this.HeaderStringTextBox.Text);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select file to send...";
            openFileDialog.Filter = "All Files|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                SendFile(openFileDialog.FileName);
            }
        }


        private void SendFile(string filename)
        {
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
            byte[] array = new byte[HeaderString.Length + bytes.Length];
            HeaderString.CopyTo(array, 0);
            bytes.CopyTo(array, HeaderString.Length);
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
                    return PixelFormat.Format32bppPArgb;
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


        private void InitializeComponent()
        {
            this.buttonSend = new  Button();
            this.HeaderStringTextBox = new TextBox();
            this.label1 = new Label();
            this.label2 = new Label();
            this.DisplayImageMillisTextBox = new TextBox();
            this.groupBox1 = new GroupBox();
            this.radioFullScreenSize = new RadioButton();
            this.radioScreen900 = new RadioButton();
            this.radioScreen300 = new RadioButton();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonSend
            // 
            this.buttonSend.Location = new Point(197, 228);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new Size(75, 21);
            this.buttonSend.TabIndex = 3;
            this.buttonSend.Text = "Send file";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new EventHandler(this.ButtonSend_Click);
            // 
            // HeaderStringTextBox
            // 
            this.HeaderStringTextBox.Location = new Point(115, 23);
            this.HeaderStringTextBox.Name = "HeaderStringTextBox";
            this.HeaderStringTextBox.Size = new Size(157, 22);
            this.HeaderStringTextBox.TabIndex = 4;
            this.HeaderStringTextBox.Text = "RAT";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new Point(12, 27);
            this.label1.Name = "label1";
            this.label1.Size = new Size(66, 12);
            this.label1.TabIndex = 5;
            this.label1.Text = "HeaderString";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new Point(13, 56);
            this.label2.Name = "label2";
            this.label2.Size = new Size(95, 12);
            this.label2.TabIndex = 6;
            this.label2.Text = "DisplayImageMillis";
            // 
            // DisplayImageMillisTextBox
            // 
            this.DisplayImageMillisTextBox.Location = new Point(115, 51);
            this.DisplayImageMillisTextBox.Name = "DisplayImageMillisTextBox";
            this.DisplayImageMillisTextBox.Size = new Size(157, 22);
            this.DisplayImageMillisTextBox.TabIndex = 7;
            this.DisplayImageMillisTextBox.Text = "3000";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioFullScreenSize);
            this.groupBox1.Controls.Add(this.radioScreen900);
            this.groupBox1.Controls.Add(this.radioScreen300);
            this.groupBox1.Location = new Point(14, 79);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new Size(258, 98);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Screen Size";
            // 
            // radioFullScreenSize
            // 
            this.radioFullScreenSize.AutoSize = true;
            this.radioFullScreenSize.Location = new Point(7, 67);
            this.radioFullScreenSize.Name = "radioFullScreenSize";
            this.radioFullScreenSize.Size = new Size(91, 16);
            this.radioFullScreenSize.TabIndex = 2;
            this.radioFullScreenSize.Text = "FullScreenSize";
            this.radioFullScreenSize.UseVisualStyleBackColor = true;
            // 
            // radioScreen900
            // 
            this.radioScreen900.AutoSize = true;
            this.radioScreen900.Location = new Point(7, 45);
            this.radioScreen900.Name = "radioScreen900";
            this.radioScreen900.Size = new Size(73, 16);
            this.radioScreen900.TabIndex = 1;
            this.radioScreen900.Text = "900 X 900";
            this.radioScreen900.UseVisualStyleBackColor = true;
            // 
            // radioScreen300
            // 
            this.radioScreen300.AutoSize = true;
            this.radioScreen300.Checked = true;
            this.radioScreen300.Location = new Point(7, 22);
            this.radioScreen300.Name = "radioScreen300";
            this.radioScreen300.Size = new Size(73, 16);
            this.radioScreen300.TabIndex = 0;
            this.radioScreen300.TabStop = true;
            this.radioScreen300.Text = "300 X 300";
            this.radioScreen300.UseVisualStyleBackColor = true;
            // 
            // FormRAT
            // 
            this.AutoScaleDimensions = new SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(284, 261);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.DisplayImageMillisTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.HeaderStringTextBox);
            this.Controls.Add(this.buttonSend);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormRAT";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RATSend";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

    }
}
