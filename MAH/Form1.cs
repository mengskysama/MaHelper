using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpPcap.LibPcap;
using SharpPcap;

namespace MAH
{
    public partial class Form1 : Form
    {
        public static Form1 frm;

        List<string> lstSrcipt = new List<string>();
        Thread thread = null;
        public Tray frmtray;
        static Thread threadScript;
        static Thread threadCookie;
        static Thread threadMA_Client;
        
        public int sleepdelay = 0;

        LibPcapLiveDeviceList devicelist = null;
        LibPcapLiveDevice usedev = null;

        INI ini = null;

        //修改的
        public static int editcard = 0;//1 2 3

        public static int use = 0;//用脚本0   1POST
        //卡组数据
        public string d1, d2, d3;
        //cookie
        public string cookie,cookie2;
        //显示快照
        public static int disbmp = 0;

        Point p1 = new Point(-1,-1), p2;

        
        int usep = 0;

        public delegate void FlushClient(Bitmap bmp); //代理

        public delegate void FlushClient_Log(string str, int type = 0); //代理

        public delegate void FlushClient_UList(string [] str); //代理

        public delegate void FlushClient_FList(string[] str, int clean, int max); //代理

        public void ImgUpdateFunction(Bitmap bmp)
        {
            if (this.pictureBox1.InvokeRequired)//等待异步 
            {
                FlushClient fc = new FlushClient(ImgUpdateFunction);
                // this.Invoke(fc);//通过代理调用刷新方法 
                this.Invoke(fc, new object[1] { bmp });
            }
            else
            {
                if (disbmp == 1)
                {
                    this.pictureBox1.Image = bmp;
                    this.pictureBox1.Refresh();
                }
            }
        }

        public void LogUpdateFunction(string str, int type = 0)
        {
            if (this.textBox1.InvokeRequired)//等待异步 
            {
                FlushClient_Log fc = new FlushClient_Log(LogUpdateFunction);
                // this.Invoke(fc);//通过代理调用刷新方法 
                this.Invoke(fc, new object[2] { str, type });
            }
            else
            {
                Log.Log_Write(str);
                this.textBox1.AppendText("\r\n" + str);
            }
        }

        public void FListUpdateFunction(string[] str, int insertid, int max = -1)
        {
            if (this.listView2.InvokeRequired)//等待异步 
            {
                FlushClient_FList fc = new FlushClient_FList(FListUpdateFunction);
                // this.Invoke(fc);//通过代理调用刷新方法 
                this.Invoke(fc, new object[3] { str, insertid, max});
            }
            else
            {
                if (max != -1)
                {
                    while(listView1.Items.Count>max)
                        listView1.Items.RemoveAt(listView1.Items.Count-1);
                }
                else
                {
                    if (insertid < listView1.Items.Count)
                    {
                        listView1.Items[insertid].Text = str[0];
                        listView1.Items[insertid].SubItems[1].Text = str[1];
                        listView1.Items[insertid].SubItems[2].Text = str[2];
                        listView1.Items[insertid].SubItems[3].Text = str[3];
                        listView1.Items[insertid].SubItems[4].Text = str[4];
                        listView1.Items[insertid].SubItems[5].Text = str[5];
                    }
                    else
                    {
                        ListViewItem it = new ListViewItem();
                        it.Text = str[0];
                        it.SubItems.Add(str[1]);
                        it.SubItems.Add(str[2]);
                        it.SubItems.Add(str[3]);
                        it.SubItems.Add(str[4]);
                        it.SubItems.Add(str[5]);
                        listView1.Items.Add(it);
                    }
                }
            }
        }

        public void UListUpdateFunction(string[] str)
        {
            if (this.listView2.InvokeRequired)//等待异步 
            {
                FlushClient_UList fc = new FlushClient_UList(UListUpdateFunction);
                // this.Invoke(fc);//通过代理调用刷新方法 
                this.Invoke(fc, new object[1] { str });
            }
            else
            {
                listView2.Items.Clear();

                foreach (string s in str)
                {
                    ListViewItem it = new ListViewItem();
                    it.Text = s;
                    listView2.Items.Add(it);
                }
            }
        }

        public Form1()
        {

            InitializeComponent();
        }


        //接收服务器命令
        private static void RecvCMD()
        {
            while (true)
            {
                string str = HTTP.HttpGet(HTTP.url + "d.php?t=RecvCMD&u=" + HTTP.user + "&p=" + HTTP.pass);
                if (str != null)
                {
                    if (str.IndexOf("stop") != -1)
                    {
                        //这样真的不太好吧呵呵呵
                        if (use == 0)
                        {
                            if (threadScript != null)
                            {
                                threadScript.Abort();
                                threadScript = null;
                                frm.LogUpdateFunction("远程请求停止");
                            }
                        }
                        else
                        {
                            if (threadMA_Client != null)
                            {
                                threadMA_Client.Abort();
                                threadMA_Client = null;
                                frm.LogUpdateFunction("远程请求停止");
                            }
                        }
                    }
                    else if (str.IndexOf("start") != -1)
                    {
                        if (use == 0)
                        {
                            if (threadScript == null)
                            {
                                threadScript = new Thread(Script.Run);
                                threadScript.Start();
                                frm.LogUpdateFunction("远程请求开始");
                            }
                        }
                        else
                        {
                            //这样真的不太好吧呵呵呵
                            if (threadMA_Client == null)
                            {
                                threadMA_Client = new Thread(MA_Client.Do);
                                threadMA_Client.Start();
                                frm.LogUpdateFunction("远程请求开始");
                            }
                        }
                    }
                    else if (str.IndexOf("end") != -1)
                    {
                        Environment.Exit(0);
                    }
                }
                Thread.Sleep(30000);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {


            //
            frmtray = new Tray(this);
            //hehe
            Script.frm = this;
            SysMsg.frm = this;
            frm = this;

            Log.Log_Create();

          
            pictureBox1.Hide();

            if (MessageBox.Show("脱机请选是,BS脚本请选否", "MAH", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
            {
                Tcp.AcceptTcp();
            }

            Thread t = new Thread(RecvCMD);
            t.Start();

            
            //初始化AIRPACP

            devicelist = LibPcapLiveDeviceList.Instance;
            foreach (LibPcapLiveDevice dev in devicelist)
            {
                string s = dev.ToString();
                s = s.Substring(s.IndexOf("FriendlyName: ") + 14, 60);
                comboBox1.Items.Add(s);
            }


            string st = string.Empty;
            string type;

            if (MA.host == "game.ma.mobimon.com.tw:10001")
            {
                type = "\\tw.txt";
            }
            else
            {
                type = "\\cn.txt";
            }

            CardInfo.readData(Application.StartupPath + type);
            using (StreamReader sr = new StreamReader(new FileStream(Application.StartupPath + type, FileMode.Open, FileAccess.Read)))
            {
                while (sr.Peek() > 0)
                {
                    string strr = sr.ReadLine();
                    comboBox2.Items.Add(strr);
                }
                sr.Close();
            }

            //默认数据
            ini = new INI(Application.StartupPath + "\\my.ini");
            string str = ini.IniReadValue("CARD", "card1");
            if (str != null && str != "")
                d1 = str;
            str = ini.IniReadValue("CARD", "card2");
            if (str != null && str != "")
                d2 = str;

            //str = ini.IniReadValue("DEVICE", "netcard");
            //if (str != null && str != "")
            //{
            //    int n = int.Parse(str);
            //    if (n >= 0 && n < devicelist.Count)
            //    {
            //        usedev = devicelist[n];
            //    }
            //}

            str = ini.IniReadValue("AI", "begin");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown2.Minimum && n <= numericUpDown2.Maximum)
                {
                    numericUpDown2.Value = n;
                }
            }

            str = ini.IniReadValue("AI", "end");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown3.Minimum && n <= numericUpDown3.Maximum)
                {
                    numericUpDown3.Value = n;
                }
            }

            str = ini.IniReadValue("AI", "rjxbc");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown4.Minimum && n <= numericUpDown4.Maximum)
                {
                    numericUpDown4.Value = n;
                }
            }

