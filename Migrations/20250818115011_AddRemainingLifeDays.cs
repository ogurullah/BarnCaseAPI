using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarnCaseAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingLifeDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RemainingLifeDays",
                table: "Animals",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingLifeDays",
                table: "Animals");
        }
    }
}
