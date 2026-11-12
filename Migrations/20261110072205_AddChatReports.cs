using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivestockMarket.Migrations
{
    /// <inheritdoc />
    public partial class AddChatReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    ListingId = table.Column<int>(type: "int", nullable: true),
                    ListingTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ReporterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReportedUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HandledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationReports_AspNetUsers_ReportedUserId",
                        column: x => x.ReportedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationReports_AspNetUsers_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationReports_ConversationId_ReporterId",
                table: "ConversationReports",
                columns: new[] { "ConversationId", "ReporterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationReports_ReportedUserId",
                table: "ConversationReports",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationReports_ReporterId",
                table: "ConversationReports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationReports_Status",
                table: "ConversationReports",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationReports");
        }
    }
}
