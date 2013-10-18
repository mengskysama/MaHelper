using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.IO.Compression;
using System.IO;
using System.Threading;
using System.Management;

namespace MAH
{
    class HTTP
    {

        public static string user;
        public static string pass;

        public static string code = "";

        public static string login_last2 = "";

        public static string url = "http://mabi.mengsky.net/";
        public static string ua = "";

        public static string GetNowStamp()
        {
            DateTime timeStamp = new DateTime(1970, 1, 1);  //得到1970年的时间戳
            return ((DateTime.UtcNow.Ticks - timeStamp.Ticks) / 10000000).ToString();
        }

        public static string EncryptMD5(string message)
        {
            byte[] hashedBytes = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(Encoding.Default.GetBytes(message));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }

        public static int Login(string user, string pass, int ver)
        {
            string ret = HttpGet(url + "l4.php?u=" + user + "&p=" + pass + "&ver=" + ver);
            if (ret != null && ret.IndexOf("1") != -1)
                return 0;
            else
                return 1;
        }

        public static string GetKey(int offset = 0)
        {
            string fs = HTTP.GetNowStamp().Substring(0, 6);
            int k = int.Parse(fs) + 100256 + offset;
            return k.ToString() + "00";
        }

        public static string Login2(string user, string pass, int reg, string reg_loginlast2)
        {
            string ret = null;
            if (reg == 1)
            {
                ret = HttpGet(url + "km_pub_10_1.php?u=" + user + "&p=" + pass + "&l2=" + reg_loginlast2 + "&m=" + HTTP.code + "&r=1", 1);
                if (ret != null && ret.Length > 0 && ret[0] == '1')
                {
                    return "1";
                }
            }
            else
            {
                Version ver = System.Environment.OSVersion.Version;
                ret = HttpGet(url + "km_pub_10_1.php?v=" + ver.Major.ToString() + "_" + ver.Minor.ToString() + "&u=" + user + "&p=" + pass + "&m=" + HTTP.code, 1);

                if (ret != null && ret.Length > 1 && ret[0] == '1')
                {
                    ret = ret.Substring(1);
                    string dec = DES.DecryptDES(ret, GetKey());
                    if (dec == null)
                    {
                        dec = DES.DecryptDES(ret, GetKey(1));
                        if (dec == null)
                        {
                            dec = DES.DecryptDES(ret, GetKey(-1));
                            if (dec == null)
                            {
                                return null;
                            }
                        }
                    }
                    return dec;
                }
            }
            return null;
        }

        public static int Reg(string user, string pass)
        {
            string ret = HttpGet(url + "l61.php?r=1&u=" + user + "&p=" + pass);
            if (ret != null && ret.IndexOf("1") != -1)
                return 0;
            else
                return 1;
        }

        public static int HttpPost(string url, string data, string cookie, string cookie2)
        {
            String recv = String.Empty;
            System.Net.HttpWebRequest get = (HttpWebRequest)System.Net.WebRequest.Create(url);
            get.Method = "POST";
            get.KeepAlive = true;
            get.Headers.Add("accept-encoding: gzip, deflate");
            //Time Out
            get.Timeout = 10000;
            get.ReadWriteTimeout = 10000;
            //302取不到cookie
            get.AllowAutoRedirect = false;
            get.UserAgent = "Million/100 (GT-I9100; GT-I9100; 2.3.4) samsung/GT-I9100/GT-I9100:2.3.4/GRJ22/eng.build.20120314.185218:eng/release-keys";
            get.Headers.Add(cookie);
            get.Headers.Add(cookie2);
            //POST数据
            if (data != null)
            {
                get.ServicePoint.Expect100Continue = false; //服务器不响应Expect: 100-continue
                get.ContentType = "application/x-www-form-urlencoded";

                byte[] bs = ASCIIEncoding.ASCII.GetBytes(data);
                get.ContentLength = bs.Length;
                using (Stream reqStream = get.GetRequestStream())
                {
                    reqStream.Write(bs, 0, bs.Length);
                    reqStream.Close();
                }
            }
            try
            {
                HttpWebResponse getRespone = (HttpWebResponse)get.GetResponse();
                getRespone.Close();
                return 0;
            }
            catch (WebException)
            {
                return 1;
            }
        }

