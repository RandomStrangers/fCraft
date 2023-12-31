﻿// Copyright 2009-2014 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using JetBrains.Annotations;
using System.Net.Sockets;
using System.Security.Cryptography;
using static fCraft.Player;
using System.IO;
using System.Threading;

namespace fCraft {
    /// <summary> Packet struct, just a wrapper for a byte array. </summary>
    public struct Packet {
        /// <summary> ID byte used in the protocol to indicate that an action should apply to self.
        /// When used in AddEntity packet, sets player's own respawn point.
        /// When used in Teleport packet, teleports the player. </summary>
        public const sbyte SelfID = -1;

        /// <summary> Raw bytes of this packet. </summary>
        public readonly byte[] Bytes;

        /// <summary> OpCode (first byte) of this packet. </summary>
        public OpCode OpCode {
            get { return (OpCode)Bytes[0]; }
        }


        /// <summary> Creates a new packet from given raw bytes. Data not be null. </summary>
        public Packet( [NotNull] byte[] rawBytes ) {
            if( rawBytes == null ) throw new ArgumentNullException( "rawBytes" );
            Bytes = rawBytes;
        }


        /// <summary> Creates a packet of correct size for a given opCode,
        /// and sets the first (opCode) byte. </summary>
        public Packet( OpCode opCode ) {
            Bytes = new byte[PacketSizes[(int)opCode]];
            Bytes[0] = (byte)opCode;
        }


        #region Packet Making
        static System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
        static MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
        public static Socket socket;

        public static void SendRaw(OpCode id)
        {
            SendRaw(id, new byte[0]);
        }
        public static void SendRaw(OpCode id, byte send)
        {
            SendRaw(id, new byte[] { send });
        }

         static void SendRaw(OpCode id, byte[] send)
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

        public static void SendExtInfo(short count)
        {
            byte[] buffer = new byte[66];
            StringFormat("Server software: " + Server.SoftwareNameVersioned, 64).CopyTo(buffer, 0);
            HTNO(count).CopyTo(buffer, 64);
            SendRaw(OpCode.ExtInfo, buffer);
        }
        public static void SendExtEntry(string name, int version)
        {
            byte[] version_ = BitConverter.GetBytes(version);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(version_);
            byte[] buffer = new byte[68];
            StringFormat(name, 64).CopyTo(buffer, 0);
            version_.CopyTo(buffer, 64);
            SendRaw(OpCode.ExtEntry, buffer);
        }
        public static Packet MakeHandshake( [NotNull] Player player, [NotNull] string serverName, [NotNull] string motd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( serverName == null ) throw new ArgumentNullException( "serverName" );
            if( motd == null ) throw new ArgumentNullException( "motd" );

            Packet packet = new Packet( OpCode.Handshake );
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
            packet.Bytes[1] = Config.ProtocolVersion;
            Encoding.ASCII.GetBytes( serverName.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            Encoding.ASCII.GetBytes( motd.PadRight( 64 ), 0, 64, packet.Bytes, 66 );
            packet.Bytes[130] = (byte)( player.Can( Permission.DeleteAdmincrete ) ? 100 : 0 );
            return packet;
        }


        public static Packet MakeSetBlock( short x, short y, short z, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( x, packet.Bytes, 1 );
            ToNetOrder( z, packet.Bytes, 3 );
            ToNetOrder( y, packet.Bytes, 5 );
            packet.Bytes[7] = (byte)type;
            return packet;
        }


        public static Packet MakeSetBlock( Vector3I coords, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( (short)coords.X, packet.Bytes, 1 );
            ToNetOrder( (short)coords.Z, packet.Bytes, 3 );
            ToNetOrder( (short)coords.Y, packet.Bytes, 5 );
            packet.Bytes[7] = (byte)type;
            return packet;
        }


        public static Packet MakeAddEntity( sbyte id, [NotNull] string name, Position pos ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            Packet packet = new Packet( OpCode.AddEntity );
            packet.Bytes[1] = (byte)id;
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            ToNetOrder( pos.X, packet.Bytes, 66 );
            ToNetOrder( pos.Z, packet.Bytes, 68 );
            ToNetOrder( pos.Y, packet.Bytes, 70 );
            packet.Bytes[72] = pos.R;
            packet.Bytes[73] = pos.L;
            return packet;
        }


        public static Packet MakeTeleport( sbyte id, Position pos ) {
            Packet packet = new Packet( OpCode.Teleport );
            packet.Bytes[1] = (byte)id;
            ToNetOrder( pos.X, packet.Bytes, 2 );
            ToNetOrder( pos.Z, packet.Bytes, 4 );
            ToNetOrder( pos.Y, packet.Bytes, 6 );
            packet.Bytes[8] = pos.R;
            packet.Bytes[9] = pos.L;
            return packet;
        }


        public static Packet MakeSelfTeleport( Position pos ) {
            return MakeTeleport( -1, pos.GetFixed() );
        }


        public static Packet MakeMoveRotate( sbyte id, Position pos ) {
            Packet packet = new Packet( OpCode.MoveRotate );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)( pos.X & 0xFF );
            packet.Bytes[3] = (byte)( pos.Z & 0xFF );
            packet.Bytes[4] = (byte)( pos.Y & 0xFF );
            packet.Bytes[5] = pos.R;
            packet.Bytes[6] = pos.L;
            return packet;
        }


        public static Packet MakeMove( sbyte id, Position pos ) {
            Packet packet = new Packet( OpCode.Move );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)pos.X;
            packet.Bytes[3] = (byte)pos.Z;
            packet.Bytes[4] = (byte)pos.Y;
            return packet;
        }


        public static Packet MakeRotate( sbyte id, Position pos ) {
            Packet packet = new Packet( OpCode.Rotate );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = pos.R;
            packet.Bytes[3] = pos.L;
            return packet;
        }


        public static Packet MakeRemoveEntity( sbyte id ) {
            Packet packet = new Packet( OpCode.RemoveEntity );
            packet.Bytes[1] = (byte)id;
            return packet;
        }


        public static Packet MakeKick( [NotNull] string reason ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );

            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            return packet;
        }


        public static Packet MakeSetPermission( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            Packet packet = new Packet( OpCode.SetPermission );
            packet.Bytes[1] = (byte)( player.Can( Permission.DeleteAdmincrete ) ? 100 : 0 );
            return packet;
        }

        #endregion


        internal static void ToNetOrder( short number, byte[] arr, int offset ) {
            arr[offset] = (byte)( ( number & 0xff00 ) >> 8 );
            arr[offset + 1] = (byte)( number & 0x00ff );
        }


        /// <summary> Returns packet size (in bytes) for a given opCode.
        /// Size includes the opCode byte itself. </summary>
        public static int GetSize( OpCode opCode ) {
            return PacketSizes[(int)opCode];
        }


        static readonly int[] PacketSizes = {
            131, // Handshake
            1, // Ping
            1, // MapBegin
            1028, // MapChunk
            7, // MapEnd
            9, // SetBlockClient
            8, // SetBlockServer
            74, // AddEntity
            10, // Teleport
            7, // MoveRotate
            5, // Move
            4, // Rotate
            2, // RemoveEntity
            66, // Message
            65, // Kick
            2 // SetPermission
        };
    }
}