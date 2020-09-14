using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CachingMechanism;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Sharding;
using Disqord.Events;
using Disqord.Extensions.Interactivity;
using Disqord.Extensions.Passive;
using notpiracybot.Entities;
using Passive;
using Passive.Logging;
using Qmmands;

namespace notpiracybot.Handlers
{
    /// <summary>
    /// Causym eventhandler, handles initial subscriptions to events for logging purposes.
    /// </summary>
    [Service]
    public class EventHandler
    {
        private readonly EntityCache _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandler"/> class.
        /// </summary>
        /// <param name="bot">The discord bot events should be logged for.</param>
        /// <param name="logger">The log handler, used for handling generic logging.</param>
        public EventHandler(DiscordBotBase bot, Logger logger, InteractivityExtension ext, CachingMechanism.EntityCache cache)
        {
            _cache = cache;
            Bot = bot;
            Logger = logger;
            Ext = ext;
            Bot.Logger.Logged += Logger_MessageLogged;
            if (bot is DiscordBotSharder sharder)
            {
                sharder.ShardReady += Bot_ShardReady;
            }
            Bot.Ready += ReadyAsync;
            Bot.CommandExecuted += CommandExecutedAsync;
            Bot.CommandExecutionFailed += CommandExecutionFailedAsync;
            Bot.ReactionAdded += Bot_ReactionAdded;
            Bot.ReactionRemoved += Bot_ReactionRemoved;
        }

        private async Task Bot_ReactionRemoved(ReactionRemovedEventArgs e)
        {
            if (e.User.HasValue && e.User.Value.IsBot)
            {
                return;
            }

            await HandleReactionAsync(e.User.Id, e.Message.Id, e.Channel.Id, e.Emoji, false);
        }

        public async Task HandleReactionAsync(ulong userId, ulong messageId, ulong channelId, IEmoji emoji, bool add)
        {
            // TODO: Cache valid reactions to reduce DB calls

            using (var db = new DataContext())
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                if (_cache.ExistsIgnore<ReactableRoleMessage>(messageId))
                {
                    return;
                }

                var messageMatch = _cache.Retrieve<ReactableRoleMessage>(x => x.ChannelId == channelId && x.MessageId == messageId);
                if (messageMatch == null)
                {
                    messageMatch =
                        db.RoleMessages.FirstOrDefault(x => x.ChannelId == channelId && x.MessageId == messageId);
                    if (messageMatch != null)
                    {
                        // Populate cache
                        _cache.Put(messageMatch, TimeSpan.FromHours(1));
                    }
                }

                if (messageMatch == null)
                {
                    _cache.PutIgnore<ReactableRoleMessage>(TimeSpan.FromHours(1), messageId);
                    return;
                }

                AssignableRole roleMatch;
                if (emoji is CustomEmoji ce)
                {
                    if (_cache.ExistsIgnore<AssignableRole>(messageMatch.GuildId, ce.Id.RawValue))
                    {
                        return;
                    }

                    roleMatch = _cache.Retrieve<AssignableRole>(x =>
                        x.GuildId == messageMatch.GuildId && x.EmojiId == ce.Id.RawValue) ??
                        db.Roles.FirstOrDefault(x =>
                        x.GuildId == messageMatch.GuildId && x.EmojiId == ce.Id.RawValue);

                    if (roleMatch == null)
                    {
                        _cache.PutIgnore<AssignableRole>(TimeSpan.FromHours(1), messageMatch.GuildId, ce.Id.RawValue);
                        return;
                    }
                }
                else if (emoji is Emoji em)
                {
                    if (_cache.ExistsIgnore<AssignableRole>(messageMatch.GuildId, em.Name))
                    {
                        return;
                    }

                    roleMatch = _cache.Retrieve<AssignableRole>(x =>
                        x.GuildId == messageMatch.GuildId && x.EmojiName == em.Name) ??
                        db.Roles.FirstOrDefault(x =>
                        x.GuildId == messageMatch.GuildId && x.EmojiName == em.Name);

                    if (roleMatch == null)
                    {
                        _cache.PutIgnore<AssignableRole>(TimeSpan.FromHours(1), messageMatch.GuildId, em.Name);
                        return;
                    }
                }
                else
                {
                    return;
                }

                _cache.Put(roleMatch, TimeSpan.FromHours(1));
#if DEBUG
                await Bot.SendMessageAsync(channelId, $"{sw.ElapsedMilliseconds}ms elapsed");
#endif
                if (add)
                {
                    await Bot.GrantRoleAsync(messageMatch.GuildId, userId, roleMatch.RoleId);
                }
                else
                {
                    await Bot.RevokeRoleAsync(messageMatch.GuildId, userId, roleMatch.RoleId);
                }
            }
        }