            str = ini.IniReadValue("AI", "rpgbc");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown5.Minimum && n <= numericUpDown5.Maximum)
                {
                    numericUpDown5.Value = n;
                }
            }

            str = ini.IniReadValue("AI", "rpg");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                checkBox1.Checked = n == 0 ? false : true;
            }



            //HKG
            str = ini.IniReadValue("HKG", "card");
            if (str != null && str != "")
                d3 = str;

            str = ini.IniReadValue("HKG", "ap");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown6.Minimum && n <= numericUpDown6.Maximum)
                {
                    numericUpDown6.Value = n;
                }
            }

            str = ini.IniReadValue("HKG", "bc");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown7.Minimum && n <= numericUpDown7.Maximum)
                {
                    numericUpDown7.Value = n;
                }
            }

            str = ini.IniReadValue("HKG", "step");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown8.Minimum && n <= numericUpDown8.Maximum)
                {
                    numericUpDown8.Value = n;
                }
            }

            int i=0;
            while (true)
            {
                   str = ini.IniReadValue("HKG", "area" + i++);
                   if (str != null && str != "")
                   {
                       if (i == 1)
                           listBox1.Items.Clear();
                       listBox1.Items.Add(str);
                   }
                   else
                       break;
            }


            //脱机用户名密码
            str = ini.IniReadValue("TJ", "user");
            if (str != null && str != "")
            {
                MA_Client.login_id = str;
                textBox3.Text = str;
            }
            str = ini.IniReadValue("TJ", "pass");
            if (str != null && str != "")
            {
                MA_Client.login_password = str;
                textBox4.Text = str;
            }


            //是否使用脱机出售

            str = ini.IniReadValue("TJ", "usesell");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                checkBox3.Checked = (n == 0 ? false : true);
            }

            //是否使用脱机领卡

            str = ini.IniReadValue("TJ", "usegetcard");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                checkBox8.Checked = false;
            }


            //是否使用自动探索
            str = ini.IniReadValue("TJ", "useexplore");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                checkBox4.Checked = (n == 0 ? false : true);
            }

            //探索AP阈值
            str = ini.IniReadValue("TJ", "explore_ap");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown9.Minimum && n <= numericUpDown9.Maximum)
                {
                    numericUpDown9.Value = n;
                }
            }

            //探索BC阈值
            str = ini.IniReadValue("TJ", "explore_bc");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown10.Minimum && n <= numericUpDown10.Maximum)
                {
                    numericUpDown10.Value = n;
                }
            }

            //觉醒时间
            str = ini.IniReadValue("TJ", "jx_time_begin");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown14.Minimum && n <= numericUpDown14.Maximum)
                {
                    numericUpDown14.Value = n;
                }
            }

            //觉醒时间
            str = ini.IniReadValue("TJ", "jx_time_end");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown13.Minimum && n <= numericUpDown13.Maximum)
                {
                    numericUpDown13.Value = n;
                }
            }

            //觉醒BC
            str = ini.IniReadValue("TJ", "jx_bc");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown12.Minimum && n <= numericUpDown12.Maximum)
                {
                    numericUpDown12.Value = n;
                }
            }

            //觉醒等待
            str = ini.IniReadValue("TJ", "jx_wait");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown16.Minimum && n <= numericUpDown16.Maximum)
                {
                    numericUpDown16.Value = n;
                }
            }

            //探索BC阈值
            str = ini.IniReadValue("TJ", "explore_bc");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown10.Minimum && n <= numericUpDown10.Maximum)
                {
                    numericUpDown10.Value = n;
                }
            }

            //探索STEP
            str = ini.IniReadValue("TJ", "explore_step");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown11.Minimum && n <= numericUpDown11.Maximum)
                {
                    numericUpDown11.Value = n;
                }
            }

            //是否强制日怪
            str = ini.IniReadValue("TJ", "force_pg");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                checkBox5.Checked = (n == 0 ? false : true);
            }

            //是否自动配卡
            str = ini.IniReadValue("TJ", "useautocard");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                checkBox9.Checked = (n == 0 ? false : true);
            }


            //是否强制探索
            str = ini.IniReadValue("TJ", "force_explore");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                checkBox7.Checked = (n == 0 ? false : true);
            }

            //是否强制探索一个区域
            str = ini.IniReadValue("TJ", "force_explore_area_next");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                checkBox11.Checked = (n == 0 ? false : true);
            }

            //日怪血量
            str = ini.IniReadValue("TJ", "force_pg_bc");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown17.Minimum && n <= numericUpDown17.Maximum)
                {
                    numericUpDown17.Value = n;
                }
            }

            //轮询时间
            str = ini.IniReadValue("TJ", "loop_time");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown18.Minimum && n <= numericUpDown18.Maximum)
                {
                    numericUpDown18.Value = n;
                }
            }


            //刷无名BC
            str = ini.IniReadValue("TJ", "noname_bc");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown19.Minimum && n <= numericUpDown19.Maximum)
                {
                    numericUpDown19.Value = n;
                }
            }

            //自动配卡CP值
            str = ini.IniReadValue("TJ", "useautocard_cp");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown21.Minimum && n <= numericUpDown21.Maximum)
                {
                    numericUpDown21.Value = n;
                }
            }

            //自动领取
            str = ini.IniReadValue("TJ", "useautorewards");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                checkBox13.Checked = (n == 0 ? false : true);
            }

            //无名亚瑟AP
            str = ini.IniReadValue("TJ", "noname_ap");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown20.Minimum && n <= numericUpDown20.Maximum)
                {
                    numericUpDown20.Value = n;
                }
            }

            //脱机出售数量
            str = ini.IniReadValue("TJ", "selln");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n >= numericUpDown15.Minimum && n <= numericUpDown15.Maximum)
                {
                    numericUpDown15.Value = n;
                }
            }

            //脱机出售id
            str = ini.IniReadValue("TJ", "sell");
            if (str != null && str != "")
            {
                string sell = null;
                string[] sz = str.Split('|');
                foreach (string s in sz)
                {
                    listBox2.Items.Add(s);
                    if (s.IndexOf(',') > 0)
                    {
                        if (sell == null)
                            sell = s.Substring(0, s.IndexOf(','));
                        else
                            sell += "," + s.Substring(0, s.IndexOf(','));
                    }
                }
                MA_Client.sell = sell;
            }

            str = ini.IniReadValue("TJ", "force_explore_area");
            if (str != null && str != "")
            {
                textBox6.Text = str;
            }

            str = ini.IniReadValue("TJ", "wake_up_key");
            if (str != null && str != "")
            {
                textBox5.Text = str;
            }

            //脱机卡组信息
            str = ini.IniReadValue("TJ", "card1");
            if (str != null && str != "")
            {
                //设置卡
                string strr, leader;
                cardformat(out strr, out leader, str);

                MA_Client.card1 = strr;
                MA_Client.card1l = leader;
            }

            str = ini.IniReadValue("TJ", "card2");
            if (str != null && str != "")
            {
                //设置卡
                string strr, leader;
                cardformat(out strr, out leader, str);

                MA_Client.card2 = strr;
                MA_Client.card2l = leader;
            }

            str = ini.IniReadValue("TJ", "card3");
            if (str != null && str != "")
            {
                //设置卡
                string strr, leader;
                cardformat(out strr, out leader, str);

                MA_Client.card3 = strr;
                MA_Client.card3l = leader;
            }

            str = ini.IniReadValue("TJ", "card4");
            if (str != null && str != "")
            {
                //设置卡
                string strr, leader;
                cardformat(out strr, out leader, str);

                MA_Client.card4 = strr;
                MA_Client.card4l = leader;
            }

            str = ini.IniReadValue("HKG", "usehkg");
            if (str != null && str != "")
            {
                int n = 0;
                int.TryParse(str, out n);
                if (n == 0)
                    checkBox2.Checked = false;
                else
                    checkBox2.Checked = true;
            }

            //初始化基本参数

            try
            {
                RegistryKey HKLM = Registry.LocalMachine;
                RegistryKey Run = HKLM.OpenSubKey(@"SOFTWARE\BlueStacks\Guests\Android\FrameBuffer\0");
                int Height = int.Parse(Run.GetValue("Height").ToString());
                int Width = int.Parse(Run.GetValue("Width").ToString());
                if (Height != 576 || Width != 1024)
                {
                    if (MessageBox.Show("检测到BlueStacks分辨率不是1024X576，是否进行修改", "notice",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question) == DialogResult.OK)
                    {
                        Run.SetValue("Height", 576);
                        Run.SetValue("Width", 1024);
                        MessageBox.Show("修改成功，请重新退出模拟器重进");
                    }
                    else
                    {
                        Environment.Exit(0);
                    }
                }
            }
            catch (System.ArgumentNullException)
            {
                MessageBox.Show("没有安装BlueStacks?");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("没有安装BlueStacks?，如果使用脱机请无视.");
            }



        }

        private void RunScript()
        {
            int i = 0;
            int px = 0, py = 0;

            MessageBox.Show("开始执行脚本.作者很懒所以会假死= = /你可以干别的事情/，亚瑟王窗体可以覆盖，。。。10分钟之内解决问题");
            while(true)
            {
                SysMsg.MouseEvent(0, 800, 540);
                SysMsg.MouseEvent(1, 800, 540);
                Thread.Sleep(200);

                SysMsg.MouseEvent(0, 800, 380);
                SysMsg.MouseEvent(1, 800, 380);
                Thread.Sleep(200);

                SysMsg.MouseEvent(0, 860, 70);
                SysMsg.MouseEvent(1, 860, 70);
                Thread.Sleep(200);

                SysMsg.MouseEvent(0, 660, 340);
                SysMsg.MouseEvent(1, 660, 340);
                Thread.Sleep(200);

                SysMsg.MouseEvent(0, 740, 217);
                SysMsg.MouseEvent(1, 740, 217);
                Thread.Sleep(200);

               
                    SysMsg.MouseEvent(0, 870, 460);
                    SysMsg.MouseEvent(1, 870, 460);
                    Thread.Sleep(200);


                    if (i % 2 == 0)
                    {
                        SysMsg.MouseEvent(1, 540, 350);
                        SysMsg.MouseEvent(1, 540, 350);
                    }
                    else
                    {
                        SysMsg.MouseEvent(0, 200, 310);
                        SysMsg.MouseEvent(1, 200, 310);
                    }
                //Thread.Sleep(100);
                    Thread.Sleep(350);
                i++;

            }
            MessageBox.Show("执行完成");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int ret = SysMsg.Init();
            if ( ret == 0)
            {
                if (thread == null)
                {
                    button1.Text = "停止";
                    thread = new Thread(new ThreadStart(RunScript));
                    thread.Start();
                }
                else
                {
                    button1.Text = "运行";
                    thread.Abort();
                    thread = null;
                }
            }
            else if (ret == -1)
            {
                MessageBox.Show("未找到模拟器");
            }
            else
            {
                MessageBox.Show("未找到HOOK");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("程序会尝试完全终止，知否继续");
            Process.Start("taskkill", " /im HD-StartLauncher.exe /f");
            Process.Start("taskkill", " /im HD-Service.exe /f");
            Process.Start("taskkill", " /im HD-Frontend.exe /f");
            Process.Start("taskkill", " /im HD-ApkHandler.ex /f");
            Process.Start("taskkill", " /im HD-Agent.exe /f");
            Process.Start("taskkill", " /im HD-RunApp.exe /f");
            Process.Start("taskkill", " /im HD-LogRotatorService.exe /f");
            Process.Start("taskkill", " /im HD-LogRotator.exe /f");
            Process.Start("taskkill", " /im HD-LogCollector.exe /f");
            
            RegistryKey HKLM = Registry.LocalMachine;
            RegistryKey Run = HKLM.CreateSubKey(@"SOFTWARE\BlueStacks\Guests\Android");
            string val = Run.GetValue("BootParameters").ToString();
            Regex r = new Regex("GUID=(?<guid>\\w+([-.]\\w+)*)");
            Match m = r.Match(val);
            string cc = m.Groups["guid"].Value;
            val = val.Replace(cc, System.Guid.NewGuid().ToString("D").ToLower());
            Run.SetValue("BootParameters", val);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("http://tieba.baidu.com/p/2475518048 这里很清楚了不解释 \n要不要自动舔怪呢?\n mengskysama");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //SysMsg.WMQuit();
            notifyIcon1.Visible = false; //任务栏显示图标
            Environment.Exit(0);
            Log.Log_Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            SysMsg.HideMA();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (disbmp == 0)
            {
                disbmp = 1;
                pictureBox1.Show();
            }
            else
            {
                disbmp = 0;
                pictureBox1.Hide();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (d1 == null || d2 == null)
            {
                MessageBox.Show("请先设置卡组");
                return;
            }

            use = 0;

            Script.rjxbegin = (int)numericUpDown2.Value;
            Script.rjxend = (int)numericUpDown3.Value;
            Script.rjxbc = (int)numericUpDown4.Value;
            Script.rpg = checkBox1.Checked ? 1 : 0;
            Script.rpgbc = (int)numericUpDown5.Value;

            string [] areas = new string[listBox1.Items.Count];
            for (int i = 0; i < listBox1.Items.Count; i++)
                areas[i] = (string)listBox1.Items[i];
            Script.hkgarea = areas;
            Script.hkgap = (int)numericUpDown6.Value;
            Script.hkgbc = (int)numericUpDown7.Value;
            Script.usehkg = checkBox2.Checked ? 1 : 0;
            Script.hkgbs = (int)numericUpDown8.Value;

            if (threadScript == null)
            {
                button10.Enabled = false;
                button11.Enabled = false;
                button12.Enabled = false;
                threadCookie = new Thread(GetPacket);
                threadCookie.Start();
                threadScript = new Thread(Script.Run);
                threadScript.Start();
            }
            else
            {
                button10.Enabled = true;
                button11.Enabled = true;
                button12.Enabled = true;
                threadCookie.Abort();
                threadScript.Abort();
                threadScript = null;
            }
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (Script.bmp != null)
            {
                LogUpdateFunction("X=" + e.X.ToString() + " Y=" + e.Y.ToString() + " " + Script.bmp.GetPixel(e.X, e.Y).ToString());
                Clipboard.SetDataObject(this.Text);

                if(usep == 1)
                {
                    if (p1.X == -1)
                    {
                        p1 = new Point(e.X, e.Y);
                    }
                    else
                    {
                        p2 = new Point(e.X, e.Y);
                        int w = p2.X - p1.X, h = p2.Y - p1.Y, x = p1.X, y = p1.Y;
                        Bitmap bm1 = new Bitmap(w, h);
                        for (int i = x; i < x + w; i++)
                            for (int j = y; j < y + h; j++)
                            {
                                Color c = Script.bmp.GetPixel(i, j);
                                if (c.R != 255 && c.G != 255 && c.B !=255)
                                {
                                    c = Color.FromArgb(255,0,0,0);

                                }
                                bm1.SetPixel(i - x, j - y, c);
                            }
                        bm1.Save(x.ToString() + "_" + y.ToString() + "_" + w.ToString() +"_" + h.ToString() + ".bmp");
                        p1.X = -1;
                    }
                }
            }


        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            frmtray.MyShow();
            this.Hide();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            MessageBox.Show("请先将模拟器处于非最小化状态然后，点击确认按钮之后请耐心等待5秒钟。");
            int ret = SysMsg.Init();
            if (ret == -1)
            {
                MessageBox.Show("查找游戏失败，请先运行模拟器");
                return;
            }
            else if (ret == -2)
            {
                MessageBox.Show("没找到HOOK程序...诶?");
                return;
            }
            button10.Enabled = true;
            button11.Enabled = true;
            button12.Enabled = true;
            button20.Enabled = true;
            button6.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            button7.Enabled = true;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            MessageBox.Show("点击按钮之后请耐心等待5秒钟。");
            int ret = SysMsg.Init2();
            if (ret == -1)
            {
                MessageBox.Show("查找游戏失败，请先运行模拟器");
            }
            else if (ret == -2)
            {
                MessageBox.Show("没找到HOOK程序...诶?");
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            int n = comboBox1.SelectedIndex;
            if (n >= 0 && n < devicelist.Count)
            {
                usedev = devicelist[n];
                ini.IniWriteValue("DEVICE", "netcard", n.ToString());
            }
            else
            {
                MessageBox.Show("请从左侧下拉框选择设备");
                return;
            }

        }

        private void GetPacket()
        {
            usedev.Open();
            usedev.Filter = "port 10001";

            try
            {
                while (true)
                {
                    try
                    {
                        usedev.Capture(1);
                        RawCapture c = usedev.GetNextPacket();
                        if (c != null)
                        {
                            byte[] b = c.Data;
                            for (int i = 0; i < b.Length; i++)
                                if (b[i] == 0)
                                    b[i] = 11;

                            string str = System.Text.Encoding.ASCII.GetString(b);
                            if (str.IndexOf("ma.sdo") != -1 && str.IndexOf("&me=1") == -1)
                            {
                                int begin1 = str.IndexOf("Cookie");
                                int begin2 = str.IndexOf("Cookie2");
                                if (begin1 != -1 && begin2 != -1)
                                {
                                    cookie = str.Substring(begin1, str.IndexOf("\r\n", begin1) - begin1);
                                    cookie2 = str.Substring(begin2, str.IndexOf("\r\n", begin2) - begin2);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
            catch (ThreadAbortException)
            {
                usedev.Close();
            }
        }

        //获取设置卡组数据
        private int getcarddata(int cardcount, ref string cookie1, ref string cookie2, ref string main)
        {
            usedev.Open();
            usedev.Filter = "port 10001";
            while (true)
            {
                usedev.Capture(1);
                RawCapture c = usedev.GetNextPacket();
                if (c != null)
                {
                    byte[] b = c.Data;
                    for (int i = 0; i < b.Length; i++)
                        if (b[i] == 0)
                            b[i] = 11;

                    string str = System.Text.Encoding.ASCII.GetString(b);
                    if (str.IndexOf("savedeckcard?cyt=1") != -1)
                    {
                        int begin = str.IndexOf("C=");

                        int begin1 = str.IndexOf("Cookie");

                        int begin2 = str.IndexOf("Cookie2");

                        if (begin != -1 && begin1 != -1 && begin2 != -1)
                        {
                            main = str.Substring(begin, str.IndexOf("%3D%3D%0A", begin) - begin + 9);
                            cookie1 = str.Substring(begin1, str.IndexOf("\r\n", begin1) - begin1);
                            cookie2 = str.Substring(begin2, str.IndexOf("\r\n", begin2) - begin2);
                            usedev.Close();
                            return 1;
                        }
                        else
                        {
                            MessageBox.Show("解析数据失败");
                            usedev.Close();
                            return -1;
                        }
                    }
                }
            }
            return 0;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (usedev == null)
            {
                MessageBox.Show("请先选择网卡");
                return;
            }

            MessageBox.Show("点击确定后，进游戏保存一次卡组，若成功会提示，如果长时间没反应说明选错网卡了= = 自己结束进程吧我不管了");

            getcarddata(1, ref cookie, ref cookie2, ref d1);
            ini.IniWriteValue("CARD", "card1", d1);

            MessageBox.Show("舔怪卡1保存成功");
        }

        private void button12_Click(object sender, EventArgs e)
        {
            MessageBox.Show("请按如下方式操作\n1.进入卡牌菜单配好觉醒卡组,要求至少三张卡组\n2.并且将配卡切换到右边(有自动配卡按钮界面)，并且不能最小化\n3.然后再点本窗口的确定");

            SysMsg.ShotEvent();
            Bitmap bmp = Tcp.GetBitmap();
            Color c = bmp.GetPixel(452,46);
            Color c1 = bmp.GetPixel(333, 46);
            Color c2 = bmp.GetPixel(200, 46);
            if (c.R == 50 || c1.R == 50 || c2.R == 50)
            {
                MessageBox.Show("觉醒组要求至少三张卡组");
                return;
            }

            SysMsg.MouseEvent(0, 800, 57);
            Thread.Sleep(1);
            SysMsg.MouseEvent(1, 800, 57);

            DateTime dt = DateTime.Now;
            getcarddata(1, ref cookie, ref cookie2, ref d2);

            TimeSpan sp = new TimeSpan();
            sp = DateTime.Now - dt;

            if (sp.TotalSeconds < 6)
            {
                ini.IniWriteValue("CARD", "card2", d2);
                MessageBox.Show("卡组保存成功");
            }
            else
            {
                MessageBox.Show("设置失败");
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            try
            {

                System.Diagnostics.Process.Start("http://mengskysama.sinaapp.com/");

            }

            catch (System.ComponentModel.Win32Exception noBrowser)
            {

                if (noBrowser.ErrorCode == -2147467259)

                    MessageBox.Show(noBrowser.Message);

            }

            catch (System.Exception other)
            {

                MessageBox.Show(other.Message);

            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            sleepdelay = (int)numericUpDown1.Value * 1000;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (usep == 0)
                usep = 1;
            else
                usep = 0;
        }

        private void button15_Click(object sender, EventArgs e)
        {
            SysMsg.ShotEvent();
            Script.bmp = Tcp.GetBitmap();
            pictureBox1.Image = Script.bmp;
        }

        private void button16_Click(object sender, EventArgs e)
        {
            //HTTP.HttpPost("http://game1-CBT.ma.sdo.com:10001/connect/app/cardselect/savedeckcard?cyt=1&me=1", "C=4MHuc0bWOf%2FY7JhZF0k2SYx7rw%2B0vUKp%2Fpc%2BN%2B2sFH3zGeMHnPJkwThx3cDEbfVXEAjoR9k2p7EF%0A6D31AT9oQ2PWUTs9pdB0p4%2FNZmMBHEQ%3D%0A&lr=ndXGsLoY3quRNy%2BdTg0aew%3D%3D%0A", "Cookie: S=441e2mbv5t13htn6n6gba5kk67", "Cookie2: $Version=1");
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("AI", "begin", numericUpDown2.Value.ToString());
            Script.rjxbegin = (int)numericUpDown2.Value;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("AI", "end", numericUpDown3.Value.ToString());
            Script.rjxend = (int)numericUpDown3.Value;
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("AI", "rjxbc", numericUpDown4.Value.ToString());
            Script.rjxbc = (int)numericUpDown4.Value;
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("AI", "rpg", (checkBox1.Checked ? 1 : 0).ToString());
            Script.rpg = checkBox1.Checked?1:0;
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("AI", "rpgbc", numericUpDown5.Value.ToString());
            Script.rpgbc = (int)numericUpDown5.Value;
        }

        private void button17_Click(object sender, EventArgs e)
        {
            cookie = "Cookie: s";
            cookie2 = "Cookie2: s";
            d3 = "s";
            MA_Client.Explore(1);
        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("HKG", "ap", numericUpDown6.Value.ToString());
            Script.hkgap = (int)numericUpDown6.Value;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void button18_Click(object sender, EventArgs e)
        {
            listBox1.Items.Add(textBox2.Text);
            for (int i = 0; i < 20; i++)
            {
                if (i < listBox1.Items.Count)
                    ini.IniWriteValue("HKG", "area" + i, (string)listBox1.Items[i]);
                else
                    ini.IniWriteValue("HKG", "area" + i, "");
            }
            string[] areas = new string[listBox1.Items.Count];
            for (int i = 0; i < listBox1.Items.Count; i++)
                areas[i] = (string)listBox1.Items[i];
            Script.hkgarea = areas;

        }

        private void button19_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
            {
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                for (int i = 0; i < 20; i++)
                {
                    if (i < listBox1.Items.Count)
                        ini.IniWriteValue("HKG", "area" + i, (string)listBox1.Items[i]);
                    else
                        ini.IniWriteValue("HKG", "area" + i, "");
                }
                string[] areas = new string[listBox1.Items.Count];
                for (int i = 0; i < listBox1.Items.Count; i++)
                    areas[i] = (string)listBox1.Items[i];
                Script.hkgarea = areas;
            }
        }

        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("HKG", "bc", numericUpDown7.Value.ToString());
            Script.hkgbc = (int)numericUpDown7.Value;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true && frm.d3 == null)
            {
                checkBox2.Checked = false;
                MessageBox.Show("必须先设置打怪卡组!");
                Script.usehkg = 0;
            }
            else
            {
                Script.usehkg = checkBox2.Checked ? 1 : 0;
            }
            ini.IniWriteValue("HKG", "usehkg", (checkBox2.Checked ? 1 : 0).ToString());
        }

        private void button20_Click(object sender, EventArgs e)
        {
            if (usedev == null)
            {
                MessageBox.Show("请先选择网卡");
                return;
            }

            MessageBox.Show("请按如下方式操作\n1.进入卡牌菜单配好卡组,要求至少三张卡组\n2.并且将配卡切换到右边(有自动配卡按钮界面)，并且不能最小化\n3.然后再点本窗口的确定");

            SysMsg.ShotEvent();
            Bitmap bmp = Tcp.GetBitmap();
            Color c = bmp.GetPixel(452, 46);
            Color c1 = bmp.GetPixel(333, 46);
            Color c2 = bmp.GetPixel(200, 46);
            if (c.R == 50 || c1.R == 50 || c2.R == 50)
            {
                MessageBox.Show("要求至少三张卡组");
                return;
            }

            SysMsg.MouseEvent(0, 800, 57);
            Thread.Sleep(1);
            SysMsg.MouseEvent(1, 800, 57);

            DateTime dt = DateTime.Now;
            getcarddata(1, ref cookie, ref cookie2, ref d3);

            TimeSpan sp = new TimeSpan();
            sp = DateTime.Now - dt;

            if (sp.TotalSeconds < 6)
            {
                ini.IniWriteValue("HKG", "card", d3);
                MessageBox.Show("卡组保存成功");
            }
            else
            {
                MessageBox.Show("卡组设置失败");
            }
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void numericUpDown8_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("HKG", "step", numericUpDown8.Value.ToString());
            Script.hkgbs = (int)numericUpDown8.Value;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

        }

        private void button21_Click(object sender, EventArgs e)
        {
            use = 2;

            if (textBox3.Text == "" && textBox4.Text != "")
            {
                MessageBox.Show("请输入用户名密码");
                return;
            }

            if (MA_Client.card1 == null || MA_Client.card2 == null || MA_Client.card3 == null)
            {
                MessageBox.Show("必须卡组全部设置");
                return;
            }

            if (textBox3.Text.Substring(textBox3.Text.Length - 2) != HTTP.login_last2)
            {
                MessageBox.Show("和绑定账号不一致!");
                return;
            }


            MA_Client.usesell = (checkBox3.Checked ? 1 : 0);
            MA_Client.useselln = (int)numericUpDown15.Value;
            MA_Client.useexplore = (checkBox4.Checked ? 1 : 0);
            MA_Client.explore_ap = (int)numericUpDown9.Value;
            MA_Client.explore_bc = (int)numericUpDown10.Value;
            MA_Client.explore_step = (int)numericUpDown11.Value;
            MA_Client.jx_time_begin = (int)numericUpDown14.Value;
            MA_Client.jx_time_end = (int)numericUpDown13.Value;
            MA_Client.jx_bc = (int)numericUpDown12.Value;
            MA_Client.jx_wait = (int)numericUpDown16.Value;
            MA_Client.force_pg = (checkBox5.Checked ? 1 : 0);
            MA_Client.force_pg_bc = (int)numericUpDown17.Value;
            MA_Client.loop_time = (int)numericUpDown18.Value;
            MA_Client.usegetcard = (checkBox8.Checked ? 1 : 0);
            MA_Client.useautocard = (checkBox9.Checked ? 1 : 0);
            MA_Client.force_explore_area = textBox6.Text;
            MA_Client.wake_up_key = textBox5.Text;
            MA_Client.force_explore_area_next = (checkBox11.Checked ? 1 : 0);
            MA_Client.noname_bc = (int)numericUpDown19.Value;
            MA_Client.noname_ap = (int)numericUpDown20.Value;
            MA_Client.useautocard_cp = (int)numericUpDown21.Value;
            MA_Client.yinzi_use_tea_bc = (checkBox12.Checked ? 1 : 0);
            MA_Client.useautorewards = (checkBox13.Checked ? 1 : 0);

            if (threadMA_Client != null)
            {
                threadMA_Client.Abort();
                threadMA_Client = null;
                frm.LogUpdateFunction("停止循环线程");
                button29.Enabled = true;
            }
            else
            {
                threadMA_Client = new Thread(MA_Client.Do);
                threadMA_Client.Start();
                frm.LogUpdateFunction("开始循环线程");
                button29.Enabled = false;
                return;
            }

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void button23_Click(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex >= 0)
            {
                try
                {
                    string s = comboBox2.Items[comboBox2.SelectedIndex].ToString();
                    string num = s.Substring(0, s.IndexOf(',') + 1);

                    for (int i = 0; i < listBox2.Items.Count; i++)
                    {
                        string s2 = listBox2.Items[i].ToString();
                        string num2 = s2.Substring(0, s2.IndexOf(',') + 1);
                        if (num2 == num)
                        {
                            listBox2.Items.RemoveAt(i);
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("格式非法");
                }

                //保存
                listBox2.Items.Add(comboBox2.Items[comboBox2.SelectedIndex]);
                string str = null;
                for (int i = 0; i < listBox2.Items.Count; i++)
                {
                    if (str != null && listBox2.Items.Count>1)
                        str += "|" + listBox2.Items[i];
                    else
                        str = listBox2.Items[i].ToString();
                }
                ini.IniWriteValue("TJ", "sell", str);

                //更新
                string sell = null;
                string[] sz = str.Split('|');
                foreach (string s in sz)
                {
                    if (s.IndexOf(',') > 0)
                    {
                        if (sell == null)
                            sell = s.Substring(0, s.IndexOf(','));
                        else
                            sell += "," + s.Substring(0, s.IndexOf(','));
                    }
                }
                MA_Client.sell = sell;
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex >= 0)
            {
                listBox2.Items.RemoveAt(listBox2.SelectedIndex);
                string str = null;
                for (int i = 0; i < listBox2.Items.Count; i++)
                {
                    if (str != null && listBox2.Items.Count > 1)
                        str += "|" + listBox2.Items[i];
                    else
                        str = listBox2.Items[i].ToString();
                }
                ini.IniWriteValue("TJ", "sell", str);

                //更新
                string sell = null;
                string[] sz = str.Split('|');
                foreach (string s in sz)
                {
                    if (s.IndexOf(',') > 0)
                    {
                        if (sell == null)
                            sell = s.Substring(0, s.IndexOf(','));
                        else
                            sell += "," + s.Substring(0, s.IndexOf(','));
                    }
                }
                MA_Client.sell = sell;
            }
        }

        private void button24_Click(object sender, EventArgs e)
        {
            putinselllist(1);
            MessageBox.Show("= =!! 请注意切尔利也在列表里哦!请手动删除!");
        }

        private void putinselllist(int star)
        {
            string str;

            for (int i = 0; i < comboBox2.Items.Count; i++)
            {
                str = comboBox2.Items[i].ToString();
                if (str.Substring(str.LastIndexOf(',') - 1, 1) == star.ToString())
                {
                    if (str.IndexOf(',') > 0)
                    {
                        string sid = str.Substring(0, str.IndexOf(','));
                        int flag = 0;
                        //判断是否包含此ID的过滤
                        for (int j = 0; j < listBox2.Items.Count; j++)
                        {
                            string ssid = listBox2.Items[j].ToString();
                            if (ssid.IndexOf(',') > 0)
                            {
                                if (ssid.Substring(0, ssid.IndexOf(',')) == sid)
                                {
                                    flag = 1;
                                    break;
                                }
                            }
                        }
                        if (flag == 0)
                            listBox2.Items.Add(str);
                    }
                }
            }

            str = null;
            for (int i = 0; i < listBox2.Items.Count; i++)
            {
                if (str != null && listBox2.Items.Count > 1)
                    str += "|" + listBox2.Items[i];
                else
                    str = listBox2.Items[i].ToString();
            }
            ini.IniWriteValue("TJ", "sell", str);

            //更新
            if (str != null)
            {
                string sell = null;
                string[] sz = str.Split('|');
                foreach (string s in sz)
                {
                    if (s.IndexOf(',') > 0)
                    {
                        if (sell == null)
                            sell = s.Substring(0, s.IndexOf(','));
                        else
                            sell += "," + s.Substring(0, s.IndexOf(','));
                    }
                }
                MA_Client.sell = sell;
            }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            putinselllist(2);
            MessageBox.Show("= =!! 请注意狼娘也在列表里哦!请手动删除!");
        }

        private void button22_Click(object sender, EventArgs e)
        {

        }

        private void button29_Click(object sender, EventArgs e)
        {
            try
            {
                MA.login(MA_Client.login_id, MA_Client.login_password);

                comboBox3.Items.Clear();

                foreach (MA.MA_Card card in MA.cardlst)
                {
                    if (checkBox6.Checked == true && card.lv < 3)
                    {
                        continue;
                    }
                    string cardname = "[未知(可用)]";
                    string id = card.master_card_id.ToString() + ",";
                    foreach (string c in comboBox2.Items)
                    {
                        if (c.IndexOf(id) == 0)
                        {
                            int bg = c.IndexOf(",");
                            cardname = c.Substring(bg + 1, c.Length - bg - 1);
                        }
                    }
                    comboBox3.Items.Add(cardname + " Lv" + card.lv + " ATK:" + card.power + " HP:"  + card.master_card_id +"@" + card.serial_id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button30_Click(object sender, EventArgs e)
        {
            string cardstr = c1.Text + "|" + c2.Text + "|" + c3.Text + "|" + c4.Text + "|" + c5.Text + "|" + c6.Text + "|" + c7.Text + "|" + c8.Text + "|" + c9.Text + "|" + c10.Text + "|" + c11.Text + "|" + c12.Text;
            
            ini.IniWriteValue("TJ", "card" + editcard, cardstr);

            //设置卡
            string str, leader;
            cardformat(out str, out leader, cardstr);

            if (editcard == 1)
            {
                MA_Client.card1 = str;
                MA_Client.card1l = leader;
            }
            else if (editcard == 2)
            {
                MA_Client.card2 = str;
                MA_Client.card2l = leader;
            }
            else if (editcard == 3)
            {
                if (c1.Text == "" || c2.Text == "")
                {
                    MessageBox.Show("保存失败,探索卡组至少2卡");
                    return;
                }
                MA_Client.card3 = str;
                MA_Client.card3l = leader;
            }

            MessageBox.Show("保存成功");

        }

        //WSVFW,222|ERGER,555 ====> 222,555,empty,empty,empty,empty,empty,empty,empty,empty,empty
        private void cardformat(out string card, out string leader, string savestr)
        {
            string str = null;
            string lr = null;
            string[] sz = savestr.Split('|');
            for (int i = 0; i < sz.Length; i++)
            {
                string s = sz[i];

                if (s != "")
                {
                    s = s.Substring(s.IndexOf('@') + 1);
                    if (lr == null)
                        lr = s;
                }
                else
                {
                    s = "empty";
                }

                if (str != null && sz.Length > 1)
                    str += "," + s;
                else
                    str = s;
            }
            card = str;
            leader = lr;
        }

        private void textBox5_MouseClick(object sender, MouseEventArgs e)
        {

        }


        private void textBox7_TextChanged(object sender, EventArgs e)
        {

        }



        private void c1_MouseClick(object sender, MouseEventArgs e)
        {
            if (c1.Text == "")
                c1.Text = comboBox3.Text;
            else
                c1.Text = "";
        }

        private void c2_MouseClick(object sender, MouseEventArgs e)
        {
            if (c2.Text == "")
                c2.Text = comboBox3.Text;
            else
                c2.Text = "";
        }

        private void c3_MouseClick(object sender, MouseEventArgs e)
        {
            if (c3.Text == "")
                c3.Text = comboBox3.Text;
            else
                c3.Text = "";
        }

        private void c4_MouseClick(object sender, MouseEventArgs e)
        {
            if (c4.Text == "")
                c4.Text = comboBox3.Text;
            else
                c4.Text = "";
        }

        private void c5_MouseClick(object sender, MouseEventArgs e)
        {
            if (c5.Text == "")
                c5.Text = comboBox3.Text;
            else
                c5.Text = "";
        }

        private void c6_MouseClick(object sender, MouseEventArgs e)
        {
            if (c6.Text == "")
                c6.Text = comboBox3.Text;
            else
                c6.Text = "";
        }

        private void c7_MouseClick(object sender, MouseEventArgs e)
        {
            if (c7.Text == "")
                c7.Text = comboBox3.Text;
            else
                c7.Text = "";
        }

        private void c8_MouseClick(object sender, MouseEventArgs e)
        {
            if (c8.Text == "")
                c8.Text = comboBox3.Text;
            else
                c8.Text = "";
        }

        private void c9_MouseClick(object sender, MouseEventArgs e)
        {
            if (c9.Text == "")
                c9.Text = comboBox3.Text;
            else
                c9.Text = "";
        }

        private void c10_MouseClick(object sender, MouseEventArgs e)
        {
            if (c10.Text == "")
                c10.Text = comboBox3.Text;
            else
                c10.Text = "";
        }

        private void c11_MouseClick(object sender, MouseEventArgs e)
        {
            if (c11.Text == "")
                c11.Text = comboBox3.Text;
            else
                c11.Text = "";
        }

        private void c12_MouseClick(object sender, MouseEventArgs e)
        {
            if (c12.Text == "")
                c12.Text = comboBox3.Text;
            else
                c12.Text = "";
        }

        private void button22_Click_1(object sender, EventArgs e)
        {
            editcard = 1;
            readcard(editcard);
            button30.Enabled = true;
            button32.Enabled = true;
        }

        private void button27_Click(object sender, EventArgs e)
        {
            editcard = 3;
            readcard(editcard);
            button30.Enabled = true;
            button32.Enabled = true;
        }

        private void button28_Click(object sender, EventArgs e)
        {
            if (c1.Text == null || c2.Text == null || c3.Text == null)
            {
                MessageBox.Show("保存失败,觉醒卡组至少3要求");
                return;
            }
            editcard = 2;
            readcard(editcard);
            button30.Enabled = true;
            button32.Enabled = true;
        }

        private void readcard(int editcard)
        {
            c1.Text = "";
            c2.Text = "";
            c3.Text = "";
            c4.Text = "";
            c5.Text = "";
            c6.Text = "";
            c7.Text = "";
            c8.Text = "";
            c9.Text = "";
            c10.Text = "";
            c11.Text = "";
            c12.Text = "";
            string str = ini.IniReadValue("TJ", "card" + editcard);
            if (str != null && str != "")
            {
                string[] sz = str.Split('|');
                if (sz.Length == 12)
                {
                    c1.Text = sz[0];
                    c2.Text = sz[1];
                    c3.Text = sz[2];
                    c4.Text = sz[3];
                    c5.Text = sz[4];
                    c6.Text = sz[5];
                    c7.Text = sz[6];
                    c8.Text = sz[7];
                    c9.Text = sz[8];
                    c10.Text = sz[9];
                    c11.Text = sz[10];
                    c12.Text = sz[11];
                }
            }
        }

        private void groupBox6_Enter(object sender, EventArgs e)
        {

        }

        private void button31_Click(object sender, EventArgs e)
        {
            if (textBox3.Text == "" && textBox4.Text != "")
            {
                MessageBox.Show("请输入用户名密码");
                return;
            }

            if (textBox3.Text.Substring(textBox3.Text.Length-2) != HTTP.login_last2)
            {
                MessageBox.Show("和绑定账号不一致!");
                //return;
            }

            MA_Client.login_id = textBox3.Text;//textBox3.Text;
            MA_Client.login_password = textBox4.Text;//textBox4.Text;
            ini.IniWriteValue("TJ", "user", textBox3.Text);
            ini.IniWriteValue("TJ", "pass", textBox4.Text);


            try
            {
                MA.login(MA_Client.login_id, MA_Client.login_password, 0);
                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void label17_Click(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "usesell", (checkBox3.Checked ? 1 : 0).ToString());
            MA_Client.usesell = (checkBox3.Checked ? 1 : 0);
        }

        private void numericUpDown15_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "selln", numericUpDown15.Value.ToString());
            MA_Client.useselln = (int)numericUpDown15.Value;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "useexplore", (checkBox4.Checked ? 1 : 0).ToString());
            MA_Client.useexplore = (checkBox4.Checked ? 1 : 0);
        }

        private void numericUpDown9_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "explore_ap", numericUpDown9.Value.ToString());
            MA_Client.explore_ap = (int)numericUpDown9.Value;
        }

        private void numericUpDown10_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "explore_bc", numericUpDown10.Value.ToString());
            MA_Client.explore_bc = (int)numericUpDown10.Value;
        }

        private void numericUpDown11_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "explore_step", numericUpDown11.Value.ToString());
            MA_Client.explore_step = (int)numericUpDown11.Value;
        }

        private void numericUpDown14_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "jx_time_begin", numericUpDown14.Value.ToString());
            MA_Client.jx_time_begin = (int)numericUpDown14.Value;
        }

        private void numericUpDown13_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "jx_time_end", numericUpDown13.Value.ToString());
            MA_Client.jx_time_end = (int)numericUpDown13.Value;
        }

        private void numericUpDown12_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "jx_bc", numericUpDown12.Value.ToString());
            MA_Client.jx_bc = (int)numericUpDown12.Value;
        }

        private void numericUpDown16_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "jx_wait", numericUpDown16.Value.ToString());
            MA_Client.jx_wait = (int)numericUpDown16.Value;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "force_pg", (checkBox5.Checked ? 1 : 0).ToString());
            MA_Client.force_pg = (checkBox5.Checked ? 1 : 0);
        }

        private void numericUpDown17_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "force_pg_bc", numericUpDown17.Value.ToString());
            MA_Client.force_pg_bc = (int)numericUpDown17.Value;
        }

        private void button32_Click(object sender, EventArgs e)
        {
            try
            {
                string cardstr = c1.Text + "|" + c2.Text + "|" + c3.Text + "|" + c4.Text + "|" + c5.Text + "|" + c6.Text + "|" + c7.Text + "|" + c8.Text + "|" + c9.Text + "|" + c10.Text + "|" + c11.Text + "|" + c12.Text;

                ini.IniWriteValue("TJ", "card" + editcard, cardstr);

                //设置卡
                string str, leader;
                cardformat(out str, out leader, cardstr);

                if (editcard == 1)
                {
                    MA_Client.card1 = str;
                    MA_Client.card1l = leader;
                    MA.cardselect_savedeckcard(str, leader);
                }
                else if (editcard == 2)
                {
                    MA_Client.card2 = str;
                    MA_Client.card2l = leader;
                    MA.cardselect_savedeckcard(str, leader);
                }
                else if (editcard == 3)
                {
                    if (c1.Text == "" || c2.Text == "")
                    {
                        MessageBox.Show("保存失败,探索卡组至少2卡");
                        return;
                    }
                    MA_Client.card3 = str;
                    MA_Client.card3l = leader;
                    MA.cardselect_savedeckcard(str, leader);
                }
                else if (editcard == 4)
                {
                    MA_Client.card4 = str;
                    MA_Client.card4l = leader;
                    MA.cardselect_savedeckcard(str, leader);
                }
                MessageBox.Show("保存完成,目测设置成功,请登陆游戏确认!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void numericUpDown18_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "loop_time", numericUpDown18.Value.ToString());
            MA_Client.loop_time = (int)numericUpDown18.Value;
        }

        private void label19_Click(object sender, EventArgs e)
        {

        }

        private void label18_Click(object sender, EventArgs e)
        {

        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "force_explore", (checkBox7.Checked ? 1 : 0).ToString());
            MA_Client.force_explore = (checkBox7.Checked ? 1 : 0);
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {

                this.Visible = false;   //隐藏窗体
                notifyIcon1.Visible = true; //任务栏显示图标
                notifyIcon1.ShowBalloonTip(3500, "提示", "双击恢复窗口", ToolTipIcon.Info); //出显汽泡提示，可以不用
                this.ShowInTaskbar = false; //从状态栏中隐藏
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            this.Visible = true; //显示窗体
            this.ShowInTaskbar = true; //从状态栏中隐藏
            WindowState = FormWindowState.Normal;  //恢复窗体默认大小
            this.Show();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked == true && checkBox3.Checked == true)
            {
                ini.IniWriteValue("TJ", "usegetcard", (checkBox8.Checked ? 1 : 0).ToString());
                MA_Client.usegetcard = (checkBox8.Checked ? 1 : 0);
            }
            else if (checkBox8.Checked == true && checkBox3.Checked == false)
            {
                MessageBox.Show("必须勾选出售卡牌才能使用!并且注意卡牌满后无法再进行战斗,确保出售列表包含所有垃圾卡!");
                MA_Client.usegetcard = (checkBox8.Checked ? 1 : 0);
            }
            MA_Client.usegetcard = (checkBox8.Checked ? 1 : 0);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "useautocard", (checkBox9.Checked ? 1 : 0).ToString());
            MA_Client.useautocard = (checkBox9.Checked ? 1 : 0);
        }

        private void button33_Click(object sender, EventArgs e)
        {
            MA_Client.CardDeck deck = MA_Client.SelectCard(85, 0, 50000, 450000, 50);
        }

        private void button33_Click_1(object sender, EventArgs e)
        {
            MA_Client.tea_bc = 1;
            MessageBox.Show("下次轮询会进行嗑红,请耐心等待");
            //MA.noname0048();
            //MessageBox.Show("你妈逼");
            //MA_Client.CardDeck deck = MA_Client.SelectCard(85, 0, 50000, 450000, 50);
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            MA_Client.force_explore_area = textBox6.Text;
            ini.IniWriteValue("TJ", "force_explore_area", textBox6.Text);
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            MA_Client.wake_up_key = textBox5.Text;
            ini.IniWriteValue("TJ", "wake_up_key", textBox5.Text);
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "yinzi", (checkBox10.Checked ? 1 : 0).ToString());
            MA_Client.yinzi = (checkBox10.Checked ? 1 : 0);
        }

        private void label31_Click(object sender, EventArgs e)
        {

        }

        private void button34_Click(object sender, EventArgs e)
        {
            MessageBox.Show("强烈推荐使用妹抖必胜，破满狼娘球娘好像也OK？");
            editcard = 4;
            readcard(editcard);
            button30.Enabled = true;
            button32.Enabled = true;
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "force_explore_area_next", (checkBox11.Checked ? 1 : 0).ToString());
            MA_Client.force_explore_area_next = (checkBox11.Checked ? 1 : 0);
        }

        private void numericUpDown19_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "noname_bc", numericUpDown19.Value.ToString());
            MA_Client.noname_bc = (int)numericUpDown19.Value;
        }

        private void numericUpDown20_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "noname_ap", numericUpDown20.Value.ToString());
            MA_Client.noname_ap = (int)numericUpDown20.Value;
        }

        private void button35_Click(object sender, EventArgs e)
        {
            MA_Client.tea_ap = 1;
            MessageBox.Show("下次轮询会进行嗑绿,请耐心等待");
        }

        private void numericUpDown21_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "useautocard_cp", numericUpDown21.Value.ToString());
            MA_Client.useautocard_cp = (int)numericUpDown21.Value;
        }

        private void button36_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "yinzi_use_tea_bc", (checkBox12.Checked ? 1 : 0).ToString());
            MA_Client.yinzi_use_tea_bc = (checkBox12.Checked ? 1 : 0);
        }

        private void numericUpDown22_ValueChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "noname_cost", numericUpDown22.Value.ToString());
            MA_Client.noname_cost = (int)numericUpDown22.Value;
        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)
        {
            ini.IniWriteValue("TJ", "useautorewards", (checkBox13.Checked ? 1 : 0).ToString());
            MA_Client.useautorewards = (checkBox13.Checked ? 1 : 0);
        }

        private void button37_Click(object sender, EventArgs e)
        {
            putinselllist(3);
            MessageBox.Show("= =!! 请注意 妹抖,超级切尔利 也在列表里哦!请手动删除!");
        }

    }
}
