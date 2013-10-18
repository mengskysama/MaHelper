using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;

namespace MAH_LocalKeyGen
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string code = textBox1.Text;
            if (code.Length == 10)
            {
                string key = EncryptMD5(EncryptMD5(code + code.Substring(2))).Substring(5);
                textBox2.Text = key;
                try
                {
                    FileStream aFile = new FileStream(Application.StartupPath+"//"+key, FileMode.OpenOrCreate);
                    StreamWriter sw = new StreamWriter(aFile);
                    sw.WriteLine("1");
                    sw.Close();
                    aFile.Close();
                }
                catch (IOException ex)
                {
                }
            }
            
        }
        public static string EncryptMD5(string message)
        {
            byte[] hashedBytes = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(Encoding.Default.GetBytes(message));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
