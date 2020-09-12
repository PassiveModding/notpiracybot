using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Sharding;
using Disqord.Events;
using Disqord.Extensions.Interactivity;
using Disqord.Extensions.Passive;
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
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandler"/> class.
        /// </summary>
        /// <param name="bot">The discord bot events should be logged for.</param>
        /// <param name="logger">The log handler, used for handling generic logging.</param>
        public EventHandler(DiscordBotBase bot, Logger logger, InteractivityExtension ext)
        {
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