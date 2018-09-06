﻿// den0bot (c) StanR 2017 - MIT License
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Meebey.SmartIrc4net;

namespace den0bot.Osu
{
    static class IRC
    {
        private static bool isConnected = false;
        private static IrcClient irc = new IrcClient()
        {
            AutoRejoin = true,
            AutoRelogin = true,
            ActiveChannelSyncing = true,
            AutoJoinOnInvite = true
        };
        public static void Connect()
        {
            new Thread(new ThreadStart(delegate ()
            {
                irc.Connect("irc.ppy.sh", 6667);
                irc.Login(Config.osu_irc_username, "den0bot", 0, Config.osu_irc_username, Config.osu_irc_password);
                isConnected = irc.IsConnected;
                irc.Listen();
            })){ Name = "IRCThread" }.Start();

            if (isConnected)
            {
                irc.OnChannelAction += (sender, e) => OnMessage(sender, e);
                irc.OnChannelNotice += OnMessage;
                irc.OnChannelMessage += OnMessage;
                irc.OnQueryAction += (sender, e) => OnMessage(sender, e);
                irc.OnQueryNotice += OnMessage;
                irc.OnQueryMessage += OnMessage;
                irc.OnMotd += delegate (object sender, MotdEventArgs e) { Log.Info("IRC", e.Data.Message); }; ;
                irc.OnError += delegate (object sender, ErrorEventArgs e) { Log.Error("IRC", e.ErrorMessage); };
                irc.OnErrorMessage += OnMessage;
            }
        }

        public static event IrcEventHandler OnMessage;

        public static void SendMessage(string msg, string channel)
        {
            if (isConnected)
                irc.SendMessage(SendType.Message, channel, msg);
        }

        public static void Join(string name)
        {
            if (isConnected)
                irc.RfcJoin(name);
        }
        public static void Rejoin(string name)
        {
            if (isConnected)
            {
                irc.RfcPart(name);
                irc.RfcJoin(name);
            }
        }

        public static List<string> UserList(string channel)
        {
            if (isConnected)
            {
                Channel chan = irc.GetChannel(channel);
                if (chan != null)
                    return chan.Users.Keys.Cast<string>().ToList();
            }
            return null;
        }
    }
}