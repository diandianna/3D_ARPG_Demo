using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks.Sources;

namespace _3DArpgServer
{


    internal class Program
    {
        static List<AcountData> acountList = new List<AcountData>();
        static List<Socket> clients = new List<Socket>();

        // 商店价格表 / 任务奖励表：id 与客户端 ItemConfig / TaskConfig 对齐
        static Dictionary<int, int> shopPrices = new Dictionary<int, int>()
        {
            { 1, 10 }, { 2, 20 }, { 3, 30 },
        };
        static Dictionary<int, (int gold, int exp)> taskRewards = new Dictionary<int, (int gold, int exp)>()
        {
            { 1, (gold: 50, exp: 30) },
            { 2, (gold: 100, exp: 60) },
            { 3, (gold: 200, exp: 100) },
        };


        //玩家数据管理器
        //static Dictionary<Socket,PlayerData> clients_playerData = new Dictionary<Socket,PlayerData>();//每个Socket对应的玩家数据
        //联机房间管理器
        static Dictionary<int,Socket> hosts  = new Dictionary<int,Socket>();//每个房间对应的房主
        static Dictionary<Socket, string> clientName  = new Dictionary<Socket, string>();//每个Socket对应的用户名（仅限保持登录期间）
        static Dictionary<int,List<Socket>> visitors  = new Dictionary<int,List<Socket>>();//每个房间里面的访客列表
        static async Task Main(string[] args)
        {
            


            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080));
            socket.Listen(10);

            Console.WriteLine("Socket服务器启动，等待连接...");

            AcountData test = new AcountData("asd", "123");
            acountList.Add(test);
            AcountPlayerData.Instance.AddAcount("asd");
            PlayerData testAcount = AcountPlayerData.Instance.GetAcountPlayerData("asd");
            testAcount.money1 = 99;
            testAcount.money2 = 52;

            // 启动读档：有存档就覆盖内存里的玩家数据
            // TODO: 账号表 acountList 也要持久化，否则重启后新注册的账号登录不了
            var saved = SaveHelper.Load();
            if (saved != null)
            {
                foreach (var kv in saved)
                    AcountPlayerData.Instance.playerDatas[kv.Key] = kv.Value;
                Console.WriteLine($"已加载存档，共 {saved.Count} 个玩家");
            }

