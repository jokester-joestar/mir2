using System;
using System.IO;
using System.Diagnostics;

namespace Client.Utils
{
    public class Download
    {
        public FileInformation Info;
        public long CurrentBytes;
        public bool Completed;
    }

    public class FileInformation
    {
        public string FileName; //Relative.
        public int Length, Compressed;
        public DateTime Creation;

        public FileInformation()
        {
        }

        public FileInformation(BinaryReader reader)
        {
            FileName = reader.ReadString();
            Length = reader.ReadInt32();
            Compressed = reader.ReadInt32();

            long dateData = reader.ReadInt64();
            bool parsed;
            Creation = ParseDateTimeFromLong(dateData, out parsed);

            if (!parsed)
            {
                // 记录详细信息，便于排查（可以替换为项目内的日志系统）
                try
                {
                    var pos = reader.BaseStream?.Position ?? -1;
                    Trace.TraceWarning("无法解析日期数据。value={0}, file={1}, streamPosition={2}", dateData, FileName, pos);
                }
                catch { /* 忽略日志错误 */ }

                // 备用策略：使用最安全的默认值
                Creation = DateTime.MinValue;
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(FileName);
            writer.Write(Length);
            writer.Write(Compressed);
            writer.Write(Creation.ToBinary());
        }

        private static DateTime ParseDateTimeFromLong(long dateData, out bool parsed)
        {
            parsed = false;

            // 尝试按 FromBinary（与 ToBinary 配对）
            try
            {
                DateTime dt = DateTime.FromBinary(dateData);
                parsed = true;
                return dt;
            }
            catch (ArgumentException) { }

            // 如果可能是 ticks
            if (dateData >= DateTime.MinValue.Ticks && dateData <= DateTime.MaxValue.Ticks)
            {
                parsed = true;
                return new DateTime(dateData, DateTimeKind.Utc);
            }

            // 尝试当作 FILETIME
            try
            {
                DateTime dt = DateTime.FromFileTime(dateData);
                parsed = true;
                return dt;
            }
            catch { }

            // 解析失败，返回占位值
            return DateTime.MinValue;
        }
    }
}
