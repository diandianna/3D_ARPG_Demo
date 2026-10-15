using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3DArpgServer
{
        public class AcountData
        {
            public string acountName;
            public string password;

            public AcountData(string acountName, string password)
            {
                this.acountName = acountName;
                this.password = password;
            }

            AcountData()
            {
            
            }
        }
    
}
