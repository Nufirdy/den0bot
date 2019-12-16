﻿// den0bot (c) StanR 2018 - MIT License
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using den0bot.Modules.Osu.Osu.API.Requests;
using den0bot.Modules.Osu.Osu.Types;
using den0bot.Util;
using FFmpeg.NET;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace den0bot.Modules.Osu
{
	public class ModBeatmap : IModule, IReceiveAllMessages, IReceiveCallback
	{
		private readonly MemoryCache sentMapsCache = MemoryCache.Default;
		private const int days_to_keep_messages = 1; // how long do we keep maps in cache

		private readonly InlineKeyboardMarkup buttons = new InlineKeyboardMarkup(
			new[] {new InlineKeyboardButton {Text = "Preview", CallbackData = "preview"},}
		);

		private class RebalanceMap
		{
			public int? BeatmapSetId { get; set; }
			public string Title { get; set; }
			public double Stars { get; set; }
			public double[] PP { get; set; }
		}

		public ModBeatmap()
		{
			AddCommand(new Command
			{
				Name = "newpp",
				ActionAsync = GetRebalancePp
			});
		}

		public async Task ReceiveMessage(Message message)
		{
			if (!string.IsNullOrEmpty(message.Text))
			{
				var beatmapId = Map.GetIdFromLink(message.Text, out var isSet, out var mods);
				if (beatmapId != 0)
				{
					Map map = null;
					if (isSet)
					{
						List<Map> set = await Osu.WebApi.MakeAPIRequest(new GetBeatmapSet(beatmapId));
						if (set?.Count > 0)
							map = set.Last();
					}
					else
					{
						map = await Osu.WebApi.MakeAPIRequest(new GetBeatmap(beatmapId,mods));
					}

					if (map != null)
					{
						var sentMessage = await API.SendPhoto(map.Thumbnail, message.Chat.Id,
							map.GetFormattedMapInfo(mods),
							Telegram.Bot.Types.Enums.ParseMode.Html, 0, buttons);
						if (sentMessage != null)
						{
							// we only store mapset id to spare the memory a bit
							sentMapsCache.Add(sentMessage.MessageId.ToString(), map.BeatmapSetID,
								DateTimeOffset.Now.AddDays(days_to_keep_messages));
						}
					}
				}
			}
		}

		public async Task<string> ReceiveCallback(CallbackQuery callback)
		{
			if (sentMapsCache.Contains(callback.Message.MessageId.ToString()) && callback.Data == "preview")
			{
				await API.AnswerCallbackQuery(callback.Id, "Ща всё будет");
				var mapsetId = sentMapsCache.Remove(callback.Message.MessageId.ToString()) as uint?;

				try
				{
					var data = await Web.DownloadBytes($"https://b.ppy.sh/preview/{mapsetId}.mp3");
					File.WriteAllBytes($"./{mapsetId}.mp3", data);
				}
				catch (Exception e)
				{
					Log.Error(e.InnerMessageIfAny());
					return string.Empty;
				}

				await new Engine("ffmpeg")
					.ConvertAsync(new MediaFile($"./{mapsetId}.mp3"), new MediaFile($"./{mapsetId}.ogg"));

				using (FileStream fs = File.Open($"./{mapsetId}.ogg", FileMode.Open, FileAccess.Read))
					await API.SendVoice(new InputOnlineFile(fs), callback.Message.Chat.Id, replyTo: callback.Message.MessageId, duration: 10);

				File.Delete($"./{mapsetId}.mp3");
				File.Delete($"./{mapsetId}.ogg");

				await API.EditMediaCaption(callback.Message.Chat.Id, callback.Message.MessageId,
					callback.Message.Caption, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
			}

			return string.Empty;
		}

		private async Task<string> GetRebalancePp(Message msg)
		{
			if (!string.IsNullOrEmpty(msg.Text))
			{
				var beatmapId = Map.GetIdFromLink(msg.Text, out var isSet, out var mods);
				if (beatmapId != 0 && !isSet)
				{
					var json = new { Map = beatmapId.ToString(), Mods = Array.Empty<string>() };
					var mapJson = await Web.PostJson("https://newpp.stanr.info/api/CalculateMap", JsonConvert.SerializeObject(json));
					if (!string.IsNullOrEmpty(mapJson))
					{
						var map = JsonConvert.DeserializeObject<RebalanceMap>(
							JsonConvert.DeserializeObject<string>(mapJson));

						await API.SendPhoto($"https://assets.ppy.sh/beatmaps/{map.BeatmapSetId}/covers/card@2x.jpg",
							msg.Chat.Id,
							$"{map.Title}\n{map.Stars:F2}*\n100% - {map.PP[10]}pp | 98% - {map.PP[8]}pp | 95% - {map.PP[5]}pp",
							replyID: msg.MessageId);
					}
				}

				return Localization.Get("generic_badrequest", msg.Chat.Id);
			}

			return string.Empty;
		}
	}
}
