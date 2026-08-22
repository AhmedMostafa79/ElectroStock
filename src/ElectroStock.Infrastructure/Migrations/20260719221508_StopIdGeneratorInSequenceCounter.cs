using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectroStock.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StopIdGeneratorInSequenceCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing table
            migrationBuilder.DropTable(name: "SequenceCounters");

            // Create new table WITHOUT identity
            migrationBuilder.CreateTable(
                name: "SequenceCounters",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),  // ← No identity!
                    CounterType = table.Column<string>(maxLength: 10, nullable: false),
                    CurrentDate = table.Column<DateOnly>(nullable: false),
                    LastNumber = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenceCounters", x => x.Id);
                    table.UniqueConstraint("UK_SequenceCounters_CounterType", x => x.CounterType);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SequenceCounters_CounterType",
                table: "SequenceCounters");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SequenceCounters",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");
        }
    }
}
