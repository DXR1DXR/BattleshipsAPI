using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BattleshipsAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Player",
                columns: table => new
                {
                    Login = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Password = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NickName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WinCount = table.Column<int>(type: "integer", nullable: false),
                    LoseCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Player__5E55825A2528DE3D", x => x.Login);
                });

            migrationBuilder.CreateIndex(
                name: "UQ__Player__01E67C8B0438CAD9",
                table: "Player",
                column: "NickName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Player");
        }
    }
}
