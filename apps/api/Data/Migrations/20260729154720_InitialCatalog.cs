using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BooksLib.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "genres",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    system_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_genres", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_genres_normalized_name",
                table: "genres",
                column: "normalized_name",
                unique: true,
                filter: "deleted_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_genres_system_code",
                table: "genres",
                column: "system_code",
                unique: true,
                filter: "system_code IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "genres");
        }
    }
}
