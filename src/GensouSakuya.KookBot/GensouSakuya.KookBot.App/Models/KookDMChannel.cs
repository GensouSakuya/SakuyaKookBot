﻿namespace GensouSakuya.KookBot.App.Models
{
    internal class KookDMChannel: BaseKookChannel
    {
        public Guid ChatCode { get; set; }

        public KookDMChannel(Guid chatCode)
        {
            ChatCode = chatCode;
        }
    }
}
