using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
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

    public void Init()
    {
        atkNum = 1;
        DefNum = 1;
        hp = 200;
        MaxHp = 200;
        level = 1;
        levelNum = 0;
        CritRate = 0.3f;
        CritDamage = 0.5f;
        DamageAdd = 0.1f;
        money1 = 0;
        money2 = 0;
    }

    //public float GetDamage()
    //{
    //    DamageRand = Random.Range(0.7f, 1f);
    //    int Crit = Random.Range(0, 100);
    //    if(Crit <= CritRate * 100)
    //    {
    //        Debug.Log("暴击了");
    //        // 暴击伤害 = 攻击力 × (1 + 攻击加成) × 暴击倍率 × 随机浮动
    //        return atkNum * (1 + DamageAdd) * CritDamage * DamageRand;
    //    }

    //    // 普通伤害 = 攻击力 × (1 + 攻击加成) × 随机浮动
    //    return atkNum * (1 + DamageAdd) * DamageRand;
    //}

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

    public void UpdataPlayerData(byte[] updataPlayerData)
    {
        //第一个字节是数据类型标识，前面已经取出来判断过了
        //第二个字节是请求结果，1成功，0失败
        int index = 12;
        level = BitConverter.ToInt32(updataPlayerData, index);
        index += 4;
        levelNum = BitConverter.ToInt32(updataPlayerData, index);
        index += 4;
        hp = BitConverter.ToInt32(updataPlayerData, index);
        index += 4;
        MaxHp = BitConverter.ToInt32(updataPlayerData, index);
        index += 4;
        atkNum = BitConverter.ToInt32(updataPlayerData, index);
        index += 4;
        DefNum = BitConverter.ToInt32(updataPlayerData, index);
        index += 4;
        CritRate = BitConverter.ToSingle(updataPlayerData, index);
        index += 4;
        CritDamage = BitConverter.ToSingle(updataPlayerData, index);
        index += 4;
        DamageAdd = BitConverter.ToSingle(updataPlayerData, index);
        index += 4;
        money1 = BitConverter.ToInt32(updataPlayerData, index);
        index += 4;
        money2 = BitConverter.ToInt32(updataPlayerData, index);
        index += 4;

        int len = BitConverter.ToInt32(updataPlayerData, index); index += 4;
        string name = Encoding.UTF8.GetString(updataPlayerData,index,len);
        GameDataMgr.Instance.acountName = name;
        return;
    }

    public void AddMoney(int money1,int money2)
    {
        this.money1 += money1;
        this.money2 += money2;
    }
    public void AddExp(int exp)
    {
        this.levelNum += exp;
    }
    public void AddMaxHp(int maxhp)
    {
        this.MaxHp = maxhp;
    }
    public void AddCrit(float CritRate,float CritDamage)
    {
        this.CritRate += CritRate;
        this.CritDamage += CritDamage;
    }

    public  void ChangeHp(int num)
    {
        this.hp += num;
        if (hp <= 0) 
        {
           Animator animator = GameDataMgr.Instance.mainCharacter.GetComponent<Animator>();
            if(animator != null)
            {
                animator.SetBool("Death", true);
                GameDataMgr.Instance.playerDeath = true;
            }
        }
        else if (hp > MaxHp)
        {
            hp = MaxHp;
        }
    }
}
