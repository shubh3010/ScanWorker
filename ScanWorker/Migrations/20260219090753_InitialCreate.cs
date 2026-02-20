using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScanWorker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventProcessingState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastProcessedEventId = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventProcessingState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CarrierId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Parcels",
                columns: table => new
                {
                    ParcelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastEventId = table.Column<long>(type: "bigint", nullable: false),
                    LastEventType = table.Column<int>(type: "int", nullable: false),
                    LastEventStatusCode = table.Column<int>(type: "int", nullable: true),
                    LastEventCreatedDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastRunId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PickupDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parcels", x => x.ParcelId);
                    table.ForeignKey(
                        name: "FK_Parcels_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ScanEvents",
                columns: table => new
                {
                    EventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParcelId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", maxLength: 50, nullable: false),
                    CreatedDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    RunId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: true),
                    DeviceTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanEvents", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_ScanEvents_Parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalTable: "Parcels",
                        principalColumn: "ParcelId");
                    table.ForeignKey(
                        name: "FK_ScanEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "do",
                table: "Parcels",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanEvents_CreatedDateTimeUtc",
                table: "ScanEvents",
                column: "CreatedDateTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ScanEvents_ParcelId",
                table: "ScanEvents",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanEvents_UserId",
                table: "ScanEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventProcessingState");

            migrationBuilder.DropTable(
                name: "ScanEvents");

            migrationBuilder.DropTable(
                name: "Parcels");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
