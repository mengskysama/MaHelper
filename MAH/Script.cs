using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Resources;

namespace MAH
{
    class ScriptImg
    {
        public Bitmap bm;
        public int x;
        public int y;

        public ScriptImg(Bitmap bm, int x, int y)
        {
            this.bm = bm;
            this.x = x;
            this.y = y;
        }
    }

    class Script
    {
        private static List<string> lstScript = new List<string>();
        
        //当前屏幕
        public static Bitmap bmp;

        //当前血量
        static int ap;
        static int bc;

        //脚本
        public static string scr;

        //日觉醒BC
        public static int rjxbc;
        public static int rpgbc;
        public static int rpg;
        //日觉醒时间
        public static int rjxbegin;
        public static int rjxend;
        //黑科技
        public static int usehkg;
        public static int hkgbc;
        public static int hkgap;
        public static int hkgbs;

        public static string[] hkgarea;

        static DateTime time_HKG = DateTime.Now.AddHours(-1.0);
        static DateTime time_SendAPBC = DateTime.Now.AddHours(-1.0);

        public static Dictionary<string, ScriptImg> dicImg = new Dictionary<string, ScriptImg>();
        public static Dictionary<string, DateTime> dicDimDt = new Dictionary<string, DateTime>();

        public static Form1 frm;

        public static bool PointColorEqual(int x, int y, int r, int g, int b)
        {
            Color pixel = bmp.GetPixel(x, y); 
            return (pixel.R == r && pixel.G == g && pixel.B == b);
        }

        public static bool PointColorNotEqual(int x, int y, int r, int g, int b)
        {
            return !PointColorEqual(x, y, r, g, b);
        }

        public static bool PointColorAEqual(int x, int y, int r, int g, int b)
        {
            Color pixel = bmp.GetPixel(x, y);
            return (Math.Abs(pixel.R - r) < 20 && Math.Abs(pixel.G - g) < 20 && Math.Abs(pixel.B - b) < 20);
        }

        public static bool PointColorNotAEqual(int x, int y, int r, int g, int b)
        {
            return !PointColorAEqual(x, y, r, g, b);
        }


        
        //将日志传送到服务器
        private static void SendLog(object str)
        {
            string str1 = (string)str;

        }

        public static string make_chk(string str)
        {
            return HTTP.EncryptMD5(HTTP.EncryptMD5(str + str)).Substring(0, 5);
        }

        //向服务器发送日志
        public static void Parse_SendLog(ref int index)
        {
            Thread t = new Thread(new ParameterizedThreadStart(SendLog));
        }
        //设置卡组
        private static void SendCARD(object i)
        {
            int n = (int)i;
            if (n == 1)
            {
                HTTP.HttpPost("http://game1-CBT.ma.sdo.com:10001/connect/app/cardselect/savedeckcard?cyt=1&me=1", frm.d1, frm.cookie, frm.cookie2);
            }
            else if( n == 2)
            {
                HTTP.HttpPost("http://game1-CBT.ma.sdo.com:10001/connect/app/cardselect/savedeckcard?cyt=1&me=1", frm.d2, frm.cookie, frm.cookie2);
            }
        }

        //向游戏付服务设置卡
        //SetCard 1
        //SetCard 2
        public static void Parse_SetCard(ref int index)
        {
            string str = lstScript[index];

            if (str.IndexOf("SetCard 1") == 0)
            {
                if (frm.d2 == null || frm.cookie == null || frm.cookie2 == null)
                {
                    frm.LogUpdateFunction("Debug !!Cooke Null!!");
                }
                Thread t = new Thread(new ParameterizedThreadStart(SendCARD));
                t.Start(1);
                index++;
            }
            else if (str.IndexOf("SetCard 2") == 0)
            {
                if (frm.d2 == null || frm.cookie == null || frm.cookie2 == null)
                {
                    frm.LogUpdateFunction("Debug !!Cooke Null!!");
                }
                else
                {
                    Thread t = new Thread(new ParameterizedThreadStart(SendCARD));
                    t.Start(2);
                    index++;
                }
            }
        }


