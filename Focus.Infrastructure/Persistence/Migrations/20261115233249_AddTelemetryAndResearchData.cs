using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryAndResearchData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelfReportedProductivity",
                table: "FocusSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivityAggregates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FocusSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActiveProcessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    KeyboardEventCount = table.Column<int>(type: "integer", nullable: false),
                    MouseClickCount = table.Column<int>(type: "integer", nullable: false),
                    WindowSwitchCount = table.Column<int>(type: "integer", nullable: false),
                    IdleSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityAggregates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityAggregates_FocusSessions_FocusSessionId",
                        column: x => x.FocusSessionId,
                        principalTable: "FocusSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FocusStateRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FocusSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    RuleVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusStateRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FocusStateRecords_FocusSessions_FocusSessionId",
                        column: x => x.FocusSessionId,
                        principalTable: "FocusSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityAggregates_FocusSessionId_StartedAt",
                table: "ActivityAggregates",
                columns: new[] { "FocusSessionId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FocusStateRecords_FocusSessionId_StartedAt",
                table: "FocusStateRecords",
                columns: new[] { "FocusSessionId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityAggregates");

            migrationBuilder.DropTable(
                name: "FocusStateRecords");

            migrationBuilder.DropColumn(
                name: "SelfReportedProductivity",
                table: "FocusSessions");
        }
    }
}
