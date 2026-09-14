using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ContosoDashboard.Data;

#nullable disable

namespace ContosoDashboard.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260914000100_DocumentManagement")]
public partial class DocumentManagementMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Documents",
            columns: table => new
            {
                DocumentId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Tags = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                FilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                FileType = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                UploaderId = table.Column<int>(type: "INTEGER", nullable: false),
                ProjectId = table.Column<int>(type: "INTEGER", nullable: true),
                UploadedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                ScannedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Documents", x => x.DocumentId);
                table.ForeignKey(
                    name: "FK_Documents_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "ProjectId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Documents_Users_UploaderId",
                    column: x => x.UploaderId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DocumentShares",
            columns: table => new
            {
                DocumentShareId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                SharedWithUserId = table.Column<int>(type: "INTEGER", nullable: true),
                SharedWithTeam = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                SharedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                SharedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentShares", x => x.DocumentShareId);
                table.ForeignKey(
                    name: "FK_DocumentShares_Documents_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "Documents",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DocumentAuditEvents",
            columns: table => new
            {
                DocumentAuditEventId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DocumentId = table.Column<int>(type: "INTEGER", nullable: true),
                ActorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Outcome = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                OccurredDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Details = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentAuditEvents", x => x.DocumentAuditEventId);
                table.ForeignKey(
                    name: "FK_DocumentAuditEvents_Documents_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "Documents",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "TaskDocuments",
            columns: table => new
            {
                TaskDocumentId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TaskId = table.Column<int>(type: "INTEGER", nullable: false),
                DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                AttachedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                AttachedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TaskDocuments", x => x.TaskDocumentId);
                table.ForeignKey(
                    name: "FK_TaskDocuments_Documents_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "Documents",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TaskDocuments_Tasks_TaskId",
                    column: x => x.TaskId,
                    principalTable: "Tasks",
                    principalColumn: "TaskId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DocumentAuditEvents_DocumentId",
            table: "DocumentAuditEvents",
            column: "DocumentId");

        migrationBuilder.CreateIndex(
            name: "IX_DocumentShares_DocumentId_SharedWithUserId",
            table: "DocumentShares",
            columns: new[] { "DocumentId", "SharedWithUserId" });

        migrationBuilder.CreateIndex(
            name: "IX_Documents_ProjectId_UploadedDate",
            table: "Documents",
            columns: new[] { "ProjectId", "UploadedDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Documents_Status_Category",
            table: "Documents",
            columns: new[] { "Status", "Category" });

        migrationBuilder.CreateIndex(
            name: "IX_Documents_UploaderId_UploadedDate",
            table: "Documents",
            columns: new[] { "UploaderId", "UploadedDate" });

        migrationBuilder.CreateIndex(
            name: "IX_TaskDocuments_DocumentId",
            table: "TaskDocuments",
            column: "DocumentId");

        migrationBuilder.CreateIndex(
            name: "IX_TaskDocuments_TaskId_DocumentId",
            table: "TaskDocuments",
            columns: new[] { "TaskId", "DocumentId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DocumentAuditEvents");

        migrationBuilder.DropTable(
            name: "DocumentShares");

        migrationBuilder.DropTable(
            name: "TaskDocuments");

        migrationBuilder.DropTable(
            name: "Documents");
    }
}
