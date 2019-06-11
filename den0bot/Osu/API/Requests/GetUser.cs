﻿using System;
using System.Collections.Generic;
using den0bot.Osu.Types;

namespace den0bot.Osu.API.Requests
{
	class GetUser : IRequest
	{
		public APIVersion API => APIVersion.V1;

		public string Address => $"get_user?u={Username}";

		public Type ReturnType => typeof(List<Player>);

		public bool ShouldReturnSingle => true;

		public string Username { get; set; }
	}
}