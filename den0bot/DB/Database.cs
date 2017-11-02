﻿// den0bot (c) StanR 2017 - MIT License
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using den0bot.DB.Types;
using SQLite;

namespace den0bot.DB
{
    public static class Database
    {
        private static SQLiteConnection db;
        private static string databasePath = Path.Combine(Environment.CurrentDirectory, "data.db");

        public static void Init()
        {
            db = new SQLiteConnection(databasePath);
            db.CreateTable<Chat>();
            db.CreateTable<Meme>();
            db.CreateTable<Player>();
            db.CreateTable<Misc>();
        }
        public static void Close() => db.Close();

        // ---
        // Chats
        // ---
        public static List<Chat> GetAllChats() => db.Table<Chat>().ToList();
        public static void AddChat(long chatID)
        {
            if (db.Table<Chat>().Where(x => x.Id == chatID).FirstOrDefault() == null)
            {
                db.Insert(new Chat
                {
                    Id = chatID,
                    Banlist = string.Empty,
                    DisableAnnouncements = false
                });
                Log.Info("Database", string.Format("Added chat '{0}' to the chat list", chatID));
            }
        }

        public static void ToggleAnnouncements(long chatID, bool enable)
        {
            Chat chat = db.Table<Chat>().Where(x => x.Id == chatID).FirstOrDefault();
            if (chat != null)
            {
                chat.DisableAnnouncements = !enable;
                db.Update(chat);
            }
        }

        // ---
        // Memes
        // ---
        public static int GetMemeCount(long chatID) => db.Table<Meme>().Where(x => x.ChatID == chatID).Count();
        public static void AddMeme(string link, long chatID)
        {
            if (db.Table<Meme>().Where(x => x.Link == link).FirstOrDefault() == null)
            {
                db.Insert(new Meme
                {
                    Link = link,
                    ChatID = chatID
                });
            }
        }
        public static string GetMeme(long chatID)
        {
            List<Meme> memes = db.Table<Meme>().Where(x => x.ChatID == chatID)?.ToList();
            if (memes != null)
            {
                memes.RemoveAll(x => x.Used == true);
                if (memes.Count == 0)
                {
                    ResetUsed(chatID);
                    return GetMeme(chatID);
                }
                else
                {
                    int num = RNG.Next(memes.Count);

                    SetUsed(memes[num].Id);
                    return memes[num].Link;
                }
            }
            return null;
        }
        public static void SetUsed(int id)
        {
            Meme meme = db.Table<Meme>().Where(x => x.Id == id).First();
            if (meme != null)
            {
                meme.Used = true;
                db.Update(meme);
            }
        }
        public static void ResetUsed(long chatID)
        {
            List<Meme> memes = db.Table<Meme>().Where(x => x.ChatID == chatID)?.ToList();
            foreach (Meme meme in memes)
                meme.Used = false;

            db.UpdateAll(memes);
        }

        // ---
        // Users
        // ---
        public static List<Player> GetAllPlayers(long chatID) => db.Table<Player>().Where(x => x.ChatID == chatID)?.ToList();
        public static int GetPlayerCount() => db.Table<Player>().Count();
        public static int GetPlayerCount(long chatID) => (int)db.Table<Player>().Where(x => x.ChatID == chatID)?.Count();

        private static Player GetPlayer(int ID) => db.Table<Player>().Where(x => x.Id == ID)?.FirstOrDefault();
        private static Player GetPlayer(string username) => db.Table<Player>().Where(x => x.Username == username)?.FirstOrDefault();
        public static string GetPlayerFriendlyName(int ID) => GetPlayer(ID)?.FriendlyName;
        public static uint GetPlayerOsuID(int ID) => GetPlayer(ID)?.OsuID ?? 0;
        public static uint GetPlayerOsuID(string username) => GetPlayer(username)?.OsuID ?? 0;
        public static long GetPlayerChatID(int ID) => GetPlayer(ID)?.ChatID ?? 0;

        public static void AddPlayer(string username, string name, uint osuID, long chatID)
        {
            if (db.Table<Player>().Where(x => x.FriendlyName == name).FirstOrDefault() == null)
            {
                db.Insert(new Player
                {
                    FriendlyName = name,
                    OsuID = osuID,
                    Topscores = string.Empty,
                    ChatID = chatID,
                    Username = username
                });
            }
        }
        public static void RemovePlayer(string name, long chatID)
        {
            Player player = db.Table<Player>().Where(x => x.FriendlyName == name && x.ChatID == chatID).FirstOrDefault();
            if (player != null)
                db.Delete(player);
        }
        public static List<Osu.Score> GetPlayerTopscores(int ID)
        {
            string storedTopscores = db.Table<Player>().Where(x => x.Id == ID)?.First().Topscores;
            if (storedTopscores != null && storedTopscores != string.Empty)
            {
                List<Osu.Score> result = new List<Osu.Score>();
                foreach (string score in storedTopscores.Split(';'))
                {
                    if (score != string.Empty)
                    {
                        string scoreID = score.Split('-')[0];
                        string date = score.Split('-')[1];
                        result.Add(new Osu.Score
                        {
                            ScoreID = uint.Parse(scoreID),
                            Date = DateTime.Parse(date, CultureInfo.GetCultureInfo("en-us"))
                        });
                    }
                }
                return result;
            }
            return null;
        }
        public static void SetPlayerTopscores(List<Osu.Score> scores, int ID)
        {
            Player player = db.Table<Player>().Where(x => x.Id == ID).FirstOrDefault();
            if (player != null)
            {
                string result = string.Empty;
                foreach (Osu.Score score in scores)
                {
                    result += score.ScoreID + "-" + score.Date.ToString(CultureInfo.GetCultureInfo("en-us")) + ";";
                }
                player.Topscores = result;
                db.Update(player);
            }
        }

        // ---
        // Misc
        // ---
        public static int CurrentLobbyID
        {
            get
            {
                Misc table = db.Table<Misc>().FirstOrDefault();
                if (table != null)
                {
                    return table.CurrentMPLobby;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                Misc table = db.Table<Misc>().FirstOrDefault();
                if (table != null)
                {
                    table.CurrentMPLobby = value;
                    db.Update(table);
                }
                else
                {
                    db.Insert(new Misc
                    {
                        Hi = true,
                        CurrentMPLobby = value
                    });
                }
            
            }
        }
    }
}
