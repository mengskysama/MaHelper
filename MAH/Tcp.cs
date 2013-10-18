using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.IO;

namespace MAH
{
    class Tcp
    {
        private static Thread accept = null;

        private static byte[] bytes = new byte[1024 * 1024 * 16];

        private static int offset = 0;
        private static int recved = 0;

        public static Bitmap GetBitmap()
        {
            //互斥
            while (recved == 0)
            {
            }

            recved = 0;

            byte[] tmp = new byte[offset];
            int i = 0;
            while (i < offset)
                tmp[i] = bytes[i++];
            MemoryStream ms1 = new MemoryStream(tmp);
            Bitmap bm = (Bitmap)Image.FromStream(ms1);
            ms1.Close();
            return bm;
        }

        private static void LinstenAccept()
        {
            IPAddress ipAddress = IPAddress.Any;
            TcpListener tcpListener = new TcpListener(ipAddress, 4814);
            tcpListener.Start();
            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                Thread t = new Thread(new ParameterizedThreadStart(RecvTcp));
                t.Start(tcpClient);
            } 
        }

        public static void AcceptTcp()
        {
            if (accept == null)
            {
                accept = new Thread(LinstenAccept);
                accept.Start();
            }
        }

        private static void RecvTcp(Object OtcpClient)
        {
            TcpClient tcpClient = (TcpClient)OtcpClient;

            NetworkStream ns = tcpClient.GetStream();
            int recv;
            offset = 0;
            while (true)
            {
                try
                {
                    if ((recv = ns.Read(bytes, offset, 2048)) == 0)
                    {
                        break;
                    }
                    else if (recv < 0)
                    {
                        break;
                    }
                    offset += recv;
                }
                catch
                {
                    break;
                }
            }
            ns.Close();
            tcpClient.Close();
            recved = 1;
        }
    }
}
