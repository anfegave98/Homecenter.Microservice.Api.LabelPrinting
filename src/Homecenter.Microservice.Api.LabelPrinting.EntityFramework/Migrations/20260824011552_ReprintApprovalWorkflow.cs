using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class ReprintApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalNote",
                table: "PrintRequests",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DecidedAt",
                table: "PrintRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdApprover",
                table: "PrintRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrintRequests_IdApprover",
                table: "PrintRequests",
                column: "IdApprover");

            migrationBuilder.CreateIndex(
                name: "IX_PrintRequests_Result_ProcessedAt",
                table: "PrintRequests",
                columns: new[] { "Result", "ProcessedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_PrintRequests_Users_IdApprover",
                table: "PrintRequests",
                column: "IdApprover",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrintRequests_Users_IdApprover",
                table: "PrintRequests");

            migrationBuilder.DropIndex(
                name: "IX_PrintRequests_IdApprover",
                table: "PrintRequests");

            migrationBuilder.DropIndex(
                name: "IX_PrintRequests_Result_ProcessedAt",
                table: "PrintRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalNote",
                table: "PrintRequests");

            migrationBuilder.DropColumn(
                name: "DecidedAt",
                table: "PrintRequests");

            migrationBuilder.DropColumn(
                name: "IdApprover",
                table: "PrintRequests");
        }
    }
}
