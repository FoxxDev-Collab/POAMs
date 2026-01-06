using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POAMs.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSTPTestCaseProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EvidenceNotes",
                table: "STPTestCases",
                newName: "FixText");

            migrationBuilder.AddColumn<string>(
                name: "CheckContent",
                table: "STPTestCases",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "STPTestCases",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Evidence",
                table: "STPTestCases",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AssessExamine",
                table: "SecurityControlTestCases",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AssessInterview",
                table: "SecurityControlTestCases",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AssessTest",
                table: "SecurityControlTestCases",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckContent",
                table: "STPTestCases");

            migrationBuilder.DropColumn(
                name: "Comments",
                table: "STPTestCases");

            migrationBuilder.DropColumn(
                name: "Evidence",
                table: "STPTestCases");

            migrationBuilder.DropColumn(
                name: "AssessExamine",
                table: "SecurityControlTestCases");

            migrationBuilder.DropColumn(
                name: "AssessInterview",
                table: "SecurityControlTestCases");

            migrationBuilder.DropColumn(
                name: "AssessTest",
                table: "SecurityControlTestCases");

            migrationBuilder.RenameColumn(
                name: "FixText",
                table: "STPTestCases",
                newName: "EvidenceNotes");
        }
    }
}
