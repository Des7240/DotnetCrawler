using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotnetCrawler.Migrations
{
    /// <inheritdoc />
    public partial class AddUserApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "AppUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
                
            // Tự động duyệt tất cả các tài khoản cũ đã có trong DB
            migrationBuilder.Sql("UPDATE \"AppUsers\" SET \"IsApproved\" = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "AppUsers");
        }
    }
}
