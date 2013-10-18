using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Globalization;

namespace MAH
{
    class DES
    {
        /// <summary>  
        /// DES加密方法  
        /// </summary>  
        /// <param name="strPlain">明文</param>  
        /// <param name="strDESKey">密钥</param>  
        /// <param name="strDESIV">向量</param>  
        /// <returns>密文</returns>  
        public static string Encrypt(string source, string _DESKey)
        {
            StringBuilder sb = new StringBuilder();
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] key = ASCIIEncoding.ASCII.GetBytes(_DESKey);
                byte[] iv = ASCIIEncoding.ASCII.GetBytes(_DESKey);
                byte[] dataByteArray = Encoding.UTF8.GetBytes(source);
                des.Mode = System.Security.Cryptography.CipherMode.CBC;
                des.Key = key;
                des.IV = iv;
                string encrypt = "";
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(dataByteArray, 0, dataByteArray.Length);
                    cs.FlushFinalBlock();
                    //輸出資料  
                    foreach (byte b in ms.ToArray())
                    {
                        sb.AppendFormat("{0:X2}", b);
                    }
                    encrypt = sb.ToString();
                }
                return encrypt;
            }

        }

        /// <summary>  
        /// 进行DES解密。  
        /// </summary>  
        /// <param name="pToDecrypt">要解密的串</param>  
        /// <param name="sKey">密钥，且必须为8位。</param>  
        /// <returns>已解密的字符串。</returns>  
        public static string DecryptDES(string decryptString, string decryptKey)
        {
            try
            {
                byte[] rgbKey = ASCIIEncoding.ASCII.GetBytes(decryptKey);
                byte[] rgbIV = rgbKey;
                byte[] inputByteArray = new byte[decryptString.Length / 2];
                for (int x = 0; x < decryptString.Length / 2; x++)
                {
                    int i = (Convert.ToInt32(decryptString.Substring(x * 2, 2), 16));
                    inputByteArray[x] = (byte)i;
                }
                DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Encoding.UTF8.GetString(mStream.ToArray());
            }
            catch
            {
                return null;
            }
        }
    }
}
