using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace _3DArpgServer
{
    /// <summary>
    /// 存档：把内存里的玩家数据落到 JSON 文件（save.json），服务器重启不丢。
    ///
    /// 为什么先落 JSON 而不是 SQL：快、零配置、能立刻跑起来演示"重启数据还在"。
    /// 以后要换 SQL Server 只改这一个类，别的代码都不用动。
    /// 存档文件位置：和 3DArpgServer.exe 同目录（AppContext.BaseDirectory）。
    /// </summary>
    public static class SaveHelper
    {
        static readonly string SavePath = Path.Combine(AppContext.BaseDirectory, "save.json");

        static JsonSerializerOptions options = new JsonSerializerOptions
        {
            // PlayerData 是公有字段（public int level 这种），
            // 默认 System.Text.Json 只序列化属性不序列化字段，必须开这个开关。
            IncludeFields = true,
            WriteIndented = true   // 缩进格式化，方便人肉看
        };

        /// <summary>把整份玩家数据字典序列化写盘。每次数据变更后调用。</summary>
        public static void Save(Dictionary<string, PlayerData> datas)
        {
            try
            {
                File.WriteAllText(SavePath, JsonSerializer.Serialize(datas, options));
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine($"[Save] 保存失败: {e.Message}");
            }
        }

        /// <summary>启动时读盘。没有存档文件返回 null（调用方当"首次启动"处理）。</summary>
        public static Dictionary<string, PlayerData> Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                return JsonSerializer.Deserialize<Dictionary<string, PlayerData>>(File.ReadAllText(SavePath), options);
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine($"[Save] 读取失败: {e.Message}");
                return null;
            }
        }
    }
}
