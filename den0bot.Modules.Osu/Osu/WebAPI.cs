﻿// den0bot (c) StanR 2019 - MIT License
using System;
using System.Threading.Tasks;
using den0bot.Util;
using den0bot.Modules.Osu.Osu.API;
using Newtonsoft.Json;

namespace den0bot.Modules.Osu.Osu
{
	public static class WebApi
	{
		public static async Task<dynamic> MakeAPIRequest(IRequest request)
		{
			return request.API switch
			{
				APIVersion.V1 => await V1APIRequest(request),
				_ => throw new NotImplementedException(),
			};
		}

		private static async Task<dynamic> V1APIRequest(IRequest request)
		{
			if (string.IsNullOrEmpty(Config.Params.osuToken))
			{
				Log.Error("API Key is not defined!");
				return null;
			}

			try
			{
				string json =
					await Web.DownloadString($"https://osu.ppy.sh/api/{request.Address}&k={Config.Params.osuToken}");

				dynamic deserializedObject = JsonConvert.DeserializeObject(json, request.ReturnType);
				if (request.ShouldReturnSingle)
					return deserializedObject[0];

				return deserializedObject;
			}
			catch (Exception ex)
			{
				Log.Error(ex.InnerMessageIfAny());
				return null;
			}
		}
	}
}
