﻿/*
	Copyright 2011 MCForge
		
	Dual-licensed under the	Educational Community License, Version 2.0 and
	the GNU General Public License, Version 3 (the "Licenses"); you may
	not use this file except in compliance with the Licenses. You may
	obtain a copy of the Licenses at
	
	http://www.opensource.org/licenses/ecl2.php
	http://www.gnu.org/licenses/gpl-3.0.html
	
	Unless required by applicable law or agreed to in writing,
	software distributed under the Licenses are distributed on an "AS IS"
	BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
	or implied. See the Licenses for the specific language governing
	permissions and limitations under the Licenses.
*/
using System;
using System.Net;
using Sharkbite.Irc;
namespace fCraft
{
    public class GlobalChatBot
    {
        public delegate void RecieveChat(string nick, string message);
        public static event RecieveChat OnNewRecieveGlobalMessage;

        public delegate void SendChat(string player, string message);
        public static event SendChat OnNewSayGlobalMessage;

        public delegate void KickHandler(string reason);
        public event KickHandler OnGlobalKicked;

        private Connection connection;
        private string server, channel, nick;
        private bool reset = false;
        private byte retries = 0;

        const string caps = "ABCDEFGHIJKLMNOPQRSTUVWXYZ ";
        const string nocaps = "abcdefghijklmnopqrstuvwxyz ";
        public GlobalChatBot(string nick)
        {
            /*if (!File.Exists("Sharkbite.Thresher.dll"))
            {
                Server.UseGlobalChat = false;
                Server.s.Log("[GlobalChat] The IRC dll was not found!");
                return;
            }*/
            server = "irc.esper.net";
            channel = "#MCForge-irc";
            this.nick = nick.Replace(" ", "");
            connection = new Connection(new ConnectionArgs(nick, server), false, false);

            if (Server.UseGlobalChat)
            {
                // Regster events for incoming
                connection.Listener.OnNickError += new NickErrorEventHandler(Listener_OnNickError);
                connection.Listener.OnRegistered += new RegisteredEventHandler(Listener_OnRegistered);
                connection.Listener.OnPublic += new PublicMessageEventHandler(Listener_OnPublic);
                connection.Listener.OnJoin += new JoinEventHandler(Listener_OnJoin);
                connection.Listener.OnKick += new KickEventHandler(Listener_OnKick);
                connection.Listener.OnError += new ErrorMessageEventHandler(Listener_OnError);
                connection.Listener.OnDisconnected += new DisconnectedEventHandler(Listener_OnDisconnected);
            }
        }
        public void Say(string message, Player p = null)
        {
            RemoveVariables(ref message);
            RemoveWhitespace(ref message);
            if (String.IsNullOrEmpty(message.Replace("Console:", "").Trim()))
            {
                Player.Message("You should send some text!");
                return;
            }

            if (OnNewSayGlobalMessage != null)
                OnNewSayGlobalMessage(p == null ? "Console" : p.name, message);

            if (Server.UseGlobalChat && IsConnected())
                connection.Sender.PublicMessage(channel, message);
        }


        public void Pm(string user, string message)
        {
            if (Server.UseGlobalChat && IsConnected())
                connection.Sender.PrivateMessage(user, message);
        }


        public void Reset()
        {
            if (!Server.UseGlobalChat)
                return;
            reset = true;
            retries = 0;
            Disconnect("Global Chat bot resetting...");
            Connect();
        }

        void Listener_OnJoin(UserInfo user, string channel)
        {
            if (user.Nick == nick)
                Server.s.Log("Joined the Global Chat!");
        }

        void Listener_OnError(ReplyCode code, string message)
        {
            switch (code)
            {
                case ReplyCode.ERR_BANNEDFROMCHAN:
                    Server.s.Log("Your server is banned from the Global Chat Channel. Please appeal at mcforge.org");
                    break;
                case ReplyCode.ERR_INVITEONLYCHAN:
                    Server.s.Log("Cannot join Global Chat. (Channel is invite only (+i))");
                    break;
                case ReplyCode.ERR_YOUREBANNEDCREEP:
                    {
                        if (Server.irc) { if (Server.ircServer == server) return; }
                        Server.s.Log(message);
                        Server.s.Log("This means your server is banned from the Global Chat server, please contact a MCForge Staff member for an unban.");
                    }
                    break;
            }
        }

