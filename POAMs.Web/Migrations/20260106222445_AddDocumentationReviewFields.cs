using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POAMs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentationReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "ControlDocumentationInstances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "ControlDocumentationInstances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReviewDate",
                table: "ControlDocumentationInstances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCycleMonths",
                table: "ControlDocumentationInstances",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "ControlDocumentationInstances");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "ControlDocumentationInstances");

            migrationBuilder.DropColumn(
                name: "NextReviewDate",
                table: "ControlDocumentationInstances");

            migrationBuilder.DropColumn(
                name: "ReviewCycleMonths",
                table: "ControlDocumentationInstances");
        }
    }
}
