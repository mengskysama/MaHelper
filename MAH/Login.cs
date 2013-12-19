using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using System.Xml;
using System.Management;
using System.IO;

namespace MAH
{
    public partial class Login : Form
    {
        public int ver = 130804;
        INI ini;


        public Login()
        {
            //if (!File.Exists(Application.StartupPath + "\\" + key))
            //{
            //    Clipboard.SetDataObject(HTTP.code);
            //    MessageBox.Show("Code=" + HTTP.code + "机器码已经复制，CTRL+V粘贴");
            //    Environment.Exit(1);
            //}

            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //string uid = DES.DecryptDES("F449E953CDA93000", "11111111");

            string pass = HTTP.EncryptMD5(HTTP.EncryptMD5(textBox2.Text));
            string ret = null;
            try
            {
                ret = HTTP.Login2(textBox1.Text, pass, 0, "");
            }
            catch (Exception ex)
            {
                MessageBox.Show("失败原因:" + ex.Message + ",可能是服务器间歇性抽风或者版本更新,多试几次");
                return;
            }

            Form1 f = new Form1();
            this.Hide();
            f.Show();
        }


        private void Login_Load(object sender, EventArgs e)
        {
            //string decryptstr = AES.Decrypt(Convert.FromBase64String("NzgOGTK08BvkZN5q8XvG6Q=="), "rBwj1MIAivVN222b");

            //string d;
            //d = Encoding.ASCII.GetString(b);

            //string id = MA.get_area_id(new string[] { "灯火摇曳的漫步之园", "通往金殿玉楼的山丘" });
            //MA.exploration_explore("50002", "5");

                //byte[] undecryptbyte = HTTP.HttpPost1("http://game1-CBT.ma.sdo.com:10001/connect/app/exploration/area?cyt=1", "", "Cookie: S=pb9qdcjmknq2r195mlhbsp7n32", "Cookie2: $Version=1");

                //

 
            ini = new INI(Application.StartupPath + "\\my.ini");
            string str = ini.IniReadValue("LOGIN", "user");
            if (str != null && str != "")
                textBox1.Text = str;

            //脱机用户名密码
            str = ini.IniReadValue("TJ", "proxy");
            if (str != null && str != "")
            {
                MA.http_proxy = str;
                textBox3.Text = str;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0 || textBox2.Text.Length == 0 || textBox4.Text.Length != 2)
            {
                MessageBox.Show("请输入正确用户名密码以及帐号后两位来注册");
                return;
            }
            string pass = HTTP.EncryptMD5(HTTP.EncryptMD5(textBox2.Text));
            try
            {
                string ret = HTTP.Login2(textBox1.Text, pass, 1, textBox4.Text);
                if (ret != null)
                {
                    MessageBox.Show("注册成功!");
                    return;
                }
                else
                {
                    MessageBox.Show("注册失败");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("连接服务器发生异常:" + ex.Message);
                return;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked == true)
            {
                MA.proxy_enable = 1;
                MA.http_proxy = textBox3.Text;
            }
            else
            {
                MA.proxy_enable = 0;
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            MA.http_proxy = textBox3.Text;
            ini.IniWriteValue("TJ", "proxy", textBox3.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string s= HTTP.TestPorxy(textBox3.Text);

            if (s!= null && s.IndexOf("baidu") != -1)
            {
                MessageBox.Show("目测可用");
            }
            else
            {
                MessageBox.Show("不可用");
            }
        }

        private void label5_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {
            try
            {

                System.Diagnostics.Process.Start("http://www.appifan.com/jc/201209/35517.html");

            }

            catch (System.ComponentModel.Win32Exception noBrowser)
            {

                if (noBrowser.ErrorCode == -2147467259)

                    MessageBox.Show(noBrowser.Message);

            }
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {


            MA.host = "game1-CBT.ma.sdo.com:10001";
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            MA.host = "game2-CBT.ma.sdo.com:10001";
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            MA.host = "game.ma.mobimon.com.tw:10001";
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
                HTTP.url = "http://mabiv4.mengsky.net/";
            else
                HTTP.url = "http://mabi.mengsky.net/";
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            MA.host = "game3-CBT.ma.sdo.com:10001";
        }
    }
}
