using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryConsentAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TelemetryConsentAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActivityAggregateId",
                table: "FocusStateRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientEventId",
                table: "ActivityAggregates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ActivityAggregates_FocusSessionId_Id",
                table: "ActivityAggregates",
                columns: new[] { "FocusSessionId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_FocusStateRecords_ActivityAggregateId",
                table: "FocusStateRecords",
                column: "ActivityAggregateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FocusStateRecords_FocusSessionId_ActivityAggregateId",
                table: "FocusStateRecords",
                columns: new[] { "FocusSessionId", "ActivityAggregateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityAggregates_FocusSessionId_ClientEventId",
                table: "ActivityAggregates",
                columns: new[] { "FocusSessionId", "ClientEventId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FocusStateRecords_ActivityAggregates_FocusSessionId_Activit~",
                table: "FocusStateRecords",
                columns: new[] { "FocusSessionId", "ActivityAggregateId" },
                principalTable: "ActivityAggregates",
                principalColumns: new[] { "FocusSessionId", "Id" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FocusStateRecords_ActivityAggregates_FocusSessionId_Activit~",
                table: "FocusStateRecords");

            migrationBuilder.DropIndex(
                name: "IX_FocusStateRecords_ActivityAggregateId",
                table: "FocusStateRecords");

            migrationBuilder.DropIndex(
                name: "IX_FocusStateRecords_FocusSessionId_ActivityAggregateId",
                table: "FocusStateRecords");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ActivityAggregates_FocusSessionId_Id",
                table: "ActivityAggregates");

            migrationBuilder.DropIndex(
                name: "IX_ActivityAggregates_FocusSessionId_ClientEventId",
                table: "ActivityAggregates");

            migrationBuilder.DropColumn(
                name: "TelemetryConsentAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActivityAggregateId",
                table: "FocusStateRecords");

            migrationBuilder.DropColumn(
                name: "ClientEventId",
                table: "ActivityAggregates");
        }
    }
}
