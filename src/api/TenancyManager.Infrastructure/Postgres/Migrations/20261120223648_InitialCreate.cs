using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenancyManager.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "landlords",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_landlords", x => x.id);
                    table.ForeignKey(
                        name: "fk_landlords_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "properties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    jurisdiction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    landlord_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bedrooms = table.Column<int>(type: "integer", nullable: false),
                    address_line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_postcode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    address_town = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_properties", x => x.id);
                    table.ForeignKey(
                        name: "fk_properties_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_properties_landlords_landlord_id",
                        column: x => x.landlord_id,
                        principalTable: "landlords",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_landlords_agency_id",
                table: "landlords",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "ix_properties_agency_id",
                table: "properties",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "ix_properties_landlord_id",
                table: "properties",
                column: "landlord_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "properties");

            migrationBuilder.DropTable(
                name: "landlords");

            migrationBuilder.DropTable(
                name: "agencies");
        }
    }
}
