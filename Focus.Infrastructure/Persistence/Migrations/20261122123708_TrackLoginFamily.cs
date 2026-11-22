using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackLoginFamily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "AccountLogins",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountLogins_UserId_FamilyId",
                table: "AccountLogins",
                columns: new[] { "UserId", "FamilyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountLogins_UserId_FamilyId",
                table: "AccountLogins");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "AccountLogins");
        }
    }
}
