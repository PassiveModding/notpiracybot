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
    }
}