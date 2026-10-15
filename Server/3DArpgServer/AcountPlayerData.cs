using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3DArpgServer
{
    class AcountPlayerData
    {
        private static AcountPlayerData instance = new AcountPlayerData();
        public static AcountPlayerData Instance=>instance;

        public Dictionary<string,PlayerData> playerDatas = new Dictionary<string,PlayerData>();

        public void AddAcount(string acountName)
        {
            playerDatas.Add(acountName, new PlayerData());
        }
        public void RemoveAcount() 
        {

        }
        public PlayerData GetAcountPlayerData(string AcountName)
        {
            if(playerDatas.ContainsKey(AcountName))
            {
                return playerDatas[AcountName];
            }
            return null;
        }
    }

}
