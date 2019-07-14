﻿// den0bot (c) StanR 2019 - MIT License

using System.Threading.Tasks;

namespace den0bot.Modules
{
	public interface IReceiveCallback
	{
		Task<string> ReceiveCallback(Telegram.Bot.Types.CallbackQuery callback);
	}
}
