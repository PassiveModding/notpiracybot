﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using notpiracybot;

namespace notpiracybot.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20200914021620_EmojiRolesExtended")]
    partial class EmojiRolesExtended
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("notpiracybot.AssignableRole", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("RoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<bool>("Animated")
                        .HasColumnType("boolean");

                    b.Property<decimal?>("EmojiId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("EmojiName")
                        .HasColumnType("text");

                    b.HasKey("GuildId", "RoleId");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("notpiracybot.Entities.ReactableRoleMessage", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("MessageId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("GuildId", "ChannelId", "MessageId");

                    b.ToTable("RoleMessages");
                });
#pragma warning restore 612, 618
        }
    }
}
