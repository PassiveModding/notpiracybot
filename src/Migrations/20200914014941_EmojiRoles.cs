using Microsoft.EntityFrameworkCore.Migrations;

namespace notpiracybot.Migrations
{
    public partial class EmojiRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoleEmoji",
                table: "Roles",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleEmoji",
                table: "Roles");
        }
    }
}
