using System;
using System.Collections.Generic;
using System.Text;

namespace notpiracybot.Entities
{
    public class ReactableRoleMessage
    {
        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong MessageId { get; set; }
    }
}