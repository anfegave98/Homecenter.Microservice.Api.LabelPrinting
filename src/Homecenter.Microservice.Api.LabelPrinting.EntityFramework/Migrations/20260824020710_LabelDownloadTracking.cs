using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class LabelDownloadTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DownloadedAt",
                table: "PrintRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdDownloadedBy",
                table: "PrintRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrintRequests_IdDownloadedBy",
                table: "PrintRequests",
                column: "IdDownloadedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_PrintRequests_Users_IdDownloadedBy",
                table: "PrintRequests",
                column: "IdDownloadedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrintRequests_Users_IdDownloadedBy",
                table: "PrintRequests");

            migrationBuilder.DropIndex(
                name: "IX_PrintRequests_IdDownloadedBy",
                table: "PrintRequests");

            migrationBuilder.DropColumn(
                name: "DownloadedAt",
                table: "PrintRequests");

            migrationBuilder.DropColumn(
                name: "IdDownloadedBy",
                table: "PrintRequests");
        }
    }
}
