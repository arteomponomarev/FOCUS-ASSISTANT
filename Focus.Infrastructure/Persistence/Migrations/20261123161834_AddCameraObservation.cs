using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCameraObservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CameraConsentAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VisionExperimental",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Vision",
                table: "ActivityAggregates",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ActivityAggregates_CameraOnly",
                table: "ActivityAggregates",
                sql: " \"Vision\" IS NULL OR (jsonb_typeof(\"Vision\") = 'object' AND \"ActiveProcessName\" IS NULL AND \"KeyboardEventCount\" = 0 AND \"MouseClickCount\" = 0 AND \"WindowSwitchCount\" = 0 AND \"IdleSeconds\" = 0) ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "ActivityAggregates" WHERE "Vision" IS NOT NULL) THEN
                        RAISE EXCEPTION 'Camera aggregates exist. Export and resolve their retention before downgrading.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_ActivityAggregates_CameraOnly",
                table: "ActivityAggregates");

            migrationBuilder.DropColumn(
                name: "CameraConsentAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VisionExperimental",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Vision",
                table: "ActivityAggregates");
        }
    }
}
