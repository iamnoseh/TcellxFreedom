using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TcellxFreedom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskPlannerFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AiContext = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NotificationTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NotificationBody = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HangfireJobId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTaskStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalTasks = table.Column<int>(type: "integer", nullable: false),
                    CompletedTasks = table.Column<int>(type: "integer", nullable: false),
                    SkippedTasks = table.Column<int>(type: "integer", nullable: false),
                    CompletionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AiImprovementSuggestions = table.Column<string>(type: "text", nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTaskStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsAiSuggested = table.Column<bool>(type: "boolean", nullable: false),
                    IsAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    Recurrence = table.Column<int>(type: "integer", nullable: false),
                    RecurrenceIntervalDays = table.Column<int>(type: "integer", nullable: true),
                    ParentTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    AiRationale = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanTasks_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_UserId",
                table: "Plans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTasks_PlanId",
                table: "PlanTasks",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTasks_PlanId_ScheduledAt",
                table: "PlanTasks",
                columns: new[] { "PlanId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskNotifications_PlanTaskId",
                table: "TaskNotifications",
                column: "PlanTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskNotifications_ScheduledAt",
                table: "TaskNotifications",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaskNotifications_UserId",
                table: "TaskNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTaskStatistics_UserId_WeekStartDate",
                table: "UserTaskStatistics",
                columns: new[] { "UserId", "WeekStartDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanTasks");

            migrationBuilder.DropTable(
                name: "TaskNotifications");

            migrationBuilder.DropTable(
                name: "UserTaskStatistics");

            migrationBuilder.DropTable(
                name: "Plans");
        }
    }
}
