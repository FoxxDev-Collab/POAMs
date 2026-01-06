using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POAMs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddNISTCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NISTControls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ControlId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Family = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ControlText = table.Column<string>(type: "TEXT", nullable: false),
                    Discussion = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedControls = table.Column<string>(type: "TEXT", nullable: true),
                    IsWithdrawn = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentControlId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NISTControls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CCIs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NISTControlId = table.Column<int>(type: "INTEGER", nullable: false),
                    CCINumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Definition = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CCIs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CCIs_NISTControls_NISTControlId",
                        column: x => x.NISTControlId,
                        principalTable: "NISTControls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NISTControlAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NISTControlId = table.Column<int>(type: "INTEGER", nullable: false),
                    SystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    Implementation = table.Column<string>(type: "TEXT", nullable: true),
                    Evidence = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    AssessedById = table.Column<int>(type: "INTEGER", nullable: true),
                    AssessedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    POAMId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NISTControlAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NISTControlAssessments_NISTControls_NISTControlId",
                        column: x => x.NISTControlId,
                        principalTable: "NISTControls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NISTControlAssessments_POAMs_POAMId",
                        column: x => x.POAMId,
                        principalTable: "POAMs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NISTControlAssessments_Systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "Systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NISTControlAssessments_Users_AssessedById",
                        column: x => x.AssessedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CCIs_CCINumber",
                table: "CCIs",
                column: "CCINumber");

            migrationBuilder.CreateIndex(
                name: "IX_CCIs_NISTControlId",
                table: "CCIs",
                column: "NISTControlId");

            migrationBuilder.CreateIndex(
                name: "IX_NISTControlAssessments_AssessedById",
                table: "NISTControlAssessments",
                column: "AssessedById");

            migrationBuilder.CreateIndex(
                name: "IX_NISTControlAssessments_NISTControlId_SystemId",
                table: "NISTControlAssessments",
                columns: new[] { "NISTControlId", "SystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NISTControlAssessments_POAMId",
                table: "NISTControlAssessments",
                column: "POAMId");

            migrationBuilder.CreateIndex(
                name: "IX_NISTControlAssessments_SystemId",
                table: "NISTControlAssessments",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_NISTControls_ControlId",
                table: "NISTControls",
                column: "ControlId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NISTControls_Family",
                table: "NISTControls",
                column: "Family");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CCIs");

            migrationBuilder.DropTable(
                name: "NISTControlAssessments");

            migrationBuilder.DropTable(
                name: "NISTControls");
        }
    }
}
