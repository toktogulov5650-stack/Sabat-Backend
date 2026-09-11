using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialNewsAndContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contact_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "news_articles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    author = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    cover_image_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news_articles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "news_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    news_article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    excerpt = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news_translations", x => x.id);
                    table.ForeignKey(
                        name: "FK_news_translations_news_articles_news_article_id",
                        column: x => x.news_article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contact_messages_email",
                table: "contact_messages",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_contact_messages_status_created_at",
                table: "contact_messages",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_news_articles_category",
                table: "news_articles",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_news_articles_slug",
                table: "news_articles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_news_articles_status_published_at",
                table: "news_articles",
                columns: new[] { "status", "published_at" });

            migrationBuilder.CreateIndex(
                name: "IX_news_translations_news_article_id_language",
                table: "news_translations",
                columns: new[] { "news_article_id", "language" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact_messages");

            migrationBuilder.DropTable(
                name: "news_translations");

            migrationBuilder.DropTable(
                name: "news_articles");
        }
    }
}