        private async Task Bot_ReactionAdded(ReactionAddedEventArgs e)
        {
            if (e.User.HasValue && e.User.Value.IsBot)
            {
                return;
            }

            await HandleReactionAsync(e.User.Id, e.Message.Id, e.Channel.Id, e.Emoji, true);
        }

        private DiscordBotBase Bot { get; }

        private Logger Logger { get; }

        public InteractivityExtension Ext { get; }

        private async Task Bot_ShardReady(Disqord.Sharding.ShardReadyEventArgs e)
        {
            Logger.Log($"Shard {e.Shard.Id} Ready, Guilds: {e.Shard.Guilds.Count}", Logger.Source.Bot);
            var prefixResponse = await Bot.PrefixProvider.GetPrefixesAsync(null);
            await e.Shard.SetPresenceAsync(new Disqord.LocalActivity($"{prefixResponse.Last()}help", Disqord.ActivityType.Watching));
        }

        public ConnectionState State = ConnectionState.Connected;

        public enum ConnectionState
        {
            Closed,

            Connected
        }

        private void Logger_MessageLogged(object sender, Disqord.Logging.LogEventArgs e)
        {
            if (e.Severity == Disqord.Logging.LogSeverity.Warning)
            {
                if (e.Message.StartsWith("Close:"))
                {
                    State = ConnectionState.Closed;
                }
            }
            else if (e.Severity == Disqord.Logging.LogSeverity.Information)
            {
                if (e.Message.StartsWith("Resumed."))
                {
                    State = ConnectionState.Connected;
                }
            }

            // TODO: Add severity back
            if (e.Exception != null)
            {
                Logger.Log(e.Message + "\n" + e.Exception.ToString(), e.Source);
                return;
            }

            Logger.Log(e.Message, e.Source);
        }

        private async Task CommandExecutionFailedAsync(CommandExecutionFailedEventArgs e)
        {
            var context = e.Context as DiscordCommandContext;
            Logger.Log(
                $"Command Failed: {e.Context.Command.Name} {e.Result.CommandExecutionStep} {e.Result.Reason}\n" +
                $"{e.Result.Exception}",
                Logger.Source.Cmd);

            await context.Channel.SendMessageAsync(
                "",
                false,
                new LocalEmbedBuilder()
                .WithTitle($"Command Failed: {e.Context.Command.Name}")
                .AddField("Reason", e.Result.Exception.Message)
                .WithColor(Color.Red)
                .Build());
        }

        private Task CommandExecutedAsync(CommandExecutedEventArgs e)
        {
            Logger.Log($"Command Executed: {e.Context.Command.Name}", Logger.Source.Cmd);
            return Task.CompletedTask;
        }

        private Task ReadyAsync(ReadyEventArgs e)
        {
            if (Bot is DiscordBotSharder sharder)
            {
                if (sharder.Shards.Count != 0)
                {
                    Logger.Log($"All Shards Ready ({string.Join(',', sharder.Shards.Select(x => x.Id))})", Logger.Source.Bot);
                }
                else
                {
                    Logger.Log($"All Shards Ready", Logger.Source.Bot);
                }

                Logger.Log($"Total Guilds: {e.Client.Guilds.Count}", Logger.Source.Bot);
            }
            State = ConnectionState.Connected;
            return Task.CompletedTask;
        }
    }
}