        //将APBC发送给服务器
        public static void SendAPBC(object str)
        {
            string str1 = (string)str;
            string ap = str1.Substring(0, str1.IndexOf(','));
            string bc = str1.Substring(ap.Length+1);
            HTTP.HttpGet(HTTP.url + "d.php?t=APBC&u=" + HTTP.user + "&p=" + HTTP.pass + "&AP=" + ap + "&BC=" + bc);
        }

        //向服务器发送apbc
        //SendAPBC
        public static void Parse_SendAPBC(ref int index)
        {
            string str = lstScript[index];

            if (str.IndexOf("SendAPBC") == 0 )
            {
                TimeSpan sp = new TimeSpan();
                sp = DateTime.Now - time_SendAPBC;
                //每3分钟更新数据
                if (sp.TotalSeconds > 60 * 3)
                {
                    time_SendAPBC = DateTime.Now;
                    Thread t = new Thread(new ParameterizedThreadStart(SendAPBC));
                    string apbc = ap.ToString() + "," + bc.ToString();
                    t.Start(apbc);
                }
                index++;
            }
        }

        //舔怪计数器
        public static void SendCnt()
        {
            HTTP.HttpGet(HTTP.url + "d.php?t=Cnt&u=" + HTTP.user + "&p=" + HTTP.pass);
        }

        //向服务器发送舔怪信息
        public static void Parse_SendCnt(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("SendCnt") == 0)
            {
                Thread t = new Thread(SendCnt);
                t.Start();
                index++;
            }
        }