            while (true)
            {

                Socket clientSocket = await socket.AcceptAsync();
                clients.Add(clientSocket);
                Console.WriteLine($"[{clientSocket.RemoteEndPoint}] 上线");

                _ = HandleClient(clientSocket); // 异步处理客户端连接
            }

        }

        static async Task HandleClient(Socket socket)
        {
            byte[] buffer = new byte[1024];

            DateTime lastHeartbeat = DateTime.UtcNow;

            // 接收缓冲区（核心！）
            byte[] _recvBuffer = new byte[1024];        //MemoryStream _recvBuffer = new MemoryStream();
            // 当前正在接收的消息的长度（-1表示还没读到长度头）
            int _currentMsgLength = -1;

            using var cts = new CancellationTokenSource();

            try
            {
                _ = Task.Run(() =>
                {
                    while (!cts.IsCancellationRequested)
                    {
                        Thread.Sleep(3000);
                        if ((DateTime.UtcNow - lastHeartbeat).TotalSeconds >= 15f)
                        {
                            //HeartBeat = false;
                            cts.Cancel();
                            break;
                        }

                    }
                });

                string acountName =null;
                while (true)
                {
                    int len = await socket.ReceiveAsync(buffer,SocketFlags.None,cts.Token);
                    if (len == 0)
                    {
                        break;
                    }
                    lastHeartbeat = DateTime.UtcNow;
                    int index = 0;
                    int AllLen;
                    if (_currentMsgLength == -1)//说明现在的数据是从数据头开始的
                    {

                        AllLen = BitConverter.ToInt32(buffer, index);
                        while (true)
                        {
                            if (len > AllLen )//黏包
                            {
                                index += 4;//排除长度数据头
                                acountName = await ProcessMessages(socket, acountName,index, AllLen, buffer);
                                index += AllLen-4;
                                len -= AllLen;
                                if (len <= 0)
                                {
                                    _currentMsgLength = -1; break;
                                }
                                AllLen = BitConverter.ToInt32(buffer, index);
                            }
                            else if(len < AllLen)//分包
                            {
                                Array.Copy(buffer, index, _recvBuffer, 0, len);
                                _currentMsgLength = len;
                                break;
                            }
                            else//整包
                            {
                                index += 4;
                                acountName = await ProcessMessages(socket, acountName, index, AllLen, buffer);
                                _currentMsgLength = -1;
                                break;
                            }
                        }
                    }
                    else
                    {
                        AllLen = BitConverter.ToInt32(_recvBuffer);
                        while (true)
                        {
                            if (len+_currentMsgLength > AllLen)//黏包
                            {
                                Array.Copy(buffer, index, _recvBuffer, _currentMsgLength, AllLen - _currentMsgLength);
                                index += 4;//排除长度数据头
                                acountName = await ProcessMessages(socket, acountName, 4, AllLen, _recvBuffer);
                                index += AllLen - 4;
                                len -= (AllLen-_currentMsgLength);
                                if(len <= 0)
                                {
                                    _currentMsgLength = -1; break;
                                }
                                AllLen = BitConverter.ToInt32(buffer, index - _currentMsgLength);
                                if(_currentMsgLength!=0)
                                {
                                    index -= _currentMsgLength;
                                    _currentMsgLength = 0;
                                }

                            }
                            else if (len+_currentMsgLength < AllLen)//分包
                            {
                                Array.Copy(buffer, 0, _recvBuffer, _currentMsgLength, len);
                                if(_currentMsgLength==0)
                                {
                                    _currentMsgLength = len;
                                }
                                else _currentMsgLength += len;
                                break;
                            }
                            else//整包
                            {
                                Array.Copy(buffer,0, _recvBuffer, _currentMsgLength, AllLen- _currentMsgLength);
                                index += 4;
                                acountName = await ProcessMessages(socket, acountName, index, AllLen, _recvBuffer);
                                _currentMsgLength = -1;
                                break;
                            }
                        }
                    }

                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[{socket.RemoteEndPoint}] 异常: {ex.Message}");
            }
            finally
            {
                // 断线清理：从房间移除该 socket，防止死 socket 残留
                foreach(var key in new List<int>(hosts.Keys))
                {
                    if (hosts[key] == socket)
                    {
                        hosts.Remove(key);
                        visitors.Remove(key);
                        Console.WriteLine($"[{socket.RemoteEndPoint}] 移除房间 {key}");
                    }
                }
                foreach(var key in new List<int>(visitors.Keys))
                {
                    if(visitors.TryGetValue(key, out var list))
                    {
                        list.RemoveAll(s=> s == socket);
                        if (list.Count == 0)
                        {
                            visitors.Remove(key);
                            Console.WriteLine($"[{socket.RemoteEndPoint}] 移除房间 {key}");
                        }
                    }
                }
                clientName.Remove(socket);

                socket.Close();
                Console.WriteLine($"[{socket.RemoteEndPoint}] 下线");
            }

        }

        //验证账号是否正确
        static bool AcountCheck(List<AcountData> acountList, byte[] acountBytes)
        {
            int index = 4;
            index += 4;
            int len = BitConverter.ToInt32(acountBytes, index);
            index += 4;

            string acountName = Encoding.UTF8.GetString(acountBytes, index, len);
            index += len;

            len = BitConverter.ToInt32(acountBytes, index);
            index += 4;

            string password = Encoding.UTF8.GetString(acountBytes, index, len);
            index += len;

            foreach (var acount in acountList)
            {
                if(acount.acountName == acountName && acount.password == password)
                {
                    Console.WriteLine("登录成功");
                    return true;
                }
            }
            Console.WriteLine("登录失败");
            return false;
        }

        //处理用户注册请求
        static bool AcountRegister(List<AcountData> acountList, byte[] acountBytes)
        {
            Console.WriteLine("收到客户端发来的注册请求");
            int index = 4;
            index += 4;
            int len = BitConverter.ToInt32(acountBytes, index);
            index += 4;

            string acountName = Encoding.UTF8.GetString(acountBytes, index, len);
            index += len;

            len = BitConverter.ToInt32(acountBytes, index);
            index += 4;

            string password = Encoding.UTF8.GetString(acountBytes, index, len);
            index += len;
            if (acountList.Exists(a => a.acountName == acountName))
            {
                Console.WriteLine("注册失败，账号已存在");
                return false;
            }
            else
            {
                acountList.Add(new AcountData(acountName,password));
                Console.WriteLine("注册成功");
                AcountPlayerData.Instance.AddAcount(acountName);
                return true;
            }
        }

        //public void SocketInit()
        //{


        //    Task.Run(() =>
        //    {
        //        while (true)
        //        {
        //            Socket clientSocket = socket.Accept();
        //            clients.Add(clientSocket);
        //            Console.WriteLine("Client connected: " + clientSocket.RemoteEndPoint.ToString());
        //            clientSocket.Send(Encoding.UTF8.GetBytes(clientSocket.RemoteEndPoint.ToString() + " connected to server."));
        //        }
        //    });
        //}


        //处理黏包分包
        public void SplitPackets(byte[] buffer)
        {

        }

        public void HandlerBuffer(byte[] buffer)
        {

        }

        public static async Task<string> ProcessMessages(Socket socket,string acountName,int index,int len,byte[] buffer)
        {
            int msgStart = index - 4;
            int type = BitConverter.ToInt32(buffer, index);
            index += 4;
            byte[] response;
            switch ((MsgType)type)
            {
                case MsgType.Login:
                    {//登录信息
                        if (AcountCheck(acountList, buffer))
                        {
                            Console.WriteLine("收到登录信息");
                            len = BitConverter.ToInt32(buffer, index);
                            index += 4;
                            acountName = Encoding.UTF8.GetString(buffer, index, len);
                            byte[] namebytes = Encoding.UTF8.GetBytes(acountName);
                            response = new byte[12 + PlayerData.GetByteSize() + 4 + namebytes.Length];
                            BitConverter.GetBytes(4 + 4 + 4 + PlayerData.GetByteSize()+4+namebytes.Length).CopyTo(response,0);
                            BitConverter.GetBytes(0).CopyTo(response, 4);//消息类型
                            BitConverter.GetBytes(1).CopyTo(response, 8);//处理结果
                            PlayerData data = AcountPlayerData.Instance.GetAcountPlayerData(acountName);
                            if (data != null)
                            {
                                data.GetPlayerDataBytes().CopyTo(response, 12);//结果数据（玩家数据）
                            }
                            BitConverter.GetBytes(namebytes.Length).CopyTo(response, 56);
                            namebytes.CopyTo(response, 60);

                            clientName[socket]=acountName;

                            await socket.SendAsync(response);
                        }
                        else
                        {
                            response = new byte[12];
                            BitConverter.GetBytes(4+4+4).CopyTo(response, 0);
                            BitConverter.GetBytes(0).CopyTo(response, 4);//消息类型
                            BitConverter.GetBytes(0).CopyTo(response, 8);//处理结果

                            await socket.SendAsync(response);
                        }
                        break;
                    }
                case MsgType.Register://注册信息
                    {
                        Console.WriteLine("收到注册信息");
                        if (AcountRegister(acountList, buffer))
                        {
                            response = new byte[12];
                            BitBaseInfo(response, 4 + 4 + 4, 1, 1);

                            await socket.SendAsync(response);
                        }
                        else
                        {
                            response = new byte[12];
                            BitBaseInfo(response, 4 + 4 + 4, 1, 0);

                            await socket.SendAsync(response);
                        }
                        break;
                    }
                case MsgType.Update://更新数据信息
                    {
                        Console.WriteLine("收到更新数据信息");
                        if (acountName == null) { Console.WriteLine("未知账号"); break; }
                        PlayerData.UpdataPlayerData(acountName, buffer);
                        SaveHelper.Save(AcountPlayerData.Instance.playerDatas);
                    }
                    break;
                case MsgType.Atk:
                    {
                        Console.WriteLine("收到攻击信息");
                        int enemyIndex = BitConverter.ToInt32(buffer, index);
                        float damage = AcountPlayerData.Instance.GetAcountPlayerData(acountName).GetDamage();
                        response = new byte[16];
                        BitBaseInfo(response, 4 + 4 + 4 + 4, 5, (int)damage);
                        BitConverter.GetBytes(enemyIndex).CopyTo(response, 12);
                        await socket.SendAsync(response);
                    }
                    break;
                case MsgType.HeartBeat://心跳消息
                    {
                        Console.WriteLine($"来自:{acountName}心跳消息");
                    }
                    break;
                case MsgType.Hurt:
                    {
                        response = new byte[16];
                        int dead = 0;
                        int damage = BitConverter.ToInt32(buffer,index);
                        if (!AcountPlayerData.Instance.playerDatas.TryGetValue(acountName, out var pd) || pd == null) break;
                        int hp = pd.hp -= damage;
                        if (hp <= 0)
                        {
                            hp = 0;
                            dead = 1;
                        }
                        BitConverter.GetBytes(16).CopyTo(response, 0);
                        BitConverter.GetBytes(95).CopyTo(response, 4);//消息类型
                        BitConverter.GetBytes(hp).CopyTo(response, 8);
                        BitConverter.GetBytes(dead).CopyTo(response, 12);
                        await socket.SendAsync(response);
                    }
                    break;
                case MsgType.VisitorHurt:
                    {

                        int dead = 0;
                        int roomid = BitConverter.ToInt32(buffer,index);
                        index += 4;
                        int damage = BitConverter.ToInt32(buffer,index);
                        index += 4;
                        int nameLen = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        string name = Encoding.UTF8.GetString(buffer, index, nameLen);
                        index += nameLen;
                        response = new byte[16+4+nameLen];
                        if (!AcountPlayerData.Instance.playerDatas.TryGetValue(name, out var pd) || pd == null) break;
                        int hp = pd.hp -= damage;
                        if (hp <= 0)
                        {
                            hp = 0;
                            dead = 1;
                        }
                        BitConverter.GetBytes(16 + 4 + nameLen).CopyTo(response, 0);
                        BitConverter.GetBytes(96).CopyTo(response, 4);//消息类型
                        BitConverter.GetBytes(hp).CopyTo(response, 8);
                        BitConverter.GetBytes(dead).CopyTo(response, 12);

                        BitConverter.GetBytes(nameLen).CopyTo(response, 16);
                        Encoding.UTF8.GetBytes(name).CopyTo(response, 20);
                        if (!visitors.TryGetValue(roomid, out var list)) break;
                        foreach (Socket visitor in list)
                        {
                            if(visitor !=null)
                                await visitor.SendAsync(response);
                        }
                    }
                    break;
                case MsgType.VisitorAtk:
                    {
                        int roomid = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        int Monsterid = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        int nameLen = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        string name = Encoding.UTF8.GetString(buffer, index, nameLen);
                        index += nameLen;
                        response = new byte[16 + 4 + nameLen];
                        if (!AcountPlayerData.Instance.playerDatas.TryGetValue(name, out var pd) || pd == null) break;
                        int damage = (int)pd.GetDamage();
                        BitConverter.GetBytes(16 + 4 + nameLen).CopyTo(response, 0);
                        BitConverter.GetBytes(97).CopyTo(response, 4);//消息类型
                        BitConverter.GetBytes(Monsterid).CopyTo(response, 8);
                        BitConverter.GetBytes(damage).CopyTo(response, 12);
                        BitConverter.GetBytes(nameLen).CopyTo(response, 16);
                        Encoding.UTF8.GetBytes(name).CopyTo(response, 20);
                        if (!visitors.TryGetValue(roomid, out var list)) break;
                        foreach (Socket visitor in list)
                        {
                            if (visitor != null)
                                await visitor.SendAsync(response);
                        }
                    }
                    break;
                case MsgType.MultiSync:
                    {
                        Console.WriteLine($"收到同步 roomid={BitConverter.ToInt32(buffer, index)}");
                        Console.WriteLine("多人同步消息");
                        int roomid = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        if (!hosts.ContainsKey(roomid))
                        {
                            Console.WriteLine("房间不存在");
                            return acountName;// 别 return null：外层靠返回值维持登录态，置 null 会让后续消息 NRE
                        }

                        Console.WriteLine("向房主发送数据中");
                        response = new byte[4+4+4+12+4+4+Encoding.UTF8.GetBytes(acountName).Length+PlayerData.GetByteSize()];
                        BitConverter.GetBytes(4 + 4 + 4 + 12 + 4+ 4 + Encoding.UTF8.GetBytes(acountName).Length + PlayerData.GetByteSize()).CopyTo(response, 0);
                        BitConverter.GetBytes(98).CopyTo(response, 4);//消息类型
                        BitConverter.GetBytes(roomid).CopyTo(response, 8);//房间号
                        Array.Copy(buffer,index, response, 12, 12);//位置
                        index += 12;
                        float y = BitConverter.ToSingle(buffer, index);
                        BitConverter.GetBytes(y).CopyTo(response, index);//旋转
                        index += 4;
                        BitConverter.GetBytes(Encoding.UTF8.GetBytes(acountName).Length) .CopyTo(response, index);//账号名长度
                        index += 4;
                        Encoding.UTF8.GetBytes(acountName).CopyTo(response, index);//账号名
                        index += Encoding.UTF8.GetBytes(acountName).Length;
                        if (!AcountPlayerData.Instance.playerDatas.TryGetValue(acountName, out var self)) break;
                        self.GetPlayerDataBytes().CopyTo(response, index);
                        if (!visitors.TryGetValue(roomid, out var list)) break;
                        foreach (Socket visitor in list)
                        {
                            if (visitor != socket)
                            {
                                visitor.Send(response);
                            }
                        }
                    }
                    break;
                case MsgType.Multiplayer:
                    {
                        Console.WriteLine($"来自{acountName}联机请求");
                        int roomid = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        if (hosts.ContainsKey(roomid) && hosts[roomid]!=null)
                        {
                            response = new byte[16];
                            BitBaseInfo(response, 4 + 4 + 4 + 4, 99, 1);
                            BitConverter.GetBytes(roomid).CopyTo(response, 12);
                            await socket.SendAsync(response);
                            visitors[roomid].Add(socket);
                        }
                        else
                        {
                            response = new byte[16];
                            BitBaseInfo(response, 4 + 4 + 4 + 4, 99, 0);
                            BitConverter.GetBytes(roomid).CopyTo(response, 12);
                            await socket.SendAsync(response);
                            hosts.Add(roomid,socket);
                            visitors.Add(roomid, new List<Socket>());
                            visitors[roomid].Add(socket);
                        }
                    }
                    break;
                case MsgType.MonsterSync:
                    {
                        Console.WriteLine($"来自{acountName}怪物创建/同步请求");
                        int roomid = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        if (!visitors.TryGetValue(roomid, out var list)) break;
                        foreach (Socket visitor in list)
                        {
                            if (visitor != socket)
                            {
                                visitor.Send(buffer, msgStart,len, SocketFlags.None);
                            }
                        }
                    }
                    break;
                case MsgType.Possess:
                    {

                        int roomid = BitConverter.ToInt32(buffer, index);
                        index += 4;

                        // 原来写的是 if (visitors[roomid] != null)：索引器在判断之前就会抛
                        // KeyNotFound，异常被外层 catch 吃掉后 finally 会把这个连接踢掉。
                        // 房间/房主不存在就静默忽略本条消息。
                        if (!visitors.TryGetValue(roomid, out _)) break;
                        if (!hosts.TryGetValue(roomid, out var host) || host == null) break;

                        byte[] str = Encoding.UTF8.GetBytes(acountName);
                        response = new byte[24+str.Length];
                        BitConverter.GetBytes(24 + str.Length).CopyTo(response, 0);//总长
                        Array.Copy(buffer, msgStart + 4, response, 4, 4);//类型
                        Array.Copy(buffer, msgStart + 8, response, 8, 4);//房间号
                        BitConverter.GetBytes(str.Length).CopyTo(response, 12);//玩家名长度
                        str.CopyTo(response, 16);//玩家名
                        Array.Copy(buffer, msgStart + 12, response, 16+str.Length, 8);//

                        await host.SendAsync(response);
                    }
                    break;
                case MsgType.PossessOK:
                    {
                        //4玩家名+4（附身/解除）+4结果
                        int roomid = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        int strlen = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        string name = Encoding.UTF8.GetString(buffer, index, strlen);
                        if (!visitors.TryGetValue(roomid, out var list)) break;
                        foreach (Socket visitor in list)
                        {
                            if (clientName.TryGetValue(visitor, out var vname) && vname == name)
                            {
                                visitor.Send(buffer, msgStart, len, SocketFlags.None);
                                break;
                            }
                        }
                        

                    }
                    break;
                case MsgType.PossessState:
                    {
                        //4附身/解除附身的怪物id+12（vector3）怪物位置+4旋转yaw
                        int roomid = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        // 房主掉线后 hosts 里已没有该房间，而访客的 SendPossessStateLoop 还在发
                        // → 裸索引抛 KeyNotFound → 访客连接被 finally 踢掉。查不到就丢弃这条。
                        if (hosts.TryGetValue(roomid, out var host) && host != null)
                        {
                            host.Send(buffer,msgStart, len, SocketFlags.None);
                        }
                    }
                    break;

                case MsgType.PlayerAnimation:
                    {
                        int roomid = BitConverter.ToInt32(buffer,index);
                        index += 4;
                        int animationId = BitConverter.ToInt32(buffer, index); 
                        index += 4;
                        int combo = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        byte[] namebytes = Encoding.UTF8.GetBytes(acountName);
                        response =new byte[20+namebytes.Length];
                        BitConverter.GetBytes(20 + namebytes.Length).CopyTo(response,0);
                        BitConverter.GetBytes(200).CopyTo(response, 4);
                        BitConverter.GetBytes(animationId).CopyTo(response, 8);
                        BitConverter.GetBytes(combo).CopyTo(response, 12);
                        BitConverter.GetBytes(namebytes.Length).CopyTo(response, 16);
                        namebytes.CopyTo(response, 20);
                        if (!visitors.TryGetValue(roomid, out var list)) break;
                        foreach (Socket visitor in list)
                        {
                            if (visitor != null)
                                await visitor.SendAsync(response);
                        }
                    }
                    break;
                case MsgType.MonsterAnimation:
                    {
                        int roomid =BitConverter.ToInt32(buffer,index);
                        index += 4;
                        int animationId = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        int combo = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        float posX = BitConverter.ToSingle(buffer, index);
                        index += 4;
                        float posY = BitConverter.ToSingle(buffer, index);
                        index += 4;
                        float posZ = BitConverter.ToSingle(buffer, index);
                        index += 4;
                        float yaw = BitConverter.ToSingle(buffer, index);
                        index += 4;
                        int monsterid = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        Console.WriteLine($"收到怪物动画 roomid={roomid} animationId={animationId} combo={combo} monsterid={monsterid} 来自={acountName}");
                        byte[] namebytes = Encoding.UTF8.GetBytes(acountName);
                        response =new byte[40+namebytes.Length];
                        BitConverter.GetBytes(40+namebytes.Length).CopyTo(response,0);
                        BitConverter.GetBytes(201).CopyTo(response, 4);
                        BitConverter.GetBytes(animationId).CopyTo (response, 8);
                        BitConverter.GetBytes(combo).CopyTo(response, 12);
                        BitConverter.GetBytes(posX).CopyTo(response, 16);
                        BitConverter.GetBytes(posY).CopyTo(response, 20);
                        BitConverter.GetBytes(posZ).CopyTo(response, 24);
                        BitConverter.GetBytes(yaw).CopyTo(response, 28);
                        BitConverter.GetBytes(monsterid).CopyTo (response, 32);
                        BitConverter.GetBytes(namebytes.Length).CopyTo(response, 36);
                        namebytes.CopyTo(response, 40);
                        if (!visitors.TryGetValue(roomid, out var list))
                        {
                            Console.WriteLine($"怪物动画中转失败：房间 {roomid} 无访客列表");
                            break;
                        }
                        Console.WriteLine($"怪物动画中转给 {list.Count} 人");
                        foreach (Socket visitor in list)
                        {
                            if (visitor != null)
                                await visitor.SendAsync(response);
                        }
                    }
                    break;

                // 商店购买：客户端发 [4 itemId]，服务器查价格表扣钱，回权威金币
                case MsgType.ShopBuy:
                    {
                        int itemId = BitConverter.ToInt32(buffer, index);   // 客户端只发商品 id
                        PlayerData pd = acountName == null ? null : AcountPlayerData.Instance.GetAcountPlayerData(acountName);
                        int result = 0;
                        // 三个条件都满足才算成功：玩家存在 + 商品在价格表里 + 金币够
                        if (pd != null && shopPrices.TryGetValue(itemId, out int price) && pd.money1 >= price)
                        {
                            pd.money1 -= price;
                            result = 1;
                            SaveHelper.Save(AcountPlayerData.Instance.playerDatas);   // 扣完钱落盘
                        }
                        // 回包布局：[4 总长][4 type=8][4 result][4 itemId][4 新金币]
                        response = new byte[20];
                        BitBaseInfo(response, 20, 8, result);                       // 写 总长/类型/结果
                        BitConverter.GetBytes(itemId).CopyTo(response, 12);
                        BitConverter.GetBytes(pd == null ? 0 : pd.money1).CopyTo(response, 16);   // 扣完钱的权威金币
                        await socket.SendAsync(response);
                    }
                    break;

                // 任务领奖：客户端发 [4 taskId]，服务器查奖励表发金币+经验
                case MsgType.TaskClaim:
                    {
                        int taskId = BitConverter.ToInt32(buffer, index);
                        PlayerData pd = acountName == null ? null : AcountPlayerData.Instance.GetAcountPlayerData(acountName);
                        int result = 0;
                        // 注意：这里没有验证"任务是否真的完成"（服务器没存任务进度），
                        // 只负责发奖励。要真正防作弊得让服务器也记任务进度（TODO）。
                        if (pd != null && taskRewards.TryGetValue(taskId, out var reward))
                        {
                            pd.money1 += reward.gold;
                            pd.levelNum += reward.exp;
                            result = 1;
                            SaveHelper.Save(AcountPlayerData.Instance.playerDatas);
                        }
                        // 回包布局：[4 总长][4 type=9][4 result][4 taskId][4 新金币][4 新经验]
                        response = new byte[24];
                        BitBaseInfo(response, 24, 9, result);
                        BitConverter.GetBytes(taskId).CopyTo(response, 12);
                        BitConverter.GetBytes(pd == null ? 0 : pd.money1).CopyTo(response, 16);
                        BitConverter.GetBytes(pd == null ? 0 : pd.levelNum).CopyTo(response, 20);
                        await socket.SendAsync(response);
                    }
                    break;
            }
            return acountName;
        }
        public static void BitBaseInfo(byte[] buffer, int AllLength, int type,int result)
        {
            BitConverter.GetBytes(AllLength).CopyTo(buffer, 0);
            BitConverter.GetBytes(type).CopyTo(buffer, 4);//消息类型
            BitConverter.GetBytes(result).CopyTo(buffer, 8);//处理结果
        }
    }

}
