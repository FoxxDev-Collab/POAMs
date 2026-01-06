using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POAMs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NISTControlAssessments_NISTControlId_SystemId",
                table: "NISTControlAssessments");

            migrationBuilder.CreateIndex(
                name: "IX_NISTControlAssessments_AssessedDate",
                table: "NISTControlAssessments",
                column: "AssessedDate");

            migrationBuilder.CreateIndex(
                name: "IX_NISTControlAssessments_NISTControlId_SystemId",
                table: "NISTControlAssessments",
                columns: new[] { "NISTControlId", "SystemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NISTControlAssessments_AssessedDate",
                table: "NISTControlAssessments");

            migrationBuilder.DropIndex(
                name: "IX_NISTControlAssessments_NISTControlId_SystemId",
                table: "NISTControlAssessments");

            migrationBuilder.CreateIndex(
                name: "IX_NISTControlAssessments_NISTControlId_SystemId",
                table: "NISTControlAssessments",
                columns: new[] { "NISTControlId", "SystemId" },
                unique: true);
        }
    }
}
