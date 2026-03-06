using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SearchService.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialSearchDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserReadMarkers",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: false),
                    LastReadUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReadMarkers", x => new { x.UserId, x.ConversationId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserReadMarkers");
        }
    }
}
