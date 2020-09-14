using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Help;
using Disqord.Extensions.Interactivity.Menus;
using Qmmands;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CachingMechanism;
using Disqord.Rest;
using notpiracybot.Entities;

namespace notpiracybot.Modules
{
    [HelpMetadata("🥦", "#39acdb")]
    public class RoleAssigner : DiscordModuleBase
    {
        private readonly EntityCache _cache;

        public RoleAssigner(CachingMechanism.EntityCache cache)
        {
            _cache = cache;
        }

        public async Task AddRoleAsync(IRole role, IEmoji emojiDefinition)
        {
            if (role.Position > Context.Guild.CurrentMember.Hierarchy || role.Position > ((CachedMember)Context.User).Hierarchy)
            {
                await ReplyAsync("This role is TOO POWERFUL you fool.");
                return;
            }

            if (!Context.Guild.CurrentMember.Permissions.ManageRoles)
            {
                await ReplyAsync("I can't give roles in this server. PLS GIV ME ADMIN");
                return;
            }

            AssignableRole newRole;
            if (emojiDefinition is LocalCustomEmoji lc)
            {
                newRole = new AssignableRole
                {
                    GuildId = Context.Guild.Id,
                    RoleId = role.Id,
                    EmojiName = emojiDefinition.Name,
                    Animated = emojiDefinition.MessageFormat.StartsWith("<a"),
                    EmojiId = ulong.Parse(emojiDefinition.ReactionFormat.Split(":")[1])
                };
            }
            else if (emojiDefinition is LocalEmoji le)
            {
                newRole = new AssignableRole
                {
                    GuildId = Context.Guild.Id,
                    RoleId = role.Id,
                    EmojiName = emojiDefinition.Name,
                    Animated = false,
                    EmojiId = null
                };
            }
            else
            {
                return;
            }

            await using (var db = new DataContext())
            {
                var roleMatch = db.Roles.FirstOrDefault(x => x.GuildId == newRole.GuildId && x.RoleId == newRole.RoleId);
                if (roleMatch != null)
                {
                    await ReplyAsync("That role can already be given to users you buffoon.");
                    return;
                }
                var emojiRoleMatch = db.Roles.FirstOrDefault(x => x.GuildId == newRole.GuildId && x.EmojiName == newRole.EmojiName);
                if (emojiRoleMatch != null)
                {
                    await ReplyAsync("That emoji is already associated with a role, imbecile.");
                    return;
                }

                await db.Roles.AddAsync(newRole);
                _cache.Put(newRole, TimeSpan.FromHours(1));

                if (newRole.IsCustomEmoji())
                {
                    _cache.RemoveIgnore<AssignableRole>(newRole.GuildId, newRole.EmojiId);
                }
                else
                {
                    _cache.RemoveIgnore<AssignableRole>(newRole.GuildId, newRole.EmojiName);
                }

                await db.SaveChangesAsync();
                await ReplyAsync(
                    $"Your citizens may now be granted this role, use `{Context.Prefix}getRole {role.Name}` to be granted it.");
            }
        }

