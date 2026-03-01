using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.InventoryService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitialWeightGrams = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingWeightGrams = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LowStockThresholdGrams = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 100m),
                    HasAlerted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_FIFO",
                table: "InventoryBatches",
                columns: new[] { "MaterialId", "Status", "ReceivedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_MaterialId",
                table: "InventoryBatches",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_ReceivedAt",
                table: "InventoryBatches",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_Status",
                table: "InventoryBatches",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryBatches");
        }
    }
}
