using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EduLearn.LessonService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    LessonId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "VIDEO"),
                    ContentUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsPreview = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.LessonId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId_DisplayOrder",
                table: "Lessons",
                columns: new[] { "CourseId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId_IsPreview",
                table: "Lessons",
                columns: new[] { "CourseId", "IsPreview" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lessons");
        }
    }
}
