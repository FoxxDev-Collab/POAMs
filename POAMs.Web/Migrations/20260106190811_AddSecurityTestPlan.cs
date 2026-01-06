using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POAMs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityTestPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecurityTestPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    POAMId = table.Column<int>(type: "INTEGER", nullable: true),
                    SystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    STPIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    STIGName = table.Column<string>(type: "TEXT", nullable: false),
                    STIGVersion = table.Column<string>(type: "TEXT", nullable: false),
                    STIGReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TestPlanVersion = table.Column<string>(type: "TEXT", nullable: false),
                    TestPlanDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HostName = table.Column<string>(type: "TEXT", nullable: false),
                    IPAddress = table.Column<string>(type: "TEXT", nullable: false),
                    OSPlatform = table.Column<string>(type: "TEXT", nullable: false),
                    Environment = table.Column<string>(type: "TEXT", nullable: false),
                    Classification = table.Column<string>(type: "TEXT", nullable: false),
                    MACCAL = table.Column<string>(type: "TEXT", nullable: false),
                    LeadAssessorId = table.Column<int>(type: "INTEGER", nullable: true),
                    AssessmentTeam = table.Column<string>(type: "TEXT", nullable: false),
                    ISSMId = table.Column<int>(type: "INTEGER", nullable: true),
                    SystemOwner = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorizingOfficial = table.Column<string>(type: "TEXT", nullable: false),
                    TestStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TestEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityTestPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityTestPlans_POAMs_POAMId",
                        column: x => x.POAMId,
                        principalTable: "POAMs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecurityTestPlans_Systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "Systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityTestPlans_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecurityTestPlans_Users_ISSMId",
                        column: x => x.ISSMId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecurityTestPlans_Users_LeadAssessorId",
                        column: x => x.LeadAssessorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "STPTestCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SecurityTestPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    VulnId = table.Column<string>(type: "TEXT", nullable: false),
                    RuleId = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    RuleVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    ExpectedResult = table.Column<string>(type: "TEXT", nullable: false),
                    ActualResult = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceNotes = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TesterId = table.Column<int>(type: "INTEGER", nullable: true),
                    TestDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RetestRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STPTestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_STPTestCases_SecurityTestPlans_SecurityTestPlanId",
                        column: x => x.SecurityTestPlanId,
                        principalTable: "SecurityTestPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_STPTestCases_Users_TesterId",
                        column: x => x.TesterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityTestPlans_CreatedById",
                table: "SecurityTestPlans",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityTestPlans_ISSMId",
                table: "SecurityTestPlans",
                column: "ISSMId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityTestPlans_LeadAssessorId",
                table: "SecurityTestPlans",
                column: "LeadAssessorId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityTestPlans_POAMId",
                table: "SecurityTestPlans",
                column: "POAMId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityTestPlans_STPIdentifier",
                table: "SecurityTestPlans",
                column: "STPIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityTestPlans_SystemId",
                table: "SecurityTestPlans",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_STPTestCases_SecurityTestPlanId_VulnId",
                table: "STPTestCases",
                columns: new[] { "SecurityTestPlanId", "VulnId" });

            migrationBuilder.CreateIndex(
                name: "IX_STPTestCases_TesterId",
                table: "STPTestCases",
                column: "TesterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "STPTestCases");

            migrationBuilder.DropTable(
                name: "SecurityTestPlans");
        }
    }
}
