using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RideLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_invoice_line_items_ledger_entries_ledger_entry_id",
                table: "invoice_line_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_invoice_line_items",
                table: "invoice_line_items");

            migrationBuilder.DropIndex(
                name: "i_x_invoice_line_items_ledger_entry_id",
                table: "invoice_line_items");

            migrationBuilder.DropColumn(
                name: "due_date",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "is_immutable",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "outstanding_amount",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "line_item_id",
                table: "invoice_line_items");

            migrationBuilder.DropColumn(
                name: "currency",
                table: "invoice_line_items");

            migrationBuilder.RenameColumn(
                name: "subtotal_amount",
                table: "invoices",
                newName: "total_payments_applied");

            migrationBuilder.RenameColumn(
                name: "paid_amount",
                table: "invoices",
                newName: "subtotal");

            migrationBuilder.RenameColumn(
                name: "issued_at",
                table: "invoices",
                newName: "generated_at_utc");

            migrationBuilder.RenameColumn(
                name: "invoice_id",
                table: "invoices",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ledger_entry_id",
                table: "invoice_line_items",
                newName: "id");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ledger_entry_ids",
                table: "invoice_line_items",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ride_id",
                table: "invoice_line_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_invoice_line_items",
                table: "invoice_line_items",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "i_x_invoice_line_items_ride_id",
                table: "invoice_line_items",
                column: "ride_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_invoice_line_items",
                table: "invoice_line_items");

            migrationBuilder.DropIndex(
                name: "i_x_invoice_line_items_ride_id",
                table: "invoice_line_items");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "status",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "ledger_entry_ids",
                table: "invoice_line_items");

            migrationBuilder.DropColumn(
                name: "ride_id",
                table: "invoice_line_items");

            migrationBuilder.RenameColumn(
                name: "total_payments_applied",
                table: "invoices",
                newName: "subtotal_amount");

            migrationBuilder.RenameColumn(
                name: "subtotal",
                table: "invoices",
                newName: "paid_amount");

            migrationBuilder.RenameColumn(
                name: "generated_at_utc",
                table: "invoices",
                newName: "issued_at");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "invoices",
                newName: "invoice_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "invoice_line_items",
                newName: "ledger_entry_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "due_date",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_immutable",
                table: "invoices",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "outstanding_amount",
                table: "invoices",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "line_item_id",
                table: "invoice_line_items",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<string>(
                name: "currency",
                table: "invoice_line_items",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddPrimaryKey(
                name: "PK_invoice_line_items",
                table: "invoice_line_items",
                column: "line_item_id");

            migrationBuilder.CreateIndex(
                name: "i_x_invoice_line_items_ledger_entry_id",
                table: "invoice_line_items",
                column: "ledger_entry_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_invoice_line_items_ledger_entries_ledger_entry_id",
                table: "invoice_line_items",
                column: "ledger_entry_id",
                principalTable: "ledger_entries",
                principalColumn: "ledger_entry_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
