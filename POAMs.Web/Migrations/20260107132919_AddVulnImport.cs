using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POAMs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddVulnImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NISTControls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ControlId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Family = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ControlText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Discussion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedControls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsWithdrawn = table.Column<bool>(type: "bit", nullable: false),
                    ParentControlId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NISTControls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsWindowsAuth = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ADObjectGuid = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastADSync = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ADDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ADEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VulnImportSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceApplication = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExportType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TotalSites = table.Column<int>(type: "int", nullable: false),
                    TotalHosts = table.Column<int>(type: "int", nullable: false),
                    TotalStigChecklists = table.Column<int>(type: "int", nullable: false),
                    TotalStigFindings = table.Column<int>(type: "int", nullable: false),
                    StigOpen = table.Column<int>(type: "int", nullable: false),
                    StigNotAFinding = table.Column<int>(type: "int", nullable: false),
                    StigNotApplicable = table.Column<int>(type: "int", nullable: false),
                    StigNotReviewed = table.Column<int>(type: "int", nullable: false),
                    TotalNessusVulns = table.Column<int>(type: "int", nullable: false),
                    NessusCritical = table.Column<int>(type: "int", nullable: false),
                    NessusHigh = table.Column<int>(type: "int", nullable: false),
                    NessusMedium = table.Column<int>(type: "int", nullable: false),
                    NessusLow = table.Column<int>(type: "int", nullable: false),
                    NessusInfo = table.Column<int>(type: "int", nullable: false),
                    UniqueCCIs = table.Column<int>(type: "int", nullable: false),
                    CCIsWithOpenFindings = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VulnImportSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CCIs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NISTControlId = table.Column<int>(type: "int", nullable: false),
                    CCINumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "ControlDocumentationRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NISTControlId = table.Column<int>(type: "int", nullable: false),
                    DocType = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "ADConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LdapServer = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LdapPort = table.Column<int>(type: "int", nullable: false),
                    UseSsl = table.Column<bool>(type: "bit", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    BaseDN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ServiceAccountUsername = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ServiceAccountPasswordEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AutoCreateUsers = table.Column<bool>(type: "bit", nullable: false),
                    DefaultNewUserRole = table.Column<int>(type: "int", nullable: false),
                    LastConnectionTest = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastConnectionSuccess = table.Column<bool>(type: "bit", nullable: true),
                    LastConnectionError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ADConfigurations_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Systems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OrganizationName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ISType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ISSMId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Systems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Systems_Users_ISSMId",
                        column: x => x.ISSMId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VulnImportHosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VulnImportSessionId = table.Column<int>(type: "int", nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DnsName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OperatingSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VulnImportHosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VulnImportHosts_VulnImportSessions_VulnImportSessionId",
                        column: x => x.VulnImportSessionId,
                        principalTable: "VulnImportSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ControlDocumentationInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequirementId = table.Column<int>(type: "int", nullable: false),
                    SystemId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: true),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EvidenceLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewStatus = table.Column<int>(type: "int", nullable: false),
                    ReviewedById = table.Column<int>(type: "int", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewCycleMonths = table.Column<int>(type: "int", nullable: true),
                    NextReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "POAMs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemId = table.Column<int>(type: "int", nullable: false),
                    ItemIdentifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WeaknessOrDeficiency = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SecurityControl = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    POCId = table.Column<int>(type: "int", nullable: true),
                    ResourcesRequired = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScheduledCompletionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdentifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OriginalPOAMDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POAMs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_POAMs_Systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "Systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_POAMs_Users_POCId",
                        column: x => x.POCId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VulnImportNessusVulns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VulnImportHostId = table.Column<int>(type: "int", nullable: false),
                    PluginId = table.Column<int>(type: "int", nullable: false),
                    PluginName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Family = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CVE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Synopsis = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsExploitable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VulnImportNessusVulns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VulnImportNessusVulns_VulnImportHosts_VulnImportHostId",
                        column: x => x.VulnImportHostId,
                        principalTable: "VulnImportHosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VulnImportStigResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VulnImportHostId = table.Column<int>(type: "int", nullable: false),
                    StigId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StigTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VulnId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RuleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RuleTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CCIs = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VulnImportStigResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VulnImportStigResults_VulnImportHosts_VulnImportHostId",
                        column: x => x.VulnImportHostId,
                        principalTable: "VulnImportHosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POAMId = table.Column<int>(type: "int", nullable: false),
                    MilestoneNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Changes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssignedToId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestones_POAMs_POAMId",
                        column: x => x.POAMId,
                        principalTable: "POAMs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Milestones_Users_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NISTControlAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NISTControlId = table.Column<int>(type: "int", nullable: false),
                    SystemId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: true),
                    Implementation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssessedById = table.Column<int>(type: "int", nullable: true),
                    AssessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    POAMId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "SecurityTestPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    POAMId = table.Column<int>(type: "int", nullable: true),
                    SystemId = table.Column<int>(type: "int", nullable: false),
                    STPIdentifier = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TestPlanVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TestPlanDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    STIGName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    STIGVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    STIGReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScanPolicyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScanDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScannerVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HostName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OSPlatform = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MACCAL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeadAssessorId = table.Column<int>(type: "int", nullable: true),
                    Assessor2Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Assessor2Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessmentTeam = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ISSMId = table.Column<int>(type: "int", nullable: true),
                    SystemOwner = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorizingOfficial = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrivilegedUser = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorizedUser = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TestStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TestEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
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
                name: "UserAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POAMId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AssignmentType = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAssignments_POAMs_POAMId",
                        column: x => x.POAMId,
                        principalTable: "POAMs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAssignments_Users_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NessusTestCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityTestPlanId = table.Column<int>(type: "int", nullable: false),
                    PluginId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PluginName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PluginFamily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CVE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CVSSScore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CVSSVector = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Synopsis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Solution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PluginOutput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AffectedHosts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AffectedPorts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActualResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RemediationSteps = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OriginalScanDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RescanDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RescanRequired = table.Column<bool>(type: "bit", nullable: false),
                    RescanResults = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TesterId = table.Column<int>(type: "int", nullable: true),
                    TestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityTestPlanId = table.Column<int>(type: "int", nullable: false),
                    ControlFamily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ControlId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ControlTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ControlEnhancement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatedSTIGIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessmentObjective = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessmentObjectiveItems = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Inheritance = table.Column<int>(type: "int", nullable: false),
                    InheritedFrom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonitoringType = table.Column<int>(type: "int", nullable: false),
                    AssessExamine = table.Column<bool>(type: "bit", nullable: false),
                    AssessInterview = table.Column<bool>(type: "bit", nullable: false),
                    AssessTest = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TestSteps = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "STPTestCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityTestPlanId = table.Column<int>(type: "int", nullable: false),
                    VulnId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RuleId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    RuleVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FixText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActualResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TesterId = table.Column<int>(type: "int", nullable: true),
                    TestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetestRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "IX_ADConfigurations_ModifiedById",
                table: "ADConfigurations",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CCIs_CCINumber",
                table: "CCIs",
                column: "CCINumber");

            migrationBuilder.CreateIndex(
                name: "IX_CCIs_NISTControlId",
                table: "CCIs",
                column: "NISTControlId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_AssignedToId",
                table: "Milestones",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_POAMId",
                table: "Milestones",
                column: "POAMId");

            migrationBuilder.CreateIndex(
                name: "IX_NessusTestCases_SecurityTestPlanId_PluginId",
                table: "NessusTestCases",
                columns: new[] { "SecurityTestPlanId", "PluginId" });

            migrationBuilder.CreateIndex(
                name: "IX_NessusTestCases_TesterId",
                table: "NessusTestCases",
                column: "TesterId");

            migrationBuilder.CreateIndex(
                name: "IX_NISTControlAssessments_AssessedById",
                table: "NISTControlAssessments",
                column: "AssessedById");

            migrationBuilder.CreateIndex(
                name: "IX_NISTControlAssessments_AssessedDate",
                table: "NISTControlAssessments",
                column: "AssessedDate");

            migrationBuilder.CreateIndex(
                name: "IX_NISTControlAssessments_NISTControlId_SystemId",
                table: "NISTControlAssessments",
                columns: new[] { "NISTControlId", "SystemId" });

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

            migrationBuilder.CreateIndex(
                name: "IX_POAMs_ItemIdentifier",
                table: "POAMs",
                column: "ItemIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_POAMs_POCId",
                table: "POAMs",
                column: "POCId");

            migrationBuilder.CreateIndex(
                name: "IX_POAMs_SystemId",
                table: "POAMs",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityControlTestCases_SecurityTestPlanId_ControlId",
                table: "SecurityControlTestCases",
                columns: new[] { "SecurityTestPlanId", "ControlId" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Systems_ISSMId",
                table: "Systems",
                column: "ISSMId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignments_AssignedById",
                table: "UserAssignments",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignments_POAMId_UserId_AssignmentType",
                table: "UserAssignments",
                columns: new[] { "POAMId", "UserId", "AssignmentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignments_UserId",
                table: "UserAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VulnImportHosts_VulnImportSessionId",
                table: "VulnImportHosts",
                column: "VulnImportSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VulnImportNessusVulns_Severity",
                table: "VulnImportNessusVulns",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_VulnImportNessusVulns_VulnImportHostId",
                table: "VulnImportNessusVulns",
                column: "VulnImportHostId");

            migrationBuilder.CreateIndex(
                name: "IX_VulnImportSessions_ImportDate",
                table: "VulnImportSessions",
                column: "ImportDate");

            migrationBuilder.CreateIndex(
                name: "IX_VulnImportStigResults_Status",
                table: "VulnImportStigResults",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VulnImportStigResults_VulnImportHostId",
                table: "VulnImportStigResults",
                column: "VulnImportHostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADConfigurations");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CCIs");

            migrationBuilder.DropTable(
                name: "ControlDocumentationInstances");

            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "NessusTestCases");

            migrationBuilder.DropTable(
                name: "NISTControlAssessments");

            migrationBuilder.DropTable(
                name: "SecurityControlTestCases");

            migrationBuilder.DropTable(
                name: "STPTestCases");

            migrationBuilder.DropTable(
                name: "UserAssignments");

            migrationBuilder.DropTable(
                name: "VulnImportNessusVulns");

            migrationBuilder.DropTable(
                name: "VulnImportStigResults");

            migrationBuilder.DropTable(
                name: "ControlDocumentationRequirements");

            migrationBuilder.DropTable(
                name: "SecurityTestPlans");

            migrationBuilder.DropTable(
                name: "VulnImportHosts");

            migrationBuilder.DropTable(
                name: "NISTControls");

            migrationBuilder.DropTable(
                name: "POAMs");

            migrationBuilder.DropTable(
                name: "VulnImportSessions");

            migrationBuilder.DropTable(
                name: "Systems");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
