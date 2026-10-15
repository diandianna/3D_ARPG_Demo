using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace _3DArpgServer
{
    class SignKeyUtil
    {
        public static string GetSignKey()
        {
            string prefix = "ARPG";
            string secret = "Secure_Key";
            return $"{prefix}_{secret}";
        }

        //组合字符串
        public static string GenerateSignature(string str1, string str2)
        {
            return str1+str2;
        }
    }
}
