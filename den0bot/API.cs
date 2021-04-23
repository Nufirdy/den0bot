﻿// den0bot (c) StanR 2021 - MIT License
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using den0bot.DB;
using den0bot.Util;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace den0bot
{
	public static class API
	{
		public static User BotUser { get; private set; }
		private static TelegramBotClient api;

		public static bool IsConnected => api.IsReceiving;

		/// <summary>
		/// Connect and start receiving messages. Returns false if failed to connect.
		/// </summary>
		public static bool Connect()
		{
			if (string.IsNullOrEmpty(Config.Params.TelegamToken))
			{
				Log.Error("Telegram token is null or empty!");
				return false;
			}

			Log.Info("Connecting...");
			api = new TelegramBotClient(Config.Params.TelegamToken);
			api.OnMessage += OnMessage;
			api.OnMessageEdited += OnMessageEdit;
			api.OnCallbackQuery += OnCallback;
			api.OnReceiveGeneralError += delegate (object _, ReceiveGeneralErrorEventArgs args) { Log.Error(args.Exception.InnerMessageIfAny()); };
			api.OnReceiveError += delegate (object _, ReceiveErrorEventArgs args) { Log.Error(args.ApiRequestException.InnerMessageIfAny()); };

			if (!api.TestApiAsync().Result)
			{
				Log.Error("API Test failed, shutting down!");
				return false;
			}
			BotUser = api.GetMeAsync().Result;

			Log.Info("API Test successful, starting receiving...");
			api.StartReceiving();

			Log.Info("Connected!");
			return true;
		}

		public static void Disconnect()
		{
			Log.Info("Disconnecting...");
			api.StopReceiving();
		}

		public static event EventHandler<MessageEventArgs> OnMessage;
		public static event EventHandler<MessageEventArgs> OnMessageEdit;
		public static event EventHandler<CallbackQueryEventArgs> OnCallback;

		/// <summary>
		/// Send message
		/// </summary>
		/// <param name="message">Text to send</param>
		/// <param name="receiverID">Chat ID to send message to</param>
		/// <param name="mode">ParseMode to use (None/Markdown/HTML)</param>
		/// <param name="replyID">Message ID to reply to</param>
		public static async Task<Message> SendMessage(string message, long receiverID, ParseMode mode = ParseMode.Default, int replyID = 0, IReplyMarkup replyMarkup = null, bool disablePreview = true)
		{
			try
			{
				if (!string.IsNullOrEmpty(message))
					return await api.SendTextMessageAsync(receiverID, message, mode, disablePreview, false, replyID, replyMarkup);
			}
			catch (Exception ex)
			{
				Log.Error(ex.InnerMessageIfAny());
			}
			return null;
		}

		/// <summary>
		/// Send message to all chats in the database
		/// </summary>
		/// <param name="msg">Text to send</param>
		/// <param name="image">Image to send if any</param>
		/// <param name="mode">ParseMode to use (None/Markdown/HTML)</param>
		public static async Task SendMessageToAllChats(string msg, string image = null, ParseMode mode = ParseMode.Default)
		{
			foreach (var receiver in DatabaseCache.Chats)
            {
                var chat = await api.GetChatAsync(receiver.Id);
                if (chat == null)
				{
					await DatabaseCache.RemoveChat(receiver.Id);
					Log.Info($"Chat {receiver.Id} removed");
					return;
				}
				else if (!receiver.DisableAnnouncements ?? false)
				{
					if (!string.IsNullOrEmpty(image))
						await SendPhoto(image, receiver.Id, msg, mode);
					else
						await SendMessage(msg, receiver.Id, mode);
				}
			}
		}

		/// <summary>
		/// Send photo
		/// </summary>
		/// <param name="photo">Photo to send. Can be both internal telegram photo ID or a link</param>
		/// <param name="receiverId">Chat ID to send photo to</param>
		/// <param name="message">Photo caption if any</param>
		/// <param name="mode">ParseMode</param>
		/// <param name="replyID">Message to reply to</param>
		/// <param name="replyMarkup"></param>
		public static async Task<Message> SendPhoto(string photo, long receiverId, string message = "", ParseMode mode = ParseMode.Default, int replyID = 0, IReplyMarkup replyMarkup = null, bool sendTextIfFailed = true)
		{
			try
			{
				if (!string.IsNullOrEmpty(photo))
				{
					return await api.SendPhotoAsync(receiverId, new InputOnlineFile(photo), message, mode, false,
						replyID, replyMarkup);
				}
			}
			catch (ApiRequestException ex)
			{
				Log.Error(ex.InnerMessageIfAny());
				if (sendTextIfFailed)
					return await api.SendTextMessageAsync(receiverId, message, mode, false, false, replyID, replyMarkup);
			}
			catch (Exception ex)
			{
				Log.Error(ex.InnerMessageIfAny());
			}
			return null;
		}

		/// <summary>
		/// Send multiple photos in an album
		/// </summary>
		/// <param name="photos">Array of InputMediaPhoto photos</param>
		/// <param name="receiverId">Chat to send photos to</param>
		public static async Task<Message[]> SendMultiplePhotos(List<InputMediaPhoto> photos, long receiverId)
		{
			try
			{
				if (photos != null && photos.Count > 1)
				{
					return await api.SendMediaGroupAsync(photos, receiverId);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.InnerMessageIfAny());
			}
			return null;
		}

		/// <summary>
		/// Send sticker
		/// </summary>
		/// <param name="sticker">Telegram sticker ID</param>
		/// <param name="receiverID">Chat to send sticker to</param>
		public static async Task<Message> SendSticker(InputOnlineFile sticker, long receiverID)
		{
			try
			{
				return await api.SendStickerAsync(receiverID, sticker);
			}
			catch (Exception ex)
			{
				Log.Error(ex.InnerMessageIfAny());
			}
			return null;
		}

		/// <summary>
		/// Returns array of chat memebers that are admins
		/// </summary>
		public static async Task<ChatMember[]> GetAdmins(long chatID)
		{
			try
			{
				return await api.GetChatAdministratorsAsync(chatID);
			}
			catch (Exception ex)
			{
				Log.Error(ex.InnerMessageIfAny());
			}
			return null;
		}

		/// <summary>
		/// Remove message from a chat if bot have enough rights
		/// </summary>
		/// <param name="chatID">Chat ID to remove message from</param>
		/// <param name="msgID">Message to remove</param>
		public static async Task RemoveMessage(long chatID, int msgID)
		{
			try
			{
				await api.DeleteMessageAsync(chatID, msgID);
			}
			catch (Exception ex)
			{
				Log.Error(ex.InnerMessageIfAny());
			}
		}

		/// <summary>
		/// Remove media caption
		/// </summary>
		/// <param name="chatID">Chat ID to edit message in</param>
		/// <param name="msgID">Message to edit</param>
		/// <param name="caption">New caption</param>
		public static async Task<Message> EditMediaCaption(long chatID, int msgID, string caption, InlineKeyboardMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Default)
		{
			try
			{
				return await api.EditMessageCaptionAsync(chatID, msgID, caption, replyMarkup, parseMode: parseMode);
			}
			catch (Exception ex)
			{
				Log.Error(ex.InnerMessageIfAny());
			}
			return null;
		}

		/// <summary>
		/// Answer callback query after receiving it
		/// </summary>
		/// <param name="callbackID">Callback ID that we need to answer</param>
		/// <param name="text">Text to send</param>
		/// <param name="showAlert">Alert user</param>
		public static async Task AnswerCallbackQuery(string callbackID, string text = null, bool showAlert = false)
		{
			try
			{
				await api.AnswerCallbackQueryAsync(callbackID, text, showAlert);
			}
			catch (Exception ex)
			{
				Log.Error(ex.InnerMessageIfAny());
			}
		}

		/// <summary>
		/// Send voice message (or just audio file if it's not vorbis ogg)
		/// </summary>
		/// <param name="audio">Audio to send</param>
		/// <param name="chatID">Chat ID to send photo to</param>
		public static async Task<Message> SendVoice(InputOnlineFile audio, long chatID, string caption = null, ParseMode parseMode = ParseMode.Default, int replyTo = 0, int duration = 0)
		{
			try
			{
				return await api.SendVoiceAsync(chatID, audio, caption, parseMode, duration, replyToMessageId: replyTo);
			}
			catch (Exception ex)
			{
				Log.Error(ex.InnerMessageIfAny());
			}
			return null;
		}

		/// <summary>
		/// Downloads file from the server using File ID
		/// </summary>
		/// <param name="fileId">File ID to download</param>
		/// <param name="path">Where to download</param>
		public static async Task<bool> DownloadFile(string fileId, string path)
		{
			if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(path))
				return false;

			await using var stream = new MemoryStream();

			var file = await api.GetFileAsync(fileId);
			await api.DownloadFileAsync(file.FilePath, stream);
			stream.Position = 0;

			Memory<byte> buf = new Memory<byte>();
			await stream.ReadAsync(buf);

			await System.IO.File.WriteAllBytesAsync(path, buf.ToArray());

			return true;
		}
	}
}