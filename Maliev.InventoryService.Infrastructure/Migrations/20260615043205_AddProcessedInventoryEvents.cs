using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.InventoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedInventoryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedInventoryEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedInventoryEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedInventoryEvents_JobId",
                table: "ProcessedInventoryEvents",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedInventoryEvents_MessageId",
                table: "ProcessedInventoryEvents",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedInventoryEvents");
        }
    }
}
