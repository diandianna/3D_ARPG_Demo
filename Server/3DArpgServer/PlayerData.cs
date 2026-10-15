using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3DArpgServer
{
    public class PlayerData
    {
        //等级
        public int level;
        //等级数值
        public int levelNum;
        //生命值
        public int hp;
        //最大生命值
        public int MaxHp;
        //攻击力
        public int atkNum;
        //防御力
        public int DefNum;
        //暴击率
        public float CritRate;
        //暴击伤害
        public float CritDamage;
        //攻击力加成
        public float DamageAdd;


        //金币
        public int money1;
        //钻石
        public int money2;
        public PlayerData()
        {
            Init();
        }

        public void Init()
        {
            atkNum = 10;
            DefNum = 10;
            hp = 200;
            MaxHp = 200;
            level = 1;
            levelNum = 0;
            CritRate = 0.15f;
            CritDamage = 1.3f;
            DamageAdd = 0f;
            money1 = 0;
            money2 = 0;
        }

        float min=0.7f;
        float max=1.0f;
        //随机数，防止伤害定死
        public float DamageRand;
        public float GetDamage()
        {
            Random r = new Random();
            DamageRand = min + (float)r.NextDouble() *(max- min);
            if(CritRate>=100)
            {
                // 暴击伤害 = 攻击力 × (1 + 攻击加成) × 暴击倍率 × 随机浮动
                return atkNum * (1 + DamageAdd) * CritDamage * DamageRand;
            }
            int Crit = r.Next(0, 100);
            if (Crit <= CritRate * 100)
            {
                Console.WriteLine("暴击了");
                // 暴击伤害 = 攻击力 × (1 + 攻击加成) × 暴击倍率 × 随机浮动
                return atkNum * (1 + DamageAdd) * CritDamage * DamageRand;
            }

            // 普通伤害 = 攻击力 × (1 + 攻击加成) × 随机浮动
            return atkNum * (1 + DamageAdd) * DamageRand;
        }

        public byte[] GetPlayerDataBytes()
        {
            byte[] buffer = new byte[44];
            int index = 0;
            BitConverter.GetBytes(level).CopyTo(buffer, index);
            index += 4;
            BitConverter.GetBytes(levelNum).CopyTo(buffer, index);
            index += 4;
            BitConverter.GetBytes(hp).CopyTo(buffer, index);
            index += 4;
            BitConverter.GetBytes(MaxHp).CopyTo(buffer, index);
            index += 4;
            BitConverter.GetBytes(atkNum).CopyTo(buffer, index);
            index += 4;
            BitConverter.GetBytes(DefNum).CopyTo(buffer, index);
            index += 4;
            BitConverter.GetBytes(CritRate).CopyTo(buffer, index);
            index += 4;
            BitConverter.GetBytes(CritDamage).CopyTo(buffer, index);
            index += 4;
            BitConverter.GetBytes(DamageAdd).CopyTo(buffer, index);
            index += 4;
            BitConverter.GetBytes(money1).CopyTo(buffer, index);
            index += 4;
            BitConverter.GetBytes(money2).CopyTo(buffer, index);
            index += 4;

            return buffer;
        }

        public static void UpdataPlayerData(string acountName, byte[] updataPlayerData)
        {
            PlayerData playerData = AcountPlayerData.Instance.GetAcountPlayerData(acountName);
            if(playerData == null)
            {
                Console.WriteLine("账号不存在");
                return;
            }

            //前一个字节是数据类型标识，前面已经取出来判断过了
            int index = 8;
            playerData.level = BitConverter.ToInt32(updataPlayerData, index);
            index += 4;
            playerData.levelNum = BitConverter.ToInt32(updataPlayerData, index);
            index += 4;
            playerData.hp = BitConverter.ToInt32(updataPlayerData, index);
            index += 4;
            playerData.MaxHp = BitConverter.ToInt32(updataPlayerData, index);
            index += 4;
            playerData.atkNum = BitConverter.ToInt32(updataPlayerData, index);
            index += 4;
            playerData.DefNum = BitConverter.ToInt32(updataPlayerData, index);
            index += 4;
            playerData.CritRate = BitConverter.ToSingle(updataPlayerData, index);
            index += 4;
            playerData.CritDamage = BitConverter.ToSingle(updataPlayerData, index);
            index += 4;
            playerData.DamageAdd = BitConverter.ToSingle(updataPlayerData, index);
            index += 4;
            playerData.money1 = BitConverter.ToInt32(updataPlayerData, index);
            index += 4;
            playerData.money2 = BitConverter.ToInt32(updataPlayerData, index);
            index += 4;
            Console.WriteLine($"用户{acountName}:数据保存成功");
            return ;
        }

        public static int GetByteSize()
        {
            return 44;
        }

        public string GetPlayerDataUtil()
        {
            return $"{level}_{levelNum}_{hp}_{MaxHp}_{atkNum}_{DefNum}_{CritRate}_{CritDamage}_{DamageAdd}_{money1}_{money2}_";
        }
    }
}
