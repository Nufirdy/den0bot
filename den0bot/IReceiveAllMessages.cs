﻿// den0bot (c) StanR 2017 - MIT License
namespace den0bot
{
    interface IReceiveAllMessages
    {
        void ReceiveMessage(Telegram.Bot.Types.Message message);
    }
}