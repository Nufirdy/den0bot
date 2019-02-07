﻿// den0bot (c) StanR 2018 - MIT License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using den0bot.Osu;
using den0bot.Osu.Types;
using den0bot.Util;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace den0bot.Modules
{
	class ModBeatmap : IModule, IReceiveAllMessages, IReceiveCallback
	{
		private Regex regex = new Regex(@"(?>https?:\/\/)?(?>osu|old)\.ppy\.sh\/([b,s]|(?>beatmapsets))\/(\d+\/?\#osu\/)?(\d+)?\/?(?>[&,?].=\d)?\s?(?>\+(\w+))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private readonly MemoryCache sentMapsCache = MemoryCache.Default;
		private const int days_to_keep_messages = 1; // how long do we keep maps in cache

		private readonly InlineKeyboardMarkup buttons = new InlineKeyboardMarkup(
			new[] { new InlineKeyboardButton {Text = "Preview", CallbackData = "preview" },}
		);

		public async void ReceiveMessage(Message message)
		{
			Match regexMatch = regex.Match(message.Text);
			if (regexMatch.Groups.Count > 1)
			{
				List<Group> regexGroups = regexMatch.Groups.OfType<Group>().Where(x => (x != null) && (x.Length > 0)).ToList();

				bool isNew = regexGroups[1].Value == "beatmapsets"; // are we using new website or not
				bool isSet = false;
				uint beatmapId = 0;
				string mods = string.Empty;

				if (isNew)
				{
					if (regexGroups[2].Value.Contains("#osu/"))
					{
						beatmapId = uint.Parse(regexGroups[3].Value);
						if (regexGroups.Count > 4)
							mods = regexGroups[4].Value;
					}
					else
					{
						isSet = true;
					}
				}
				else
				{ 
					if(regexGroups[1].Value == "s")
						isSet = true;

					beatmapId = uint.Parse(regexGroups[2].Value);
					if (regexGroups.Count > 3)
						mods = regexGroups[3].Value;
				}

				Map map = null;
				if (isSet)
				{
					List<Map> set = await OsuAPI.GetBeatmapSetAsync(beatmapId);
					if (set?.Count > 0)
						map = set?.Last();
				}
				else
				{
					map = await OsuAPI.GetBeatmapAsync(beatmapId);
				}

				if (map != null)
				{
					var sentMessage = await API.SendPhoto(map.Thumbnail, message.Chat.Id, FormatMapInfo(map, mods, message.Chat.Id), Telegram.Bot.Types.Enums.ParseMode.Html, 0, buttons);
					if (sentMessage != null)
					{
						sentMapsCache.Add(sentMessage.MessageId.ToString(), map, DateTimeOffset.Now.AddDays(days_to_keep_messages));
					}
				}
			}
		}

		public static string FormatMapInfo(Map map, string mods, long chatID)
		{
			double starRating = map.StarRating;
			string pp = string.Empty;

			try
			{
				Mods modsEnum = mods.ConvertToMods();

				OppaiInfo info100 = Oppai.GetBeatmapOppaiInfo(map.FileBytes, modsEnum, 100);
				if (info100 != null && info100.PP > 0)
				{
					pp = $"\n100% - {info100.PP.FN2()}pp";
					starRating = info100.Stars;

					double info98 = Oppai.GetBeatmapPP(map.FileBytes, modsEnum, 98);
					if (info98 != -1)
						pp += $" | 98% - {info98.FN2()}pp";

					double info95 = Oppai.GetBeatmapPP(map.FileBytes, modsEnum, 95);
					if (info95 != -1)
						pp += $" | 95% - {info95.FN2()}pp";
				}
			}
			catch (Exception)
			{ }

			string result = string.Format("[{0}] - {1}* - {2}{3} - <b>{4}</b>\n<b>CS:</b> {5} | <b>AR:</b> {6} | <b>OD:</b> {7} | <b>BPM:</b> {8}",
				map.Difficulty.FilterToHTML(), starRating.FN2(), map.DrainLength(mods).ToString("mm':'ss"), $" - {map.Creator}", map.Status.ToString(),
				map.CS(mods).FN2(), map.AR(mods).FN2(), map.OD(mods).FN2(), map.BPM(mods).FN2());

			//result = result.FilterToHTML(); // remove any possible html stuff before adding our own
			result += pp;
			//result += $"\n[<a href=\"https://osu.ppy.sh/beatmapsets/{map.BeatmapSetID}/download\">{Localization.Get("beatmap_download", chatID)}</a>]";

			return result;
		}

		public void ReceiveCallback(CallbackQuery callback)
		{
			if (sentMapsCache.Contains(callback.Message.MessageId.ToString()) && callback.Data == "preview")
			{
				var map = sentMapsCache.Remove(callback.Message.MessageId.ToString()) as Map;
				API.SendVoice(new InputOnlineFile($"https://b.ppy.sh/preview/{map.BeatmapSetID}.mp3"), callback.Message.Chat.Id, replyTo: callback.Message.MessageId);
				API.EditMediaCaption(callback.Message.Chat.Id, callback.Message.MessageId, callback.Message.Caption, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
				API.AnswerCallbackQuery(callback.Id, "Ща всё будет");
			}
		}
	}
}
