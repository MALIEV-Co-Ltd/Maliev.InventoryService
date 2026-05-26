using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.InventoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicalInventoryItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "InventoryBatches",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiameterMm",
                table: "InventoryBatches",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormFactor",
                table: "InventoryBatches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Spool");

            migrationBuilder.AddColumn<decimal>(
                name: "HeightMm",
                table: "InventoryBatches",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InitialQuantity",
                table: "InventoryBatches",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthMm",
                table: "InventoryBatches",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "InventoryBatches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LowStockThresholdQuantity",
                table: "InventoryBatches",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManufacturerSku",
                table: "InventoryBatches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialGrade",
                table: "InventoryBatches",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "InventoryBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrPayload",
                table: "InventoryBatches",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuantityUnit",
                table: "InventoryBatches",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "g");

            migrationBuilder.AddColumn<string>(
                name: "ReceivedBy",
                table: "InventoryBatches",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingQuantity",
                table: "InventoryBatches",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "InventoryBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ThicknessMm",
                table: "InventoryBatches",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                table: "InventoryBatches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthMm",
                table: "InventoryBatches",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryConsumptionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    MachineId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    QuantityConsumed = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    RemainingQuantityAfter = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryConsumptionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryConsumptionEvents_InventoryBatches_InventoryBatchId",
                        column: x => x.InventoryBatchId,
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                WITH numbered_batches AS (
                    SELECT "Id",
                           row_number() OVER (ORDER BY "ReceivedAt", "Id") AS row_number
                    FROM "InventoryBatches"
                )
                UPDATE "InventoryBatches" AS batch
                SET "TrackingCode" = 'INV-' || to_char(batch."ReceivedAt", 'YY') || '-' || lpad(numbered_batches.row_number::text, 6, '0'),
                    "QrPayload" = '/mfg/inventory/items/' || ('INV-' || to_char(batch."ReceivedAt", 'YY') || '-' || lpad(numbered_batches.row_number::text, 6, '0')),
                    "InitialQuantity" = batch."InitialWeightGrams",
                    "RemainingQuantity" = batch."RemainingWeightGrams",
                    "LowStockThresholdQuantity" = batch."LowStockThresholdGrams",
                    "QuantityUnit" = 'g',
                    "FormFactor" = 'Legacy batch'
                FROM numbered_batches
                WHERE batch."Id" = numbered_batches."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "QrPayload",
                table: "InventoryBatches",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrackingCode",
                table: "InventoryBatches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_TrackingCode",
                table: "InventoryBatches",
                column: "TrackingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryConsumptionEvents_ConsumedAt",
                table: "InventoryConsumptionEvents",
                column: "ConsumedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryConsumptionEvents_InventoryBatchId",
                table: "InventoryConsumptionEvents",
                column: "InventoryBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryConsumptionEvents_JobId",
                table: "InventoryConsumptionEvents",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryConsumptionEvents");

            migrationBuilder.DropIndex(
                name: "IX_InventoryBatches_TrackingCode",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "DiameterMm",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "FormFactor",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "HeightMm",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "InitialQuantity",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "LengthMm",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "LowStockThresholdQuantity",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "ManufacturerSku",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "MaterialGrade",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "QrPayload",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "QuantityUnit",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "RemainingQuantity",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "ThicknessMm",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "WidthMm",
                table: "InventoryBatches");
        }
    }
}
