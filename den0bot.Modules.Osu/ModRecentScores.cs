﻿// den0bot (c) StanR 2019 - MIT License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using den0bot.DB;
using den0bot.Modules.Osu.Osu;
using den0bot.Modules.Osu.Osu.API.Requests;
using den0bot.Modules.Osu.Osu.Types;
using den0bot.Util;
using SQLite;
using Telegram.Bot.Types.Enums;

namespace den0bot.Modules.Osu
{
	public class ModRecentScores : IModule
	{
		private readonly Regex profileRegex = new Regex(@"(?>https?:\/\/)?(?>osu|old)\.ppy\.sh\/u(?>sers)?\/(\d+|\S+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private const int score_amount = 5;

		public ModRecentScores()
		{
			Database.CreateTable<Player>();

			AddCommands(new [] 
			{
				new Command
				{
					Name = "addme",
					Action = AddMe
				},
				new Command
				{
					Name = "removeme",
					Action = RemoveMe
				},
				new Command
				{
					Name = "removeplayer",
					IsOwnerOnly = true,
					Action = RemovePlayer
				},
				new Command
				{
					Name = "last",
					Reply = true,
					ActionAsync = GetScores,
					ParseMode = ParseMode.Html
				}
			});
			Log.Debug("Enabled");
		}

		private async Task<string> GetScores(Telegram.Bot.Types.Message message)
		{
			string playerID = string.Empty;
			int amount = 1;

			List<string> msgSplit = message.Text.Split(' ').ToList();
			msgSplit.RemoveAt(0);

			if (msgSplit.Count > 0)
			{
				if (int.TryParse(msgSplit.Last(), out amount))
				{
					if (amount > score_amount)
						amount = score_amount;
				}
				msgSplit.Remove(msgSplit.Last());
			}

			if (msgSplit.Count > 0)
			{
				playerID = string.Join(" ", msgSplit);
			}
			else
			{
				var id = GetPlayerOsuIDFromDatabase(message.From.Id);
				if (id == 0)
					return Localization.Get("recentscores_unknown_player", message.Chat.Id);

				playerID = id.ToString();
			}

			List<Score> lastScores = await Osu.WebApi.MakeAPIRequest(new GetRecentScores() {
				Username = playerID,
				Amount = amount
			});

			if (lastScores != null)
			{
				if (lastScores.Count == 0)
					return Localization.Get("recentscores_no_scores", message.Chat.Id);

				string result = string.Empty;
				foreach (Score score in lastScores)
				{
					Mods enabledMods = score.EnabledMods ?? Mods.None;
					string mods = string.Empty;
					if (enabledMods > 0)
						mods = " +" + enabledMods.ToString().Replace(", ", "");

					TimeSpan ago = DateTime.Now.ToUniversalTime() - score.Date;
					string date = ago.ToString(@"hh\:mm\:ss") + " ago";

					Map map = await Osu.WebApi.MakeAPIRequest(new GetBeatmap
					{
						ID = score.BeatmapID

					});
					if (map != null)
					{
						string mapInfo = $"{map.Artist} - {map.Title} [{map.Difficulty}]".FilterToHTML();

						result += $"<b>({score.Rank})</b> <a href=\"{map.Link}\">{mapInfo}</a><b>{mods} ({score.Accuracy:N2}%)</b>{Environment.NewLine}" +
								  $"{score.Combo}/{map.MaxCombo}x ({score.Count300}/ {score.Count100} / {score.Count50} / {score.Misses})";
						try
						{
							// Add pp values
							double scorePP = Oppai.GetBeatmapOppaiInfo(map, score).PP;
							string possiblePP = string.Empty;
							if (score.Combo < map.MaxCombo - 1 || score.Misses > 0)
							{
								// Add possible pp value if they missed or dropped more than 1 combo
								Score fcScore = (Score) score.Clone();
								fcScore.Combo = map.MaxCombo ?? 0;
								fcScore.Misses = 0;
								double possiblePPval = Oppai.GetBeatmapOppaiInfo(map, fcScore).PP;
								possiblePP = $"({possiblePPval:N2}pp if FC)";
							}

							result += $" | ~{scorePP:N2}pp {possiblePP}";
						}
						catch (Exception e)
						{
							Log.Error($"Oppai failed: {e.InnerMessageIfAny()}");
						}
					}
					else
					{
						// Didn't get beatmap info, insert plain link
						result += $"<b>({score.Rank})</b> https://osu.ppy.sh/b/{score.BeatmapID}<b>{mods} ({score.Accuracy:N2}%)</b>{Environment.NewLine}" +
								  $"{score.Combo}x ({score.Count300}/ {score.Count100} / {score.Count50} / {score.Misses})";
					}

					// Add date
					result += $"{Environment.NewLine}{date}{Environment.NewLine}{Environment.NewLine}";
				}

				return result;
			}
			return null;
		}

		private string AddMe(Telegram.Bot.Types.Message message)
		{
			Match regexMatch = profileRegex.Match(message.Text);
			if (regexMatch.Groups.Count > 1)
			{
				string player = regexMatch.Groups[1]?.Value;
				if (!string.IsNullOrEmpty(player))
				{
					uint osuID = 0;
					if (!uint.TryParse(player, out osuID))
					{
						// if they used /u/cookiezi instead of /u/124493 we ask osu API for an ID
						Osu.Types.Player info = Osu.WebApi.MakeAPIRequest(new GetUser
						{
							Username = player

						}).Result;

						if (info == null)
							return "Ты че деб? /addme <ссылка на профиль>";
						else
							osuID = info.ID;
					}
					if (osuID != 0)
					{
						AddPlayerToDatabase(message.From.Id, osuID);
						return "Добавил!";
					}
				}
			}
			
			return "Ты че деб? /addme <ссылка на профиль>";
		}

		private string RemoveMe(Telegram.Bot.Types.Message message)
		{
			RemovePlayerFromDatabase(message.From.Id);
			return "Удалил.";
		}

		private string RemovePlayer(Telegram.Bot.Types.Message message)
		{
			string name = message.Text.Substring(14);

			if (!string.IsNullOrEmpty(name))
			{
				RemovePlayerFromDatabase(Database.GetUserID(name));
				return $"{name} удален.";
			}
			return "Ты че деб? /removeplayer <юзернейм>";
		}

		#region Database
		private class Player
		{
			[PrimaryKey]
			public int TelegramID { get; set; }
			public uint OsuID { get; set; }
		}

		private Player GetPlayerFromDatabase(int ID) => Database.GetFirst<Player>(x => x.TelegramID == ID);

		private uint GetPlayerOsuIDFromDatabase(int ID) => GetPlayerFromDatabase(ID)?.OsuID ?? 0;

		private bool AddPlayerToDatabase(int tgID, uint osuID)
		{
			if (!Database.Exist<Player>(x => x.TelegramID == tgID))
			{
				Database.Insert(new Player
				{
					TelegramID = tgID,
					OsuID = osuID,
				});
				return true;
			}
			return false;
		}
		private void RemovePlayerFromDatabase(int tgID)
		{
			Database.Remove<Player>(x => x.TelegramID == tgID);
		}

		#endregion
	}
}