using System;
using System.Collections.Generic;
using System.Text;

namespace notpiracybot
{
    public class AssignableRole
    {
        public ulong GuildId { get; set; }

        public ulong RoleId { get; set; }

        public ulong? EmojiId { get; set; }

        public string EmojiName { get; set; }

        public bool Animated { get; set; } = false;

        public bool IsCustomEmoji()
        {
            return EmojiId.HasValue;
        }

        public string GetEmojiString()
        {
            StringBuilder content = new StringBuilder();
            if (EmojiId != null)
            {
                if (Animated)
                {
                    content.Append("a");
                }

                content.Append(":");
                content.Append(EmojiName);
                content.Append(":");
                content.Append(EmojiId.Value);
            }
            else
            {
                content.Append(EmojiName);
            }

            return content.ToString();
        }
    }
}