﻿// den0bot (c) StanR 2017 - MIT License
using System;
using Newtonsoft.Json;

namespace den0bot.Osu
{
    public class Score : ICloneable
    {
        [JsonProperty("beatmap_id")]
        public uint BeatmapID;
        [JsonProperty("score_id")]
        public uint ScoreID;
        [JsonProperty("user_id")]
        public uint UserID;

        [JsonProperty("date")]
        public DateTime Date;

        [JsonProperty("maxcombo")]
        public uint Combo;

        [JsonProperty("perfect")]
        public short Perfect;

		[JsonProperty("score")]
		public uint ScorePoints;
		[JsonProperty("count300")]
        public uint Count300;
        [JsonProperty("count100")]
        public uint Count100;
        [JsonProperty("count50")]
        public uint Count50;
        [JsonProperty("countmiss")]
        public uint Misses;
        [JsonProperty("countkatu")]
        public uint CountKatu;
        [JsonProperty("countgeki")]
        public uint CountGeki;

        [JsonProperty("enabled_mods")]
        public Mods EnabledMods;
        [JsonProperty("rank")]
        public string Rank;
        [JsonProperty("pp")]
        public double Pp;

        // multiplayer
        [JsonProperty("slot")]
        public uint Slot;
        [JsonProperty("team")]
        public uint Team; // FIXME: make it into enum
        [JsonProperty("pass")]
        public short IsPass;

        public double Accuracy
        {
            get
            {
                if (accuracy <= 0)
                {
                    /*
                     * Accuracy = Total points of hits / (Total number of hits * 300)
                     * Total points of hits  =  Number of 50s * 50 + Number of 100s * 100 + Number of 300s * 300
                     * Total number of hits  =  Number of misses + Number of 50's + Number of 100's + Number of 300's
                     */

                    double totalPoints = Count50 * 50 + Count100 * 100 + Count300 * 300;
                    double totalHits = Misses + Count50 + Count100 + Count300;

                    accuracy = totalPoints / (totalHits * 300) * 100;
                    return accuracy;
                }
                else
                {
                    return accuracy;
                }
            }
            set
            {
                accuracy = value;
            }
        }
        private double accuracy = 0.0;

        public object Clone()
        {
            return MemberwiseClone();
        }
        public override bool Equals(object obj)
        {
            Score b = obj as Score;
            if (ReferenceEquals(this, b))
                return true;

            if (b == null)
                return false;

            return ScoreID == b.ScoreID && Date == b.Date;
        }
        public override int GetHashCode()
        {
            return ScoreID.GetHashCode() ^ Date.GetHashCode();
        }
        public static bool operator ==(Score a, Score b)
        {
            return Equals(a, b);
        }
        public static bool operator !=(Score a, Score b)
        {
            return !Equals(a, b);
        }
    }
}
