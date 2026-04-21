using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EduLearn.CourseService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    InstructorId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Beginner"),
                    Language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "English"),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    ThumbnailUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    TotalDuration = table.Column<int>(type: "integer", nullable: false),
                    EnrollmentCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.CourseId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Category",
                table: "Courses",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_InstructorId",
                table: "Courses",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_IsPublished_IsApproved",
                table: "Courses",
                columns: new[] { "IsPublished", "IsApproved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Courses");
        }
    }
}
