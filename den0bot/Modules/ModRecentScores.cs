﻿// den0bot (c) StanR 2017 - MIT License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using den0bot.DB;
using den0bot.Osu;
using Telegram.Bot.Types.Enums;

namespace den0bot.Modules
{
    class ModRecentScores : IModule
    {
        public ModRecentScores()
        {
            AddCommand(new Command
            {
                Name = "last",
                IsAsync = true,
                Reply = true,
                ActionAsync = (msg) => GetScores(msg),
                ParseMode = ParseMode.Html
            });
            Log.Info(this, "Enabled");
        }

        private async Task<string> GetScores(Telegram.Bot.Types.Message message)
        {
            string playerID = string.Empty;
            int amount = 1;

            List<string> msgSplit = message.Text.Split(' ').ToList();
            msgSplit.RemoveAt(0);

            try
            {
                amount = int.Parse(msgSplit.Last());
                if (amount > 10)
                    amount = 10;
                msgSplit.Remove(msgSplit.Last());
            }
            catch { }

            if (msgSplit.Count > 0)
            {
                playerID = string.Join(" ", msgSplit);
            }
            else
            {
                playerID = Database.GetPlayerOsuID(message.From.Username).ToString();
                if (playerID == "0")
                    return "ты кто";
            }

            List<Score> lastScores = await OsuAPI.GetRecentScoresAsync(playerID, amount);
            if (lastScores != null)
            {
                if (lastScores.Count == 0)
                    return "Нет скоров";

                string result = string.Empty;
                foreach (Score score in lastScores)
                {
                    Mods enabledMods = score.EnabledMods;
                    string mods = string.Empty;
                    if (enabledMods > 0)
                        mods = " +" + enabledMods.ToString().Replace(", ", "");

                    TimeSpan ago = DateTime.Now.ToUniversalTime().AddHours(8) - score.Date; // osu is UTC+8
                    string date = ago.ToString(@"hh\:mm\:ss");

                    Map map = await OsuAPI.GetBeatmapAsync(score.BeatmapID);
                    if (map != null)
                    {
                        string mapInfo = $"{map.Artist} - {map.Title} [{map.Difficulty}]".FilterToHTML();
                        OppaiInfo oppaiInfo = Oppai.GetBeatmapInfo(map.File, mods, score.Accuracy, score.Combo, score.Misses);

                        result += $"<b>({score.Rank})</b> <a href=\"{map.Link}\">{mapInfo}</a><b>{mods} ({score.Accuracy.ToString("N2")}%)</b>{Environment.NewLine}" +
                                  $"{score.Combo}/{map.MaxCombo}x ({score.Count300}/ {score.Count100} / {score.Count50} / {score.Misses}) | ~{oppaiInfo.pp.ToString("N2")}pp{Environment.NewLine}" +
                                  $"{date} ago{Environment.NewLine}{Environment.NewLine}";
                    }
                    else
                    {
                        result += $"<b>({score.Rank})</b> https://osu.ppy.sh/b/{score.BeatmapID}<b>{mods} ({score.Accuracy.ToString("N2")}%)</b>{Environment.NewLine}" +
                                  $"{score.Combo}x ({score.Count300}/ {score.Count100} / {score.Count50} / {score.Misses}){Environment.NewLine}" +
                                  $"{date} ago{Environment.NewLine}{Environment.NewLine}";
                    }
                }
                return result;
            }
            return null;
        }
    }
}
