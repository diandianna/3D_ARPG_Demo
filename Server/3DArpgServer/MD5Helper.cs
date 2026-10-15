using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace _3DArpgServer
{
    class MD5Helper
    {
        //字符串转换成md5签名
        public static string GetMd5(string str)
        {
            using (var md5 = MD5.Create())
            {
                byte[] strBytes = Encoding.UTF8.GetBytes(str);
                byte[] hashBytes = md5.ComputeHash(strBytes);
                return BitConverter.ToString(hashBytes).Replace("-","").ToUpper();
            }
        }
    }
}