        //后台AP
        private static void DoHKG()
        {
            MA_Client.Explore(hkgbs);
        }
        //解析黑科技
        public static void Parse_HKG(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("HKG") == 0)
            {
                if (usehkg == 1 && ap > hkgap && bc > hkgbc && frm.cookie != null)
                {
                    TimeSpan sp = new TimeSpan();
                    sp = DateTime.Now - time_HKG;
                    if (sp.TotalSeconds > 240)
                    {
                        time_HKG = DateTime.Now;
                        frm.LogUpdateFunction("满足黑科技运行条件开始运行脚本延迟执行");
                        Thread t = new Thread(DoHKG);
                        t.Start();
                        Thread.Sleep(1000 * hkgbs + 10000);
                    }
                    
                }
                index++;
            }
        }


        //解析坑货
        public static void Parse_Empty(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("//") == 0 || str.IndexOf("#") == 0 || str.IndexOf(":") == 0 || str.IndexOf("'") == 0 || str.Length == 0)
            {
                index++;
            }
        }

        //保存当前坐标图像为变量timeimg
        //SaveImg timeimg X=4 Y=6 W=10 H=5
        public static void Parse_SaveImg(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("SaveImg") == 0)
            {
                Regex reg = new Regex("SaveImg (?<name>\\w+([-.]\\w+)*).X=(?<x>\\w+([-.]\\d+)*).Y=(?<y>\\w+([-.]\\d+)*).W=(?<w>\\w+([-.]\\d+)*).H=(?<h>\\w+([-.]\\d+)*)");
                Match m = reg.Match(str);
                string name = m.Groups["name"].Value;
                int x = int.Parse(m.Groups["x"].Value);
                int y = int.Parse(m.Groups["y"].Value);
                int w = int.Parse(m.Groups["w"].Value);
                int h = int.Parse(m.Groups["h"].Value);

                Bitmap bm = new Bitmap(w, h);
                for(int i = x; i<x+w; i++)
                    for(int j = y; j<y+h; j++)
                        bm.SetPixel(i-x,j-y,bmp.GetPixel(i,j));

                if (dicImg.ContainsKey(name) == true)
                    dicImg.Remove(name);

                dicImg.Add(name, new ScriptImg(bm, x, y));

                index++;
            }
        }

        //调试输出
        public static void Parse_Debug(ref int index)
        {
            //委托
            //好吧先不用了..
            string str = lstScript[index];
            if (str.IndexOf("Debug") == 0)
            {
                frm.LogUpdateFunction(str);
                index++;
            }
        }

        //延迟
        //Sleep 2000
        public static void Parse_Sleep(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("Sleep") == 0)
            {
                Regex reg = new Regex("Sleep (?<sleep>\\w+([-.]\\d+)*)");
                Match m = reg.Match(str);
                int sleep = int.Parse(m.Groups["sleep"].Value);
                Thread.Sleep(sleep + frm.sleepdelay);
                index++;
            }
        }

        //更新图像
        //UpdataImg
        public static void Parse_UpdataImg(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("UpdataImg") == 0)
            {
                SysMsg.ShotEvent();
                bmp = Tcp.GetBitmap();
                frm.ImgUpdateFunction(bmp);
                index++;
            }
        }

        //更新血量
        //UpdateBloodImg
        public static void Parse_UpdateBloodImg(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("UpdateBloodImg") == 0)
            {
                int x = 87;
                int y = 550;//548
                int w = 162;
                int h = 5;//16
                Bitmap bm1 = new Bitmap(w, h);
                for (int i = x; i < x + w; i++)
                    for (int j = y; j < y + h; j++)
                    {
                        bm1.SetPixel(i - x, j - y, bmp.GetPixel(i, j));
                    }

                for(int i=0;i<162;i++)
                {
                    Color r = bm1.GetPixel(i, 0);
                    if (r.G < 100)
                    {
                        ap = (int)((100 / 160.0) * i);
                        break;
                    }
                }

                x = 271;
                 y = 550;
                w = 162;
                 h = 5;
                Bitmap bm2 = new Bitmap(w, h);
                for (int i = x; i < x + w; i++)
                    for (int j = y; j < y + h; j++)
                        bm2.SetPixel(i - x, j - y, bmp.GetPixel(i, j));

                for (int i = 0; i < 162; i++)
                {
                    Color r = bm2.GetPixel(i, 0);
                    if (r.R < 100)
                    {
                        bc = (int)((100 / 160.0) * i);
                        break;
                    }
                }

                frm.frmtray.ImgUpdateFunction(bm1, bm2);
                index++;
            }
        }

        //跳转
        //Goto Label
        public static void Parse_Goto(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("Goto") == 0)
            {
                Regex reg = new Regex("Goto (?<goto>\\w+([-.]\\d+)*)");
                Match m = reg.Match(str);
                string st = m.Groups["goto"].Value;

                for (int i = 0; i < lstScript.Count - 1; i++ )
                {
                    if (lstScript[i].IndexOf(":" + st) == 0)
                    {
                        index = i;
                        return;
                    }
                }
            }
        }

        //定义变量
        public static void Parse_Dim(ref int index)
        {
            string str = lstScript[index];
            //Var TIME time 定义一个变量time为当前时间
            //Var IF TIME time IN 5 判断时间time与当前时间差是否小于5秒
            if (str.IndexOf("Var TIME") == 0)
            {
                if (str.IndexOf("TIME") != -1)
                {
                    Regex reg = new Regex("TIME (?<name>\\w+([-.]\\w+)*)");
                    Match m = reg.Match(str);
                    string name = m.Groups["name"].Value;
                    DateTime dt = new DateTime();
                    dt = DateTime.Now;
                    if (dicDimDt.ContainsKey(name))
                    {
                        dicDimDt.Remove(name);
                    }
                    dicDimDt.Add(name, dt);
                    index++;
                    return;
                }
            }
            else if (str.IndexOf("Var IF TIME") == 0)
                {
                    Regex reg = new Regex("TIME (?<name>\\w+([-.]\\w+)*).*.IN (?<in>\\w+([-.]\\d+)*)");
                    Match m = reg.Match(str);
                    string name = m.Groups["name"].Value;
                    int n = int.Parse(m.Groups["in"].Value);

                    int flag = 0;

                    if (dicDimDt.ContainsKey(name))
                    {
                        //定义过
                        TimeSpan sp = new TimeSpan();
                        sp = DateTime.Now - dicDimDt[name];
                        if (sp.TotalSeconds < n)
                        {
                            //满足
                            flag = 1;
                        }
                    }

                    if (flag == 1)
                    {
                        index++;
                        return;
                    }
                    else
                    {
                        index++;
                        Parse_JumpIF(ref index);
                        return;
                    }
            }
        }

        //条件等待
        //WaitColor
        //失败执行语句
        //循环执行语句Touch X= Y=
        //....
        //若超时未满足则执行下1句，第二句为每秒循环执行,否则下3句开始
        public static void Parse_WaitColor(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("WaitColor") == 0)
            {
                int t,x, y, r, g, b;
                Regex reg = new Regex("T=(?<t>\\d+).*.X=(?<x>\\d+).*.Y=(?<y>\\d+).*.R=(?<r>\\d+).*.G=(?<g>\\d+).*.B=(?<b>\\d+)");
                Match m = reg.Match(str);
                t = int.Parse(m.Groups["t"].Value);
                x = int.Parse(m.Groups["x"].Value);
                y = int.Parse(m.Groups["y"].Value);
                r = int.Parse(m.Groups["r"].Value);
                g = int.Parse(m.Groups["g"].Value);
                b = int.Parse(m.Groups["b"].Value);

                t /= 1000;

                while (--t>=0)
                {
                    SysMsg.ShotEvent();
                    bmp = Tcp.GetBitmap();
                    frm.ImgUpdateFunction(bmp);
                    if (PointColorAEqual(x, y, r, g, b))
                    {
                        index+=4;
                        return;
                    }
                    string str1;
                    if (t % 2 == 0)
                    {
                        str1 = lstScript[index + 2];
                    }
                    else
                    {
                        str1 = lstScript[index + 3];
                    }
                        if (str1.IndexOf("Touch") == 0)
                        {
                            Regex reg1 = new Regex("X=(?<x>\\d+).*.Y=(?<y>\\d+)");
                            Match m1 = reg1.Match(str1);
                            int x1 = int.Parse(m1.Groups["x"].Value);
                            int y1 = int.Parse(m1.Groups["y"].Value);
                            SysMsg.MouseEvent(0, x1, y1);
                            Thread.Sleep(1);
                            SysMsg.MouseEvent(1, x1, y1);
                        }
                    Thread.Sleep(1000);
                }
                //失败执行下一句
                index++;
            }
        }

        //触摸事件
        public static void Parse_Touch(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("Touch") == 0)
            {
                Regex reg = new Regex("X=(?<x>\\d+).*.Y=(?<y>\\d+)");
                Match m = reg.Match(str);
                int x = int.Parse(m.Groups["x"].Value);
                int y = int.Parse(m.Groups["y"].Value);
                SysMsg.MouseEvent(0, x, y);
                Thread.Sleep(1);
                SysMsg.MouseEvent(1, x, y);
                index++;
            }
        }

        //触摸事件
        public static void Parse_Drag(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("Drag") == 0)
            {
                Regex reg = new Regex("X1=(?<x1>\\d+).*.Y1=(?<y1>\\d+).X2=(?<x2>\\d+).*.Y2=(?<y2>\\d+)");
                Match m = reg.Match(str);
                int x1 = int.Parse(m.Groups["x1"].Value);
                int y1 = int.Parse(m.Groups["y1"].Value);
                int x2 = int.Parse(m.Groups["x2"].Value);
                int y2 = int.Parse(m.Groups["y2"].Value);
                SysMsg.MouseEvent(0, x1, y1);
                Thread.Sleep(200);
                SysMsg.MouseEvent(2, x2, y2);
                Thread.Sleep(200);
                SysMsg.MouseEvent(1, x2, y2);
                index++;
            }
        }

        //控制栏触摸
        public static void Parse_ControlTouch(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("Contorl") == 0)
            {
                Regex reg = new Regex("X=(?<x>\\d+).*.Y=(?<y>\\d+)");
                Match m = reg.Match(str);
                int x = int.Parse(m.Groups["x"].Value);
                int y = int.Parse(m.Groups["y"].Value);
                SysMsg.MouseControlEvent(0, x, y);
                Thread.Sleep(1);
                SysMsg.MouseControlEvent(1, x, y);
                index++;
            }
        }

        //触摸颜色点
        public static void Parse_ColorTouch(ref int index)
        {
            string str = lstScript[index];
            if (str.IndexOf("ColorTouch") == 0)
            {
                int r, g, b;
                Regex reg = new Regex("R=(?<r>\\d+).*.G=(?<g>\\d+).*.B=(?<b>\\d+)");
                Match m = reg.Match(str);
                r = int.Parse(m.Groups["r"].Value);
                g = int.Parse(m.Groups["g"].Value);
                b = int.Parse(m.Groups["b"].Value);

                Color c = Color.FromArgb(255,r,g,b);

                for (int i = 0; i < bmp.Width; i++)
                {
                    for (int j = 0; j < bmp.Height; j++)
                    {
                        if (bmp.GetPixel(i, j) == c)
                        {
                            SysMsg.MouseEvent(0, i, j);
                            Thread.Sleep(1);
                            SysMsg.MouseEvent(1, i, j);
                            index++;
                            return;
                        }
                    }
                }
                index++;
            }
        }

        //跳过IF结束语句
        private static void Parse_JumpIFF(ref int index)
        {
            if (lstScript[index].IndexOf("EndIF") == 0)
            {
                index++;
            }
            if (lstScript[index].IndexOf("Else") == 0)
            {
                while (lstScript[index].IndexOf("EndIF") == -1 && index<lstScript.Count-1)
                {
                    index++;
                }
                index++;
            }
        }

        //跳IF，不包括内嵌(内部调)
        private static void Parse_JumpIF(ref int index)
        {
            int n = 1;
            while (true)
            {
                if (lstScript[index].IndexOf("EndIF") == 0)
                {
                    index++;
                    n--;
                    if(n == 0)
                        return;
                }
                else if (lstScript[index].IndexOf("Else") == 0 && n == 1)
                {
                    //有Else需要执行
                    index++;
                    return;
                }
                else
                {
                    if (lstScript[index].IndexOf("IF") == 0 || lstScript[index].IndexOf("Var IF") == 0)
                    {
                        n++;
                    }
                    index++;
                    if (index == lstScript.Count - 1)
                    {
                        break;
                    }
                }
            }
        }


        //解析IF串
        public static void Parse_if(ref int index)
        {
            string str = lstScript[index];
            //判断保存的图片与当前是否仍然一致
            //IFSaveImg name Equal
            if (str.IndexOf("IFSaveImg") == 0)
            {
                Regex reg = new Regex("IFSaveImg (?<name>\\w+([-.]\\w+)*)");
                Match m = reg.Match(str);
                string name = m.Groups["name"].Value;
                int flag = 0;

                if (dicImg.ContainsKey(name))
                {
                    ScriptImg ori = dicImg[name];

                    int x = ori.x;
                    int y = ori.y;
                    int h = ori.bm.Height;
                    int w = ori.bm.Width;

                    for (int i = x; i < x + w; i++)
                        for (int j = y; j < y + h; j++)
                            if (ori.bm.GetPixel(i - x, j - y) != bmp.GetPixel(i, j))
                            {
                                flag = 1;
                                break;
                            }
                }
                else
                {
                    flag = 1;
                }

                if (flag == 1)
                {
                    //图片不相等
                    index++;
                    Parse_JumpIF(ref index);
                    return;
                }
                else
                {
                    //相等
                    index++;
                    return;
                }
            }


            //判断坐标颜色
            //IFColor X=445 Y=48 PointColorAEqual [A=255, R=139 G=23, B=23]
            else if (str.IndexOf("IFColor") == 0)
            {
                int x,y,r,g,b;
                Regex reg = new Regex("X=(?<x>\\d+).Y=(?<y>\\d+).*.R=(?<r>\\d+).*.G=(?<g>\\d+).*.B=(?<b>\\d+)");
                Match m = reg.Match(str);
                x = int.Parse(m.Groups["x"].Value);
                y = int.Parse(m.Groups["y"].Value);
                r = int.Parse(m.Groups["r"].Value);
                g = int.Parse(m.Groups["g"].Value);
                b = int.Parse(m.Groups["b"].Value);
                if (str.IndexOf("PointColorAEqual") != -1)
                {
                    if (PointColorAEqual(x, y, r, g, b))
                    {
                        index++;
                        return;
                    }
                    else
                    {
                        index++;
                        Parse_JumpIF(ref index);
                        return;
                    }
                }
                else if (str.IndexOf("PointColorNotAEqual") != -1)
                {
                    if (PointColorNotAEqual(x, y, r, g, b))
                    {
                        index++;
                        return;
                    }
                    else
                    {
                        index++;
                        Parse_JumpIF(ref index);
                        return;
                    }
                }
            }

            //判断最后Update APBC 值是否大于
            //IFAPBC AP>70 BC>70
            else if (str.IndexOf("IFAPBC") == 0)
            {
                int sap, sbc;
                Regex reg = new Regex("AP>(?<ap>\\d+).BC>(?<bc>\\d+)");
                Match m = reg.Match(str);
                sap = int.Parse(m.Groups["ap"].Value);
                sbc = int.Parse(m.Groups["bc"].Value);
                if (sap < ap && sbc < bc)
                {
                    index++;
                    return;
                }
                else
                {
                    index++;
                    Parse_JumpIF(ref index);
                    return;
                }
            }

            //判断时间范围
            //IFTIME S=0 E=6
            else if (str.IndexOf("IFTIME") == 0)
            {
                int sst, set;
                Regex reg = new Regex("S=(?<ap>\\d+).E=(?<bc>\\d+)");
                Match m = reg.Match(str);
                sst = int.Parse(m.Groups["ap"].Value);
                set = int.Parse(m.Groups["bc"].Value);
                int flag = 0;
                if (sst < set)
                {
                    if (sst <= DateTime.Now.Hour && set >= DateTime.Now.Hour)
                        flag = 1;
                }
                else if (sst > set)
                {
                    if (sst <= DateTime.Now.Hour || set >= DateTime.Now.Hour)
                        flag = 1;
                }
                else
                {//IFTIME S=0 E=0
                    flag = 1;
                }

                if (flag == 1)
                {
                    index++;
                    return;
                }
                else
                {
                    index++;
                    Parse_JumpIF(ref index);
                    return;
                }
            }
            //判断是否为觉醒自动日判断
            //IFTIME S=0 E=6
            else if (str.IndexOf("IFAICNJX") == 0)
            {
                int sst, set;
                sst = rjxbegin;
                set = rjxend;
                int flag = 0;
                if (sst < set)
                {
                    if (sst <= DateTime.Now.Hour && set > DateTime.Now.Hour)
                        flag = 1;
                }
                else if (sst > set)
                {
                    if (sst <= DateTime.Now.Hour || set > DateTime.Now.Hour)
                        flag = 1;
                }
                else
                {//IFTIME S=0 E=0
                    flag = 0;
                }

                if (flag == 1 && bc > rjxbc ||  (rpg == 1 && bc > rpgbc))
                {
                    //BC 时间都满足，判断是否是觉醒
                    Bitmap m = Properties.Resources._330_160_65_26;

                    int x = 330;
                    int y = 160;
                    int h = 26;
                    int w = 65;
                    flag = 0;

                    for (int i = x; i < x + w; i++)
                    {
                        for (int j = y; j < y + h; j++)
                        {
                            if (m.GetPixel(i - x, j - y) != bmp.GetPixel(i, j))
                            {
                                flag = 1;
                                break;
                            }
                        }
                        if (flag == 1)
                            break;
                    }
                    if (flag == 0)
                    {
                        //是觉醒
                        frm.LogUpdateFunction("Debug AICNJX 是觉醒且满足日觉醒条件");
                        index++;
                        return;
                    }
                    else
                    {
                        //不是觉醒开始判断是否强制日普怪
                        if (rpg == 1 && bc > rpgbc)
                        {
                            frm.LogUpdateFunction("Debug AICNJX 满足日普怪条件");
                            //满足日普怪条件
                            index++;
                            return;
                        }
                        else
                        {
                            frm.LogUpdateFunction("Debug AICNJX 不符合普怪条件");
                            index++;
                            Parse_JumpIF(ref index);
                            return;
                        }
                    }
                }
                else
                {
                    frm.LogUpdateFunction("Debug AICNJX 不符合日觉醒条件");
                    index++;
                    Parse_JumpIF(ref index);
                    return;
                }
            }
            //判断是否为觉醒
            //IFTIME S=0 E=6
            else if (str.IndexOf("IFCNJX") == 0)
            {
                Bitmap m = Properties.Resources._330_160_65_26;

                int x = 330;
                int y = 160;
                int h = 26;
                int w = 65;
                int flag = 0;

                for (int i = x; i < x + w; i++)
                {
                    for (int j = y; j < y + h; j++)
                    {
                        if (m.GetPixel(i - x, j - y) != bmp.GetPixel(i, j))
                        {
                            flag = 1;
                            break;
                        }
                    }
                    if (flag == 1)
                        break;
                }

                if (flag == 0)
                {
                    //相等
                    index++;
                    return;
                }
                else
                {
                    index++;
                    Parse_JumpIF(ref index);
                    return;
                }
            }
        }

        public static void Run()
        {
            /*
          string st = string.Empty;
          using (StreamReader sr = new StreamReader(new FileStream(Application.StartupPath + "\\jb.txt", FileMode.Open, FileAccess.Read)))
          {
              lstScript.Clear();
              while (sr.Peek() > 0)
              {
                  string str = sr.ReadLine();
                  while (str.IndexOf('	') == 0)
                      str = str.Substring(1);
                  while (str.IndexOf(' ') == 0)
                      str = str.Substring(1);
                  lstScript.Add(str);
                  st += str;
              }
              lstScript.Add("@end");
              sr.Close();
          }*/
          
         lstScript.Clear();
         scr = scr.Replace("\r\n", "\n");
         string[] s = scr.Split('\n');
         foreach (string ss in s)
         {
             string sss = ss;
             while (sss.IndexOf('	') == 0)
                 sss = sss.Substring(1);
             while (sss.IndexOf(' ') == 0)
                 sss = sss.Substring(1);
             lstScript.Add(sss);
         }
         

            int index = 0;
            while (true)
            {
                Parse_Empty(ref index);
                Parse_SaveImg(ref  index);
                Parse_Debug(ref  index);
                Parse_Sleep(ref  index);
                Parse_UpdataImg(ref  index);
                Parse_Touch(ref  index);
                Parse_ControlTouch(ref  index);
                Parse_ColorTouch(ref  index);
                Parse_JumpIFF(ref  index);
                Parse_if(ref  index);
                Parse_Goto(ref  index);
                Parse_Dim(ref index);
                Parse_WaitColor(ref index);
                Parse_UpdateBloodImg(ref index);

                Parse_SendAPBC(ref index);
                Parse_SendCnt(ref index);
                Parse_SetCard(ref index);

                Parse_Drag(ref index);

                Parse_HKG(ref index);

                if (index == lstScript.Count - 1)
                {
                    frm.LogUpdateFunction("脚本执行完毕!");
                    return;
                }
            }
        }
    }
}
