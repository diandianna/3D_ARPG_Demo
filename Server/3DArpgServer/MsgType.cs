using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3DArpgServer
{
    enum MsgType
    {
        Login = 0,//登录信息，登录成功返回账户用户数据给客户端
        Register = 1,//注册信息
        Update = 2,//更新玩家数据信息

        ChangePassword = 3,//更改账号密码信息
        CancelAcount = 4,//注销账号信息
        Atk = 5,//攻击信息，返回伤害值给客户端
        MoveSync = 6,//移动同步
        HeartBeat = 7,//心跳消息
        ShopBuy = 8,//商店购买
        TaskClaim = 9,//任务领奖

        Hurt = 95,
        VisitorHurt = 96,//访客受伤消息（受击和死亡动画都通过这个）
        VisitorAtk = 97,//访客攻击消息（攻击动画）
        MultiSync = 98,//多人信息（位置，怪物数据等等）同步
        Multiplayer = 99,//多人联机请求消息
        MonsterSync = 100,//怪物同步消息
        Possess = 101,
        PossessOK = 102,
        PossessState = 103,

        PlayerAnimation = 200,
        MonsterAnimation = 201
    }

    /*
     *客户端发送
     *Login        消息（4数据总长+4类型+ N账号_密码）
     *Register     消息（4数据总长+4类型+ N账号_密码）
     *Updata       消息（4数据总长+4类型+ 4行为（什么导致的数据变化））
     *Atk          消息（4数据总长+4类型+ 4敌人index）
     *MoveSync     消息（4数据总长+4类型+ 4模拟输入+12（Vecter3）预测位置）
     *MultiSync    消息（4数据总长+4类型+ 4房间号+12位置 +4旋转）
     *Multiplayer  消息（4数据总长+4类型+ 4房间号）
     *MonsterSync  消息（4数据总长+4类型+ 4房间号+4怪物count+怪物数据）
     *Possess      消息（4数据总长+4类型+ 4房间号+4附身/解除附身的怪物id+4附身/解除【1附身，0解除】）
     *PossessOK    消息（4数据总长+4类型+ 4房间号+4玩家名长度+n玩家名+4附身/解除附身的怪物id+4结果【1成功，0失败】）
     *PossessState 消息（4数据总长+4类型+ 4房间+4附身/解除附身的怪物id+12（vector3）怪物位置+4旋转yaw）
     *
     *Hurt         消息（4数据总长+4类型+4伤害）
     *VisitorHurt  消息（4数据总长+4类型+4房间号+4伤害+4账号名长度+n账号名）
     *VisitorAtk   消息（4数据总长+4类型+4房间号+4怪物id+4账号名长度+n账号名）
     *                                    
     *服务器回复/转发                          
     *Login        消息（4数据总长+4类型+ 4结果+44玩家数据+4账号名长度+N账号名）
     *Updata       消息（4数据总长+4类型+ 4结果+44玩家数据+签名（MD5(玩家数据+密钥)）
     *Register     消息（4数据总长+4类型+ 4结果） 
     *Atk          消息（4数据总长+4类型+ 4伤害值+4敌人index）
     *MoveSync     消息（4数据总长+4类型+ 12（Vecter3）实际位置）
     *MultiSync    消息（4数据总长+4类型+ 4房间号+12位置+4旋转+4账号名长度+n账户名+账号数据）
     *Multiplayer  消息（4数据总长+4类型+ 结果+4房间号）
     *MonsterSync  消息（4数据总长+4类型+ 4房间号+4怪物count+怪物数据） 
     *Possess      消息（4数据总长+4类型+ 4房间号+4玩家名长度+n玩家名+4附身/解除附身的怪物id+4附身/解除【1附身，0解除】）
     *PossessOK    消息（4数据总长+4类型+ 4房间号+4玩家名长度+n玩家名+4附身/解除附身的怪物id+4结果【1成功，0失败】）
     *
     *ShopBuy       消息（4数据总长+4类型+ 4商品id）——客户端发，服务器查价格表扣钱
     *ShopBuy       消息（4数据总长+4类型+ 4结果+4商品id+4新金币）——服务器回
     *TaskClaim     消息（4数据总长+4类型+ 4任务id）——客户端发，服务器查奖励表发奖
     *TaskClaim     消息（4数据总长+4类型+ 4结果+4任务id+4新金币+4新经验）——服务器回
     *
     * 
     * 
     * 
     * 
     * 客户端发送PlayerAnimation （4数据总长+4类型+4房间号+4动画id+4idCombo）
     * 服务器发送PlayerAnimation （4数据总长+4类型+4动画id+4idCombo+4玩家名长度+n玩家名）
     * 
     * 
     * 客户端发送MonsterAnimation（4数据总长+4类型+4房间号+4动画id+4idCombo+4怪物id）
     * 服务器发送MonsterAnimation（4数据总长+4类型+4动画id+4idCombo+4怪物id+4玩家名长度+n玩家名+）
     */





    enum UpdataPlayerDataAction
    {
        //获取经验，金币，钻石相关
        killLittleMonster = 0,
        killEliteMonster = 1,
        killBossMonster = 2,
        //提升攻击，防御，生命相关
        Upgrade = 3,
        Timed_buff = 4,

        //消耗金币相关
        Shopping = 5,

        Test = 99,//测试用
    }
}
