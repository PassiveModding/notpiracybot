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
using Disqord.Rest;

namespace notpiracybot.Modules
{
    [HelpMetadata("📎", "#39acdb")]
    public class General : DiscordModuleBase
    {
        public General(CommandService cmdService)
        {
            CmdService = cmdService;
        }

        public CommandService CmdService { get; }

        [Command("Ping", "Latency")]
        [Description("Display bot latency.")]
        public async Task PingAsync()
        {
            // Latency is null until the first heartbeat sent by the bot.
            var latency = Context.Bot.Latency.HasValue ? "heartbeat: " + Context.Bot.Latency.Value.TotalMilliseconds + "ms, " : "";

            var s = Stopwatch.StartNew();
            var m = await ReplyAsync($"{latency}init: ---, rtt: ---");
            var init = s.ElapsedMilliseconds;
            await m.ModifyAsync(x => x.Content = $"{latency}init: {init}ms, rtt: Calculating");
            s.Stop();
            await m.ModifyAsync(x => x.Content = $"{latency}init: {init}ms, rtt: {s.ElapsedMilliseconds}ms");
        }

        [Command("invite")]
        [Description("Replies with an invite link for the bot.")]
        public async Task InviteAsync()
        {
            RestApplication restApp;
            if (!Context.Bot.CurrentApplication.IsFetched)
            {
                restApp = await Context.Bot.CurrentApplication.FetchAsync();
            }
            else
            {
                restApp = Context.Bot.CurrentApplication.Value;
            }

            if (restApp.IsBotPublic)
            {
                await ReplyAsync("", false, new LocalEmbedBuilder().WithColor(Color.BlueViolet)
                    .WithDescription($"https://discordapp.com/oauth2/authorize" +
                    $"?client_id={Context.Bot.CurrentUser.Id}" +
                    "&scope=bot&permissions=8").Build());
            }
            else
            {
                await ReplyAsync("", false, new LocalEmbedBuilder().WithColor(Color.BlueViolet)
                .WithDescription($"This bot is currently not available to the public").Build());
            }
        }

        [Command("Help")]
        [Description("Display command info")]
        public async Task HelpAsync()
        {
            await Context.Channel.StartMenuAsync(new HelpMenu(CmdService), TimeSpan.FromMinutes(5));
        }

        [Command("Stats", "Info")]
        [Description("Display bot info")]
        public async Task StatsAsync()
        {
            var embed = new LocalEmbedBuilder();

            embed.WithAuthor(
                x =>
                {
                    x.IconUrl = Context.Bot.CurrentUser.GetAvatarUrl();
                    x.Name = Context.Bot.CurrentUser.Name;
                });

            int bots = Context.Bot.Guilds
                .Sum(x => x.Value.Members.Count(z => z.Value?.IsBot == true));
            int humans = Context.Bot.Guilds
                .Sum(x => x.Value.Members.Count(z => z.Value?.IsBot == false));
            int presentUsers = Context.Bot.Guilds
                .Sum(x => x.Value.Members.Count(u => u.Value?.Presence?.Status != UserStatus.Offline));

            embed.AddField(
                "Members",
                $"Bot: {bots}\n" +
                $"Human: {humans}\n" +
                $"Present: {presentUsers}",
                true);

            int online = Context.Bot.Guilds
                .Sum(x => x.Value.Members.Count(z => z.Value?.Presence?.Status == UserStatus.Online));
            int afk = Context.Bot.Guilds
                .Sum(x => x.Value.Members.Count(z => z.Value?.Presence?.Status == UserStatus.Idle));
            int dnd = Context.Bot.Guilds
                .Sum(x => x.Value.Members.Count(z => z.Value?.Presence?.Status == UserStatus.DoNotDisturb));

            embed.AddField(
                "Members",
                $"Online: {online}\n" +
                $"AFK: {afk}\n" +
                $"DND: {dnd}",
                true);

            embed.AddField(
                "Channels",
                $"Text: {Context.Bot.Guilds.Sum(x => x.Value.TextChannels.Count)}\n" +
                $"Voice: {Context.Bot.Guilds.Sum(x => x.Value.VoiceChannels.Count)}\n" +
                $"Total: {Context.Bot.Guilds.Sum(x => x.Value.Channels.Count)}",
                true);

            embed.AddField(
                "Guilds",
                $"Count: {Context.Bot.Guilds.Count}\n" +
                $"Total Users: {Context.Bot.Guilds.Sum(x => x.Value.MemberCount)}\n" +
                $"Total Cached: {Context.Bot.Guilds.Sum(x => x.Value.Members.Count)}\n",
                true);

            embed.AddField(
                "Commands",
                $"Commands: {CmdService.GetAllCommands().Count()}\n" +
                $"Aliases: {CmdService.GetAllCommands().Sum(x => x.Aliases.Count)}\n" +
                $"Modules: {CmdService.GetAllModules().Count()}",
                true);

            embed.AddField(
                ":hammer_pick:",
                $"Heap: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB\n" +
                $"Up: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\D\ hh\H\ mm\M\ ss\S")}",
                true);

            embed.AddField(":beginner:", $"Written by: [PassiveModding](https://github.com/PassiveModding)", true);

            await ReplyAsync("", false, embed.Build());
        }

        public async Task AddRoleAsync(IRole role)
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

            var newRole = new AssignableRole
            {
                GuildId = Context.Guild.Id,
                RoleId = role.Id
            };

            await using (var db = new DataContext())
            {
                var roleMatch = db.Roles.FirstOrDefault(x => x.GuildId == newRole.GuildId && x.RoleId == newRole.RoleId);
                if (roleMatch != null)
                {
                    await ReplyAsync("That role can already be given to users you buffoon.");
                    return;
                }

                await db.Roles.AddAsync(newRole);
                await db.SaveChangesAsync();
                await ReplyAsync(
                    $"Your citizens may now be granted this role, use `{Context.Prefix}getRole {role.Name}` to be granted it.");
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
                        roleBuilder.AppendLine(role.Mention);
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
                await db.SaveChangesAsync();

                await ReplyAsync(
                    "This role has been removed from the database but not from the server or any members, do it yourself you lazy POS");
            }
        }

        [Command("AddRole")]
        [RequireMemberGuildPermissions(Permission.ManageRoles)]
        public async Task CreateRoleAsync([Remainder]string roleName)
        {
            var dRoleMatch = Context.Guild.Roles.FirstOrDefault(x => x.Value.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (dRoleMatch.Value != null)
            {
                await AddRoleAsync(dRoleMatch.Value);
                return;
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

            await AddRoleAsync(dNewRole);
        }

        [Command("LeaveRole")]
        public async Task LeaveRoleAsync(CachedRole role)
        {
            await using (var db = new DataContext())
            {
                var roleMatch = db.Roles.FirstOrDefault(x => x.GuildId == Context.Guild.Id && x.RoleId == role.Id.RawValue);
                if (roleMatch == null)
                {
                    await ReplyAsync("I cannot remove this role, as it does not exist <a:slinky:743336270972846091>");
                    return;
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
                var roleMatch = db.Roles.FirstOrDefault(x => x.GuildId == Context.Guild.Id && x.RoleId == role.Id.RawValue);
                if (roleMatch == null)
                {
                    await ReplyAsync("I cannot give you this role, mere mortal.");
                    return;
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