        void Listener_OnPublic(UserInfo user, string channel, string message)
        {
            //string allowedchars = "1234567890-=qwertyuiop[]\\asdfghjkl;'zxcvbnm,./!@#$%^*()_+QWERTYUIOPASDFGHJKL:\"ZXCVBNM<>? ";
            //string msg = message;
            RemoveVariables(ref message);
            RemoveWhitespace(ref message);

            if (message.Contains("^UGCS"))
            {
                Server.UpdateGlobalSettings();
                return;
            }
            if (message.Contains("^IPGET "))
            {
                foreach (Player p in Player.players)
                {
                    if (p.name == message.Split(' ')[1])
                    {
                        if (Server.UseGlobalChat && IsConnected())
                        {
                            if (Player.IsLocalIpAddress(p.ip))
                            {
                                connection.Sender.PublicMessage(channel, "^IP " + p.name + ": " + Server.IP);
                                connection.Sender.PublicMessage(channel, "^PLAYER IS CONNECTING THROUGH A LOCAL IP.");
                            }
                            else { connection.Sender.PublicMessage(channel, "^IP " + p.name + ": " + p.ip); }
                        }
                    }
                }
            }
            if (message.Contains("^SENDRULES "))
            {
                Player who = Player.Find(message.Split(' ')[1]);
                if (who != null)
                {
                    Command.all.Find("gcrules").Use(who, "");
                }
            }
            if (message.Contains("^GETINFO "))
            {
                if (Server.GlobalChatNick == message.Split(' ')[1])
                {
                    if (Server.UseGlobalChat && IsConnected())
                    {
                        connection.Sender.PublicMessage(channel, "^NAME: " + Server.name);
                        connection.Sender.PublicMessage(channel, "^MOTD: " + Server.motd);
                        connection.Sender.PublicMessage(channel, "^VERSION: " + Server.Version);
                        connection.Sender.PublicMessage(channel, "^GLOBAL NAME: " + Server.GlobalChatNick);
                        connection.Sender.PublicMessage(channel, "^URL: " + Server.CCURL);
                        connection.Sender.PublicMessage(channel, "^PLAYERS: " + Player.players.Count + "/" + Server.players);
                    }
                }
            }

            //for RoboDash's anti advertise/swear in #globalchat
            if (message.Contains("^ISASERVER "))
            {
                if (Server.GlobalChatNick == message.Split(' ')[1])
                {
                    connection.Sender.PublicMessage(channel, "^IMASERVER");
                }
            }

            if (message.StartsWith("^"))
                return;

            message = message.MCCharFilter();

            if (String.IsNullOrEmpty(message))
                return;

            if (Player.MessageHasBadColorCodes(null, message))
                return;

            if (OnNewRecieveGlobalMessage != null)
                OnNewRecieveGlobalMessage(user.Nick, message);

            if (Server.Devs.Contains(message.Split(':')[0].ToLower()) && !message.StartsWith("[Dev]") && !message.StartsWith("[Developer]"))
                message = "[Dev]" + message;
            else if (Server.Mods.Contains(message.Split(':')[0].ToLower()) && !message.StartsWith("[Mod]") && !message.StartsWith("[Moderator]"))
                message = "[Mod]" + message;
            else if (Server.Mods.Contains(message.Split(':')[0].ToLower()) && !message.StartsWith("[GCMod]"))
                message = "[GCMod]" + message;

            /*try { 
                if(GUI.GuiEvent != null)
                GUI.GuiEvents.GlobalChatEvent(this, "> " + user.Nick + ": " + message); }
            catch { Server.s.Log(">[Global] " + user.Nick + ": " + message); }*/
            Player.GlobalMessage(Player.MessageType.Chat, String.Format("{0}>[Global] {1}: &f{2}", Server.GlobalChatColor, user.Nick, Server.profanityFilter ? ProfanityFilter.Parse(message) : message), true);
        }

        void Listener_OnRegistered()
        {
            reset = false;
            retries = 0;
            connection.Sender.Join(channel);
        }

        void Listener_OnDisconnected()
        {
            if (!reset && retries < 5) { retries++; Connect(); }
        }

        void Listener_OnNickError(string badNick, string reason)
        {
            Server.s.Log("Global Chat nick \"" + badNick + "\" is  taken, please choose a different nick.");
        }

        void Listener_OnKick(UserInfo user, string channel, string kickee, string reason)
        {
            if (kickee.Trim().ToLower() == nick.ToLower())
            {
                Server.s.Log("Kicked from Global Chat: " + reason);
                if (OnGlobalKicked != null)
                    OnGlobalKicked(reason);
                Server.s.Log("Attempting to rejoin...");
                connection.Sender.Join(channel);
            }

        }

        public void Connect()
        {
            if (!Server.UseGlobalChat || Server.shuttingDown)
                return;
            try { connection.Connect(); }
            catch { }
        }

        public void Disconnect(string reason)
        {
            if (IsConnected()) { connection.Disconnect(reason); Server.s.Log("Disconnected from Global Chat!"); }
        }

        public bool IsConnected()
        {
            if (!Server.UseGlobalChat)
                return false;
            try { return connection.Connected; }
            catch { return false; }
        }

        private void RemoveWhitespace(ref string message)
        {
            string[] msg = message.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            message = "";
            foreach (string word in msg)
                message = message + word + " ";
        }
        private void RemoveVariables(ref string message)
        {
            string[] msg = message.Split('$');
            message = "";
            foreach (string part in msg)
                message = message + part;
        }
    }
}