        [Command("GenerateCard")]
        [RequireMemberGuildPermissions(Permission.ManageRoles)]
        public async Task GenerateCardAsync()
        {
            await using (var db = new DataContext())
            {
                var roles = db.Roles.Where(x => x.GuildId == Context.Guild.Id.RawValue);

                StringBuilder roleBuilder = new StringBuilder();
                foreach (var roleDef in roles)
                {
                    if (Context.Guild.Roles.TryGetValue(roleDef.RoleId, out var role))
                    {
                        if (roleDef.IsCustomEmoji())
                        {
                            roleBuilder.AppendLine($"<{roleDef.GetEmojiString()}> {role.Mention}");
                        }
                        else
                        {
                            roleBuilder.AppendLine($"{roleDef.GetEmojiString()} {role.Mention}");
                        }
                    }
                }

                if (roleBuilder.Length == 0)
                {
                    await ReplyAsync("There are no roles... it's all a lie");
                    return;
                }

                var message = await ReplyAsync("", false,
                    new LocalEmbedBuilder().WithDescription(roleBuilder.ToString()).Build());

                foreach (var role in roles)
                {
                    IEmoji emoji;
                    if (role.IsCustomEmoji())
                    {
                        Debug.Assert(role.EmojiId != null, "role.EmojiId != null");
                        emoji = new LocalCustomEmoji(role.EmojiId.Value, role.EmojiName, role.Animated);
                    }
                    else
                    {
                        emoji = new LocalEmoji(role.EmojiName);
                    }

                    await message.AddReactionAsync(emoji);
                }

                await db.RoleMessages.AddAsync(new ReactableRoleMessage
                {
                    ChannelId = message.ChannelId.RawValue,
                    GuildId = Context.Guild.Id.RawValue,
                    MessageId = message.Id.RawValue
                });

                await db.SaveChangesAsync();
            }
        }

        [Command("PurgeRoles")]
        public async Task PurgeRolesAsync()
        {
            await using (var db = new DataContext())
            {
                var roles = db.Roles.Where(x => x.GuildId == Context.Guild.Id.RawValue);

                StringBuilder roleBuilder = new StringBuilder();
                foreach (var roleDef in roles)
                {
                    if (Context.Guild.Roles.TryGetValue(roleDef.RoleId, out var role) == false)
                    {
                        db.Roles.Remove(roleDef);
                        _cache.Remove<AssignableRole>(x => x.RoleId == roleDef.RoleId);

                        roleBuilder.AppendLine(
                            $"Removed Role with ID: {roleDef.RoleId} and emoji {roleDef.GetEmojiString()}");
                    }
                }

                await db.SaveChangesAsync();

                await ReplyAsync("", false, new LocalEmbedBuilder().WithDescription(roleBuilder.ToString()).Build());
            }
        }

        [Command("Roles")]
        public async Task ShowRolesAsync()
        {
            await using (var db = new DataContext())
            {
                var roles = db.Roles.Where(x => x.GuildId == Context.Guild.Id.RawValue);

                StringBuilder roleBuilder = new StringBuilder();
                foreach (var roleDef in roles)
                {
                    if (Context.Guild.Roles.TryGetValue(roleDef.RoleId, out var role))
                    {
                        if (roleDef.IsCustomEmoji())
                        {
                            roleBuilder.AppendLine($"<{roleDef.GetEmojiString()}> {role.Mention}");
                        }
                        else
                        {
                            roleBuilder.AppendLine($"{roleDef.GetEmojiString()} {role.Mention}");
                        }
                    }
                }

                if (roleBuilder.Length == 0)
                {
                    await ReplyAsync("There are no roles... it's all a lie");
                    return;
                }

                await ReplyAsync("", false, new LocalEmbedBuilder().WithDescription(roleBuilder.ToString()).Build());
            }
        }

