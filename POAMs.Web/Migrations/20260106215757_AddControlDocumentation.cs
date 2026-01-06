using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POAMs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddControlDocumentation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControlDocumentationRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NISTControlId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocType = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlDocumentationRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlDocumentationRequirements_NISTControls_NISTControlId",
                        column: x => x.NISTControlId,
                        principalTable: "NISTControls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ControlDocumentationInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequirementId = table.Column<int>(type: "INTEGER", nullable: false),
                    SystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    TargetDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EvidenceLocation = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReviewStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewedById = table.Column<int>(type: "INTEGER", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlDocumentationInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlDocumentationInstances_ControlDocumentationRequirements_RequirementId",
                        column: x => x.RequirementId,
                        principalTable: "ControlDocumentationRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ControlDocumentationInstances_Systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "Systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ControlDocumentationInstances_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ControlDocumentationInstances_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ControlDocumentationInstances_OwnerId",
                table: "ControlDocumentationInstances",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlDocumentationInstances_RequirementId_SystemId",
                table: "ControlDocumentationInstances",
                columns: new[] { "RequirementId", "SystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ControlDocumentationInstances_ReviewedById",
                table: "ControlDocumentationInstances",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_ControlDocumentationInstances_SystemId",
                table: "ControlDocumentationInstances",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlDocumentationRequirements_NISTControlId",
                table: "ControlDocumentationRequirements",
                column: "NISTControlId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlDocumentationRequirements_NISTControlId_DocType",
                table: "ControlDocumentationRequirements",
                columns: new[] { "NISTControlId", "DocType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ControlDocumentationInstances");

            migrationBuilder.DropTable(
                name: "ControlDocumentationRequirements");
        }
    }
}
