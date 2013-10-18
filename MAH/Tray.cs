using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MAH
{
    public partial class Tray : Form
    {
        Form1 frm;


        public delegate void FlushClient(Bitmap bmp1, Bitmap bmp2); //代理
        public void ImgUpdateFunction(Bitmap bmp1, Bitmap bmp2)
        {
            if (this.pictureBox1.InvokeRequired && this.pictureBox2.InvokeRequired)//等待异步 
            {
                FlushClient fc = new FlushClient(ImgUpdateFunction);
                // this.Invoke(fc);//通过代理调用刷新方法 
                this.Invoke(fc, new object[2] { bmp1,bmp2 });
            }
            else
            {
                this.pictureBox1.Image = new Bitmap(bmp1);
                this.pictureBox1.Refresh();

                this.pictureBox2.Image = new Bitmap(bmp2);
                this.pictureBox2.Refresh();
            }
        }


        public Tray(Form1 f)
        {
            frm = f;
            InitializeComponent();
        }

        public void MyShow()
        {
            Show();
            this.Location = new Point(0,System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - 68);
            this.ShowInTaskbar = false;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.TopMost = true;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            frm.Show();
            this.Hide();
            this.ShowInTaskbar = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            frm.Show();
            this.Hide();
            this.ShowInTaskbar = true;
        }

        private void Tray_Click(object sender, EventArgs e)
        {
            frm.Show();
            this.Hide();
            this.ShowInTaskbar = true;
        }
    }
}
