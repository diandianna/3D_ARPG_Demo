using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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


        MultiSync = 98,//多人信息（位置，怪物数据等等）同步
        Multiplayer = 99,//多人联机请求消息
        MonsterSync = 100,//怪物同步消息
    }

    /*
     *客户端发送
     *Login消息（4数据总长+4类型+N账号_密码）
     *Register消息（4数据总长+4类型+N账号_密码）
     *Updata消息（4数据总长+4类型+行为（什么导致的数据变化））
     *Atk消息（4数据总长+4类型+4敌人index）
     *MoveSync消息（4数据总长+4类型+4模拟输入+12（Vecter3）预测位置）
     *
     *
     *服务器回复
     *Login消息（4数据总长+4类型+4结果+44玩家数据）
     *Updata消息（4数据总长+4类型+4结果+44玩家数据+签名（MD5(玩家数据+密钥)）
     *Register消息（4数据总长+4类型+4结果） 
     *Atk消息（4数据总长+4类型+4伤害值+4敌人index）
     *MoveSync消息（4数据总长+4类型+12（Vecter3）实际位置）
     * 
     */

