using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BooksLib.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorsAndBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    system_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "books",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    normalized_title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    genre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_credit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    isbn13 = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    isbn10 = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    publisher = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    published_on = table.Column<DateOnly>(type: "date", nullable: true),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    copy_count = table.Column<int>(type: "integer", nullable: false),
                    cover_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    collection_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    source_added_on = table.Column<DateOnly>(type: "date", nullable: true),
                    publish_on_site = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_books", x => x.id);
                    table.ForeignKey(
                        name: "FK_books_authors_author_id",
                        column: x => x.author_id,
                        principalTable: "authors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_books_genres_genre_id",
                        column: x => x.genre_id,
                        principalTable: "genres",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_authors_normalized_name",
                table: "authors",
                column: "normalized_name",
                unique: true,
                filter: "deleted_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_authors_system_code",
                table: "authors",
                column: "system_code",
                unique: true,
                filter: "system_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_books_author_id",
                table: "books",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_books_genre_id",
                table: "books",
                column: "genre_id");

            migrationBuilder.CreateIndex(
                name: "IX_books_isbn10",
                table: "books",
                column: "isbn10",
                filter: "isbn10 IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_books_isbn13",
                table: "books",
                column: "isbn13",
                filter: "isbn13 IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_books_normalized_title",
                table: "books",
                column: "normalized_title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "books");

            migrationBuilder.DropTable(
                name: "authors");
        }
    }
}