        public static byte[] HttpPost1(string url, string data, string cookie, string cookie2, int exception = 0)
        {

            String recv = String.Empty;
            System.Net.HttpWebRequest get = (HttpWebRequest)System.Net.WebRequest.Create(url);

            if (MA.proxy_enable == 1 && MA.http_proxy != null)
            {
                WebProxy proxy = new WebProxy(MA.http_proxy, true);
                get.Proxy = proxy;
            }

            get.Method = "POST";
            get.KeepAlive = true;

            if (MA.proxy_enable == 0)
            {
                //get.Headers.Add("accept-encoding: gzip, deflate");
            }
            else
            {
                //gae加这gzip会悲剧
                //get.Headers.Add("accept-encoding: deflate");
            }

            //Time Out
            get.Timeout = 15000;
            get.ReadWriteTimeout = 15000;
            //302取不到cookie
            get.AllowAutoRedirect = false;
            get.UserAgent = HTTP.ua;

            if(cookie != null)
                get.Headers.Add(cookie);
            if (cookie2 != null)
                get.Headers.Add(cookie2);
            //POST数据
            if (data != null)
            {
                get.ServicePoint.Expect100Continue = false; //服务器不响应Expect: 100-continue
                get.ContentType = "application/x-www-form-urlencoded";

                byte[] bs = ASCIIEncoding.ASCII.GetBytes(data);
                get.ContentLength = bs.Length;
                using (Stream reqStream = get.GetRequestStream())
                {
                    reqStream.Write(bs, 0, bs.Length);
                    reqStream.Close();
                }
            }
            try
            {
                HttpWebResponse getRespone = (HttpWebResponse)get.GetResponse();
                try
                {
                    if (getRespone != null && getRespone.StatusCode == HttpStatusCode.OK)
                    {
                        if (getRespone.Headers.Get("Set-Cookie") != null)
                        {
                            string str = getRespone.Headers.Get("Set-Cookie");
                            int index = str.LastIndexOf("S=");
                            int end = str.IndexOf(";", index);
                            Script.frm.cookie2 = "cookie2: $Version=1";
                            Script.frm.cookie = "cookie: " + str.Substring(index, end - index);
                        }

                        recv = getRespone.ContentEncoding;
                        System.IO.Stream resStream = getRespone.GetResponseStream();
                        try
                        {
                            if (recv == "gzip")
                            {
                                //
                                return null;
                            }
                            else
                            {

                                int len = (int)getRespone.ContentLength;
                                byte[] buff = null;
                                int count = 1024 * 400;
                                if (len < 0)
                                {
                                    buff = new byte[count];
                                    MemoryStream memStream = new MemoryStream();
                                    try
                                    {
                                        for (int i = resStream.Read(buff, 0, count); i > 0; i = resStream.Read(buff, 0, count))
                                        {
                                            memStream.Write(buff, 0, i);
                                        }
                                        buff = memStream.ToArray();
                                        memStream.Close();
                                    }
                                    catch (Exception)
                                    {
                                        memStream.Close();
                                        throw;
                                    }
                                }
                                else
                                {
                                    buff = new byte[len];
                                    int offset = 0;
                                    while (len > 0)
                                    {
                                        int n = resStream.Read(buff, offset, len);
                                        offset += n;
                                        len -= n;
                                    }
                                }
                                return buff;
                            }
                        }
                        catch (Exception)
                        {
                            resStream.Close();
                            throw;
                        }
                    }
                    else
                    {
                        //http err code
                        return null;
                    }
                }
                catch (Exception)
                {
                    getRespone.Close();
                    throw;
                }
            }
            catch (Exception)
            {
                if (exception == 1)
                    throw;
                else
                    return null;
            }
        }

        public static string HttpGet(string url, int exception = 0)
        {
            System.Net.HttpWebRequest get = (HttpWebRequest)System.Net.WebRequest.Create(url);

            get.Method = "GET";
            get.UserAgent = "User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1500.72 Safari/537.36";
            get.KeepAlive = true;
            get.Headers.Add("accept-encoding: gzip, deflate");
            get.Accept = "*/*";
            get.Timeout = 10000;
            get.ReadWriteTimeout = 10000;

            try
            {
                string recv = null;
                HttpWebResponse getRespone = (HttpWebResponse)get.GetResponse();
                if (getRespone != null && getRespone.StatusCode == HttpStatusCode.OK)
                {
                    recv = getRespone.ContentEncoding;
                    System.IO.Stream resStream = getRespone.GetResponseStream();
                    if (recv == "gzip")
                    {
                        resStream = new GZipStream(resStream, CompressionMode.Decompress);
                        System.IO.StreamReader sr = new System.IO.StreamReader(resStream, Encoding.UTF8);
                        recv = sr.ReadToEnd();
                        sr.Close();
                        resStream.Close();
                    }
                    else
                    {
                        System.IO.StreamReader sr = new System.IO.StreamReader(resStream);
                        recv = sr.ReadToEnd();
                        sr.Close();
                        resStream.Close();
                    }
                }
                getRespone.Close();
                return recv;
            }
            catch (Exception)
            {
                if(exception == 1)
                    throw;
                return null;
            }
        }

        public static string TestPorxy(string uri)
        {
            System.Net.HttpWebRequest get = (HttpWebRequest)System.Net.WebRequest.Create("http://www.baidu.com");

            get.Method = "GET";
            get.UserAgent = "User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1500.72 Safari/537.36";
            get.KeepAlive = true;
            //代理不支持get.Headers.Add("accept-encoding: gzip, deflate");
            get.Accept = "*/*";
            get.Timeout = 5000;
            get.ReadWriteTimeout = 5000;


            try
            {
                if (MA.http_proxy == null)
                {
                    return null;
                }

                WebProxy proxy = new WebProxy(MA.http_proxy, true);
                get.Proxy = proxy;

                string recv = null;
                HttpWebResponse getRespone = (HttpWebResponse)get.GetResponse();
                if (getRespone != null && getRespone.StatusCode == HttpStatusCode.OK)
                {
                    recv = getRespone.ContentEncoding;
                    System.IO.Stream resStream = getRespone.GetResponseStream();
                    if (recv == "gzip")
                    {
                        resStream = new GZipStream(resStream, CompressionMode.Decompress);
                        System.IO.StreamReader sr = new System.IO.StreamReader(resStream, Encoding.UTF8);
                        recv = sr.ReadToEnd();
                        sr.Close();
                        resStream.Close();
                    }
                    else
                    {
                        System.IO.StreamReader sr = new System.IO.StreamReader(resStream);
                        recv = sr.ReadToEnd();
                        sr.Close();
                        resStream.Close();
                    }
                }
                getRespone.Close();
                return recv;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
