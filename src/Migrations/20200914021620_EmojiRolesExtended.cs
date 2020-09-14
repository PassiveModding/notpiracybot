using Microsoft.EntityFrameworkCore.Migrations;

namespace notpiracybot.Migrations
{
    public partial class EmojiRolesExtended : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleEmoji",
                table: "Roles");

            migrationBuilder.AddColumn<bool>(
                name: "Animated",
                table: "Roles",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "EmojiId",
                table: "Roles",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmojiName",
                table: "Roles",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoleMessages",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    ChannelId = table.Column<decimal>(nullable: false),
                    MessageId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMessages", x => new { x.GuildId, x.ChannelId, x.MessageId });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleMessages");

            migrationBuilder.DropColumn(
                name: "Animated",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "EmojiId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "EmojiName",
                table: "Roles");

            migrationBuilder.AddColumn<string>(
                name: "RoleEmoji",
                table: "Roles",
                type: "text",
                nullable: true);
        }
    }
}
