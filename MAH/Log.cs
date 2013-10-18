using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace MAH
{
    class Log
    {
        static FileStream fst;
        static StreamWriter swt;

        public static void Log_Create(string name = "log.txt", int mode = 0)
        {

            string log_dir = System.AppDomain.CurrentDomain.BaseDirectory + name;
            try
            {
                if (File.Exists(log_dir) == true && mode == 0)
                    fst = new FileStream(log_dir, FileMode.Append);
                else
                {
                    File.Delete(log_dir);
                    fst = new FileStream(log_dir, FileMode.OpenOrCreate);
                }
                swt = new StreamWriter(fst, System.Text.Encoding.GetEncoding("utf-8"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static void Log_Write(string main, int type = 0)
        {
            try
            {
                swt.WriteLine("[" + DateTime.Now.ToString("MM月dd日 HH时mm分ss秒") + "] " + main);
                swt.Flush();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static void Log_Close()
        {
            try
            {
                swt.Close();
                fst.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
