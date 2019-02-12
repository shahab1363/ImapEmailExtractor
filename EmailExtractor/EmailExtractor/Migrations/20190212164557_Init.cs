using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EmailExtractor.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailItems",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Server = table.Column<string>(nullable: false),
                    Folder = table.Column<string>(nullable: false),
                    UniqueId = table.Column<long>(nullable: false),
                    EmailAddress = table.Column<string>(nullable: false),
                    FullName = table.Column<string>(nullable: true),
                    Domain = table.Column<string>(nullable: true),
                    MessageDateTime = table.Column<DateTimeOffset>(nullable: false),
                    MessageEml = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailItems_EmailAddress",
                table: "MailItems",
                column: "EmailAddress");

            migrationBuilder.CreateIndex(
                name: "IX_MailItems_EmailAddress_Folder_UniqueId_Server",
                table: "MailItems",
                columns: new[] { "EmailAddress", "Folder", "UniqueId", "Server" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailItems");
        }
    }
}
