﻿// den0bot (c) StanR 2020 - MIT License

using System;
using Newtonsoft.Json;

namespace den0bot.Modules.Osu.Osu.Types.V1
{
	public class Player
	{
		[JsonProperty("user_id")]
		public uint ID;
		[JsonProperty("username")]
		public string Username;
		[JsonProperty("country")]
		public string Country;
		[JsonProperty("join_date")]
		public DateTime JoinDate;
		
		[JsonProperty("playcount")]
		public uint Playcount;
		[JsonProperty("total_seconds_played")]
		public uint PlaytimeSeconds;

		[JsonProperty("pp_raw")]
		public double Pp;
		[JsonProperty("pp_rank")]
		public uint Rank;
		[JsonProperty("pp_country_rank")]
		public uint CountryRank;
		[JsonProperty("accuracy")]
		public double Accuracy;

		[JsonProperty("level")]
		public double Level;
		[JsonProperty("ranked_score")]
		public ulong RankedScore;
		[JsonProperty("total_score")]
		public ulong TotalScore;

		[JsonProperty("count300")]
		public uint Count300;
		[JsonProperty("count100")]
		public uint Count100;
		[JsonProperty("count50")]
		public uint Count50;
		[JsonProperty("count_rank_ss")]
		public uint CountSS;
		[JsonProperty("count_rank_s")]
		public uint CountS;
		[JsonProperty("count_rank_a")]
		public uint CountA;
	}
}