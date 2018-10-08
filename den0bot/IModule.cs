﻿// den0bot (c) StanR 2017 - MIT License
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace den0bot
{
    abstract public class IModule
    {
        public class Command
        {
            private string name;
            public string Name
            {
                get
                {
                    return "/" + name;
                }
                set
                {
                    name = value;
                }
            }

            public ParseMode ParseMode;

            public bool IsAdminOnly;
			public bool IsOwnerOnly;
            public bool Reply;

            public Func<Message, Task<string>> ActionAsync;
            public Func<Message, string> Action;
            public Action<Message> ActionResult;
        }

        protected List<Command> Commands = new List<Command>();

        protected void AddCommand(Command command)
        {
            Commands.Add(command);
        }

        protected void AddCommands(ICollection<Command> coll)
        {
            Commands.AddRange(coll);
        }

        public Command GetCommand(string name)
        {
            if (Commands.Count <= 0)
                return null;

            int nameEndIndex = name.IndexOf(' ');
            if (nameEndIndex != -1)
                name = name.Remove(nameEndIndex, name.Length - nameEndIndex);

            if (name.EndsWith("@den0bot"))
                name = name.Replace("@den0bot", "");

            return Commands.Find(x => name == x.Name);
        }

        public virtual void Think() {}
    }
}
