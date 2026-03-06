using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConversationMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Members",
                table: "conversations");

            migrationBuilder.CreateTable(
                name: "conversation_members",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: false),
                    LastReadUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_members", x => new { x.UserId, x.ConversationId });
                    table.ForeignKey(
                        name: "FK_conversation_members_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_members_ConversationId",
                table: "conversation_members",
                column: "ConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_members");

            migrationBuilder.AddColumn<HashSet<string>>(
                name: "Members",
                table: "conversations",
                type: "text[]",
                nullable: false);
        }
    }
}
