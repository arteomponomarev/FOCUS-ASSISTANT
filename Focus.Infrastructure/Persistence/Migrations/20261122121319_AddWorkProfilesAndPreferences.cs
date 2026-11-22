using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkProfilesAndPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InputProcessCategory",
                table: "FocusStateRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkProfileId",
                table: "FocusSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FocusMinutes = table.Column<int>(type: "integer", nullable: false),
                    ShortBreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    LongBreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    IntervalsBeforeLongBreak = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    Intensity = table.Column<int>(type: "integer", nullable: false),
                    AvatarId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FocusMinutes = table.Column<int>(type: "integer", nullable: false),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkProfiles", x => x.Id);
                    table.UniqueConstraint("AK_WorkProfiles_UserId_Id", x => new { x.UserId, x.Id });
                    table.ForeignKey(
                        name: "FK_WorkProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessRules",
                columns: table => new
                {
                    WorkProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessRules", x => new { x.WorkProfileId, x.ProcessName });
                    table.ForeignKey(
                        name: "FK_ProcessRules_WorkProfiles_WorkProfileId",
                        column: x => x.WorkProfileId,
                        principalTable: "WorkProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FocusSessions_UserId_WorkProfileId",
                table: "FocusSessions",
                columns: new[] { "UserId", "WorkProfileId" });

            migrationBuilder.AddForeignKey(
                name: "FK_FocusSessions_WorkProfiles_UserId_WorkProfileId",
                table: "FocusSessions",
                columns: new[] { "UserId", "WorkProfileId" },
                principalTable: "WorkProfiles",
                principalColumns: new[] { "UserId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FocusSessions_WorkProfiles_UserId_WorkProfileId",
                table: "FocusSessions");

            migrationBuilder.DropTable(
                name: "ProcessRules");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "WorkProfiles");

            migrationBuilder.DropIndex(
                name: "IX_FocusSessions_UserId_WorkProfileId",
                table: "FocusSessions");

            migrationBuilder.DropColumn(
                name: "InputProcessCategory",
                table: "FocusStateRecords");

            migrationBuilder.DropColumn(
                name: "WorkProfileId",
                table: "FocusSessions");
        }
    }
}
