using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionPauses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FocusSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPauses", x => x.Id);
                    table.CheckConstraint("CK_SessionPauses_Interval", " \"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\" ");
                    table.ForeignKey(
                        name: "FK_SessionPauses_FocusSessions_FocusSessionId",
                        column: x => x.FocusSessionId,
                        principalTable: "FocusSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FocusSessions_UserId_CreatedAt",
                table: "FocusSessions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionPauses_FocusSessionId",
                table: "SessionPauses",
                column: "FocusSessionId",
                unique: true,
                filter: " \"EndedAt\" IS NULL ");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPauses_FocusSessionId_StartedAt",
                table: "SessionPauses",
                columns: new[] { "FocusSessionId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionPauses");

            migrationBuilder.DropIndex(
                name: "IX_FocusSessions_UserId_CreatedAt",
                table: "FocusSessions");
        }
    }
}
