using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TcellxFreedom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTcellPassFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LevelRewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    RewardType = table.Column<int>(type: "integer", nullable: false),
                    RewardDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RewardQuantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelRewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PassTaskTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    XpReward = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    IsPremiumOnly = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassTaskTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTcellPasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TotalXp = table.Column<int>(type: "integer", nullable: false),
                    CurrentLevel = table.Column<int>(type: "integer", nullable: false),
                    CurrentStreakDays = table.Column<int>(type: "integer", nullable: false),
                    LongestStreak = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    PremiumExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastStreakDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTcellPasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserLevelRewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    LevelRewardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLevelRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLevelRewards_LevelRewards_LevelRewardId",
                        column: x => x.LevelRewardId,
                        principalTable: "LevelRewards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserDailyTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserTcellPassId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassTaskTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedDayNumber = table.Column<int>(type: "integer", nullable: false),
                    AssignedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    XpAwarded = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDailyTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDailyTasks_PassTaskTemplates_PassTaskTemplateId",
                        column: x => x.PassTaskTemplateId,
                        principalTable: "PassTaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDailyTasks_UserTcellPasses_UserTcellPassId",
                        column: x => x.UserTcellPassId,
                        principalTable: "UserTcellPasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LevelRewards_Level_Tier",
                table: "LevelRewards",
                columns: new[] { "Level", "Tier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PassTaskTemplates_DayNumber",
                table: "PassTaskTemplates",
                column: "DayNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PassTaskTemplates_DayNumber_SortOrder",
                table: "PassTaskTemplates",
                columns: new[] { "DayNumber", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDailyTasks_AssignedDate_Status",
                table: "UserDailyTasks",
                columns: new[] { "AssignedDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDailyTasks_PassTaskTemplateId",
                table: "UserDailyTasks",
                column: "PassTaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDailyTasks_UserId_AssignedDate",
                table: "UserDailyTasks",
                columns: new[] { "UserId", "AssignedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDailyTasks_UserTcellPassId",
                table: "UserDailyTasks",
                column: "UserTcellPassId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLevelRewards_LevelRewardId",
                table: "UserLevelRewards",
                column: "LevelRewardId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLevelRewards_UserId",
                table: "UserLevelRewards",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLevelRewards_UserId_LevelRewardId",
                table: "UserLevelRewards",
                columns: new[] { "UserId", "LevelRewardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTcellPasses_TotalXp",
                table: "UserTcellPasses",
                column: "TotalXp");

            migrationBuilder.CreateIndex(
                name: "IX_UserTcellPasses_UserId",
                table: "UserTcellPasses",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDailyTasks");

            migrationBuilder.DropTable(
                name: "UserLevelRewards");

            migrationBuilder.DropTable(
                name: "PassTaskTemplates");

            migrationBuilder.DropTable(
                name: "UserTcellPasses");

            migrationBuilder.DropTable(
                name: "LevelRewards");
        }
    }
}
