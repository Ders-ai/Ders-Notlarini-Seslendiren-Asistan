using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesliAsistan.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Hafta5_SocialNetworkAndModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shared_contents_Privacy",
                table: "shared_contents");

            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspensionExpiresAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "study_groups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                table: "shared_contents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnderReview",
                table: "shared_contents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReportCount",
                table: "shared_contents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "shared_contents",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SectionId",
                table: "quizzes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "quizzes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "admin_actions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admin_actions_users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_study_groups_InviteCode",
                table: "study_groups",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shared_contents_Privacy_CreatedAt",
                table: "shared_contents",
                columns: new[] { "Privacy", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_actions_AdminId",
                table: "admin_actions",
                column: "AdminId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_actions");

            migrationBuilder.DropIndex(
                name: "IX_study_groups_InviteCode",
                table: "study_groups");

            migrationBuilder.DropIndex(
                name: "IX_shared_contents_Privacy_CreatedAt",
                table: "shared_contents");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SuspensionExpiresAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "study_groups");

            migrationBuilder.DropColumn(
                name: "IsRemoved",
                table: "shared_contents");

            migrationBuilder.DropColumn(
                name: "IsUnderReview",
                table: "shared_contents");

            migrationBuilder.DropColumn(
                name: "ReportCount",
                table: "shared_contents");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "shared_contents");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "quizzes");

            migrationBuilder.CreateIndex(
                name: "IX_shared_contents_Privacy",
                table: "shared_contents",
                column: "Privacy");
        }
    }
}
