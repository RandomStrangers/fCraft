
using fCraft;
using System;
using System.Reflection.Emit;
using System.Reflection;

using System.Xml.Linq;
using System.Net.Sockets;

namespace fCraft
{
    public partial class Player
    {
        public int ClickDistance = 0;
        public int CustomBlocks = 0;
        public int HeldBlock = 0;
        public int TextHotKey = 0;
        public int ExtPlayerList = 0;
        public int EnvColors = 0;
        public int SelectionCuboid = 0;
        public int BlockPermissions = 0;
        public int ChangeModel = 0;
        public int EnvMapAppearance = 0;
        public int EnvWeatherType = 0;
        public int HackControl = 0;
        public int EmoteFix = 0;
        public int MessageTypes = 0;
        public int TwoWayPing = 0;
        public int LongerMessages = 1;
        public void AddExtension(string Extension, int version)
        {
            lock (this)
            {
                switch (Extension.Trim())
                {
                    case "ClickDistance":
                        ClickDistance = version;
                        break;
                    case "CustomBlocks":
                        CustomBlocks = version;
                        break;
                    case "HeldBlock":
                        HeldBlock = version;
                        break;
                    case "TextHotKey":
                        TextHotKey = version;
                        break;
                    case "ExtPlayerList":
                        ExtPlayerList = version;
                        break;
                    case "EnvColors":
                        EnvColors = version;
                        break;
                    case "SelectionCuboid":
                        SelectionCuboid = version;
                        break;
                    case "BlockPermissions":
                        BlockPermissions = version;
                        break;
                    case "ChangeModel":
                        ChangeModel = version;
                        break;
                    case "EnvMapAppearance":
                        EnvMapAppearance = version;
                        break;
                    case "EnvWeatherType":
                        EnvWeatherType = version;
                        break;
                    case "HackControl":
                        HackControl = version;
                        break;
                    case "EmoteFix":
                        EmoteFix = version;
                        break;
                    case "MessageTypes":
                        MessageTypes = version;
                        break;
                    case "TwoWayPing":
                        TwoWayPing = version;
                        break;
                }
            }
        }
        public void SendRaw(OpCode id)
        {
            SendRaw(id, new byte[0]);
        }
        public void SendRaw(OpCode id, byte send)
        {
            SendRaw(id, new byte[] { send });
        }
        public void SendRaw(OpCode id, byte[] send)
        {
            // Abort if socket has been closed
            if (socket == null || !socket.Connected)
                return;
            byte[] buffer = new byte[send.Length + 1];
            buffer[0] = (byte)id;
            for (int i = 0; i < send.Length; i++)
            {
                buffer[i + 1] = send[i];
            }
            try
            {
                socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, delegate (IAsyncResult result) { }, Block.Air);
                buffer = null;
            }
            catch (SocketException)
            {
                buffer = null;
                Disconnect();
            }
        }
        public static byte[] HTNO(ushort x)
        {
            byte[] y = BitConverter.GetBytes(x); Array.Reverse(y); return y;
        }
        public static ushort NTHO(byte[] x, int offset)
        {
            byte[] y = new byte[2];
            Buffer.BlockCopy(x, offset, y, 0, 2); Array.Reverse(y);
            return BitConverter.ToUInt16(y, 0);
        }
        public static byte[] HTNO(short x)
        {
            byte[] y = BitConverter.GetBytes(x); Array.Reverse(y); return y;
        }

        public static byte[] StringFormat(string str, int size)
        {
            byte[] bytes = new byte[size];
            bytes = enc.GetBytes(str.PadRight(size).Substring(0, size));
            return bytes;
        }

        public void SendExtInfo(short count)
        {
            byte[] buffer = new byte[66];
            StringFormat("Server software: " + Server.SoftwareNameVersioned, 64).CopyTo(buffer, 0);
            HTNO(count).CopyTo(buffer, 64);
            SendRaw(OpCode.ExtInfo, buffer);
        }
        public void SendExtEntry(string name, int version)
        {
            byte[] version_ = BitConverter.GetBytes(version);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(version_);
            byte[] buffer = new byte[68];
            StringFormat(name, 64).CopyTo(buffer, 0);
            version_.CopyTo(buffer, 64);
            SendRaw(OpCode.ExtEntry, buffer);
        }
        public bool HasExtension(string Extension, int version = 1)
        {
            if (!extension) return true;
            {
                extension = true;
                SendExtInfo(14);
                SendExtEntry("ClickDistance", 1);
                SendExtEntry("CustomBlocks", 1);
                SendExtEntry("HeldBlock", 1);
                SendExtEntry("TextHotKey", 1);
                SendExtEntry("ExtPlayerList", 2);
                SendExtEntry("EnvColors", 1);
                SendExtEntry("SelectionCuboid", 1);
                SendExtEntry("BlockPermissions", 1);
                SendExtEntry("ChangeModel", 1);
                SendExtEntry("EnvMapAppearance", 1);
                SendExtEntry("EnvWeatherType", 1);
                SendExtEntry("HackControl", 1);
                SendExtEntry("EmoteFix", 1);
                SendExtEntry("LongerMessages", 1);
            }
            switch (Extension)
            {
                case "ClickDistance": return ClickDistance == version;
                case "CustomBlocks": return CustomBlocks == version;
                case "HeldBlock": return HeldBlock == version;
                case "TextHotKey": return TextHotKey == version;
                case "ExtPlayerList": return ExtPlayerList == version;
                case "EnvColors": return EnvColors == version;
                case "SelectionCuboid": return SelectionCuboid == version;
                case "BlockPermissions": return BlockPermissions == version;
                case "ChangeModel": return ChangeModel == version;
                case "EnvMapAppearance": return EnvMapAppearance == version;
                case "EnvWeatherType": return EnvWeatherType == version;
                case "HackControl": return HackControl == version;
                case "EmoteFix": return EmoteFix == version;
                case "MessageTypes": return MessageTypes == version;
                case "TwoWayPing": return TwoWayPing == version;
                case "LongerMessages": return LongerMessages == version;
                default: return true;
            }
        }
    }
}
