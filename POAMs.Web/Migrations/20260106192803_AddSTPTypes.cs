using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POAMs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSTPTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Assessor2Name",
                table: "SecurityTestPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Assessor2Position",
                table: "SecurityTestPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AuthorizedUser",
                table: "SecurityTestPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrivilegedUser",
                table: "SecurityTestPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScanDate",
                table: "SecurityTestPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScanPolicyName",
                table: "SecurityTestPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScannerVersion",
                table: "SecurityTestPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "SecurityTestPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "NessusTestCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SecurityTestPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    PluginId = table.Column<string>(type: "TEXT", nullable: false),
                    PluginName = table.Column<string>(type: "TEXT", nullable: false),
                    PluginFamily = table.Column<string>(type: "TEXT", nullable: false),
                    CVE = table.Column<string>(type: "TEXT", nullable: false),
                    CVSSScore = table.Column<string>(type: "TEXT", nullable: false),
                    CVSSVector = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Synopsis = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Solution = table.Column<string>(type: "TEXT", nullable: false),
                    PluginOutput = table.Column<string>(type: "TEXT", nullable: false),
                    AffectedHosts = table.Column<string>(type: "TEXT", nullable: false),
                    AffectedPorts = table.Column<string>(type: "TEXT", nullable: false),
                    ExpectedResult = table.Column<string>(type: "TEXT", nullable: false),
                    ActualResult = table.Column<string>(type: "TEXT", nullable: false),
                    RemediationSteps = table.Column<string>(type: "TEXT", nullable: false),
                    Evidence = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalScanDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RescanDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RescanRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    RescanResults = table.Column<string>(type: "TEXT", nullable: false),
                    TesterId = table.Column<int>(type: "INTEGER", nullable: true),
                    TestDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NessusTestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NessusTestCases_SecurityTestPlans_SecurityTestPlanId",
                        column: x => x.SecurityTestPlanId,
                        principalTable: "SecurityTestPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NessusTestCases_Users_TesterId",
                        column: x => x.TesterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SecurityControlTestCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SecurityTestPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    ControlFamily = table.Column<string>(type: "TEXT", nullable: false),
                    ControlId = table.Column<string>(type: "TEXT", nullable: false),
                    ControlTitle = table.Column<string>(type: "TEXT", nullable: false),
                    ControlEnhancement = table.Column<string>(type: "TEXT", nullable: false),
                    RelatedSTIGIds = table.Column<string>(type: "TEXT", nullable: false),
                    AssessmentObjective = table.Column<string>(type: "TEXT", nullable: false),
                    AssessmentObjectiveItems = table.Column<string>(type: "TEXT", nullable: false),
                    Inheritance = table.Column<int>(type: "INTEGER", nullable: false),
                    InheritedFrom = table.Column<string>(type: "TEXT", nullable: false),
                    MonitoringType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TestSteps = table.Column<string>(type: "TEXT", nullable: false),
                    OverallResult = table.Column<string>(type: "TEXT", nullable: false),
                    Comments = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewerName = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityControlTestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityControlTestCases_SecurityTestPlans_SecurityTestPlanId",
                        column: x => x.SecurityTestPlanId,
                        principalTable: "SecurityTestPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NessusTestCases_SecurityTestPlanId_PluginId",
                table: "NessusTestCases",
                columns: new[] { "SecurityTestPlanId", "PluginId" });

            migrationBuilder.CreateIndex(
                name: "IX_NessusTestCases_TesterId",
                table: "NessusTestCases",
                column: "TesterId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityControlTestCases_SecurityTestPlanId_ControlId",
                table: "SecurityControlTestCases",
                columns: new[] { "SecurityTestPlanId", "ControlId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NessusTestCases");

            migrationBuilder.DropTable(
                name: "SecurityControlTestCases");

            migrationBuilder.DropColumn(
                name: "Assessor2Name",
                table: "SecurityTestPlans");

            migrationBuilder.DropColumn(
                name: "Assessor2Position",
                table: "SecurityTestPlans");

            migrationBuilder.DropColumn(
                name: "AuthorizedUser",
                table: "SecurityTestPlans");

            migrationBuilder.DropColumn(
                name: "PrivilegedUser",
                table: "SecurityTestPlans");

            migrationBuilder.DropColumn(
                name: "ScanDate",
                table: "SecurityTestPlans");

            migrationBuilder.DropColumn(
                name: "ScanPolicyName",
                table: "SecurityTestPlans");

            migrationBuilder.DropColumn(
                name: "ScannerVersion",
                table: "SecurityTestPlans");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "SecurityTestPlans");
        }
    }
}