        [Command("DeleteRole")]
        [RequireMemberGuildPermissions(Permission.ManageRoles)]
        public async Task DeleteRoleAsync([Remainder]string roleName)
        {
            var dRoleMatch = Context.Guild.Roles.FirstOrDefault(x => x.Value.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (dRoleMatch.Value == null)
            {
                await ReplyAsync("This role doesn't exist you fucking moron. SMH");
                return;
            }

            if (!Context.Guild.CurrentMember.Permissions.ManageRoles)
            {
                await ReplyAsync("I can't manage roles in this server. PLS GIV ME ADMIN");
                return;
            }

            await using (var db = new DataContext())
            {
                var roleMatch = db.Roles.FirstOrDefault(x => x.GuildId == dRoleMatch.Value.Guild.Id.RawValue && x.RoleId == dRoleMatch.Value.Id.RawValue);
                if (roleMatch == null)
                {
                    await ReplyAsync("Why would you want me to delete this role, you haven't even made it available to users yet :/");
                    return;
                }

                db.Roles.Remove(roleMatch);
                _cache.Remove<AssignableRole>(x => x.RoleId == roleMatch.RoleId);

                await db.SaveChangesAsync();

                await ReplyAsync(
                    "This role has been removed from the database but not from the server or any members, do it yourself you lazy POS");
            }
        }

        [Command("AddRole")]
        [RequireMemberGuildPermissions(Permission.ManageRoles)]
        public async Task CreateRoleAsync(IEmoji emoji, [Remainder]string roleName)
        {
            var dRoleMatch = Context.Guild.Roles.FirstOrDefault(x => x.Value.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (dRoleMatch.Value != null)
            {
                await AddRoleAsync(dRoleMatch.Value, emoji);
                return;
            }

            if (roleName.Contains("<@&"))
            {
                ulong roleId = ulong.Parse(roleName.Replace("<@&", "").Replace(">", "").Trim());
                var dRolePingMatch = Context.Guild.Roles.FirstOrDefault(x =>
                    x.Value.Id == roleId);

                if (dRolePingMatch.Value != null)
                {
                    await AddRoleAsync(dRolePingMatch.Value, emoji);
                    return;
                }
            }

            if (!Context.Guild.CurrentMember.Permissions.ManageRoles)
            {
                await ReplyAsync("I can't create roles in this server. PLS GIV ME ADMIN");
                return;
            }

            var dNewRole = await Context.Guild.CreateRoleAsync(x =>
            {
                x.Name = roleName;
                x.IsMentionable = true;
                x.IsHoisted = false;
            });

            await AddRoleAsync(dNewRole, emoji);
        }

        [Command("LeaveRole")]
        public async Task LeaveRoleAsync(CachedRole role)
        {
            await using (var db = new DataContext())
            {
                AssignableRole roleMatch = _cache.Retrieve<AssignableRole>(x =>
                    x.GuildId == Context.Guild.Id.RawValue && x.RoleId == role.Id.RawValue)
                    ?? db.Roles.FirstOrDefault(x => x.GuildId == Context.Guild.Id.RawValue && x.RoleId == role.Id.RawValue);

                if (roleMatch == null)
                {
                    await ReplyAsync("I cannot remove this role, as it does not exist <a:slinky:743336270972846091>");
                    return;
                }
                else
                {
                    _cache.Put(roleMatch, TimeSpan.FromHours(1));
                }
            }

            if (!Context.Guild.CurrentMember.Permissions.ManageRoles)
            {
                await ReplyAsync("I can't manage roles in this server. PLS GIV ADMIN");
                return;
            }

            await Context.Guild.RevokeRoleAsync(Context.User.Id, role.Id);
            await ReplyAsync("You hath been abolished from this role, be free my child.");
        }

        [Command("GetRole")]
        public async Task CreateRoleAsync(CachedRole role)
        {
            await using (var db = new DataContext())
            {
                AssignableRole roleMatch = _cache.Retrieve<AssignableRole>(x =>
                                               x.GuildId == Context.Guild.Id.RawValue && x.RoleId == role.Id.RawValue)
                                           ?? db.Roles.FirstOrDefault(x => x.GuildId == Context.Guild.Id.RawValue && x.RoleId == role.Id.RawValue);
                if (roleMatch == null)
                {
                    await ReplyAsync("I cannot give you this role, mere mortal.");
                    return;
                }
                else
                {
                    _cache.Put(roleMatch, TimeSpan.FromHours(1));
                }
            }

            if (!Context.Guild.CurrentMember.Permissions.ManageRoles)
            {
                await ReplyAsync("I can't give roles in this server. PLS GIV ADMIN");
                return;
            }

            await Context.Guild.GrantRoleAsync(Context.User.Id, role.Id);
            await ReplyAsync("You hath been granted this role, continue forth youngling.");
        }
    }
}