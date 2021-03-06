﻿using Disqord;
using Disqord.Bot;
using Microsoft.EntityFrameworkCore;
using Passive.Discord.Setup;
using System.Linq;
using notpiracybot.Entities;

namespace notpiracybot
{
    public class DataContext : DbContext
    {
        public DbSet<AssignableRole> Roles { get; set; }

        public DbSet<ReactableRoleMessage> RoleMessages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!DbConnection.IsInitialized)
            {
                DbConnection.Initialize(Config.LoadFromFile(null));
            }

            optionsBuilder.UseNpgsql(DbConnection.DbConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AssignableRole>(e =>
            {
                e.HasKey(x => new { x.GuildId, x.RoleId });
            });

            modelBuilder.Entity<ReactableRoleMessage>(e =>
            {
                e.HasKey(x => new { x.GuildId, x.ChannelId, x.MessageId });
            });
        }
    }
}