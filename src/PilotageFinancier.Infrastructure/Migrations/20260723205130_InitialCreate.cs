using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PilotageFinancier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetsVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodePCGE = table.Column<string>(type: "TEXT", nullable: false),
                    Exercice = table.Column<int>(type: "INTEGER", nullable: false),
                    MontantVote = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DateValidite = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetsVotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EcrituresNormalisees",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceBatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodePCGE = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MontantDebit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MontantCredit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcrituresNormalisees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeriesAgregees",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TypeSerie = table.Column<int>(type: "INTEGER", nullable: false),
                    Granularite = table.Column<int>(type: "INTEGER", nullable: false),
                    Periode = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Valeur = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CodePCGE = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesAgregees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    CreeLe = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    NomFichier = table.Column<string>(type: "TEXT", nullable: false),
                    ImporteLe = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NbLignes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportBatches_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodeClientSource = table.Column<string>(type: "TEXT", nullable: false),
                    CodePCGENormalise = table.Column<string>(type: "TEXT", nullable: false),
                    CreeLe = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mappings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EcrituresBrutes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodeSource = table.Column<string>(type: "TEXT", nullable: false),
                    Libelle = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MontantDebit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MontantCredit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcrituresBrutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EcrituresBrutes_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetsVotes_TenantId_CodePCGE_Exercice_DateValidite",
                table: "BudgetsVotes",
                columns: new[] { "TenantId", "CodePCGE", "Exercice", "DateValidite" });

            migrationBuilder.CreateIndex(
                name: "IX_EcrituresBrutes_ImportBatchId",
                table: "EcrituresBrutes",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_EcrituresBrutes_TenantId_ImportBatchId",
                table: "EcrituresBrutes",
                columns: new[] { "TenantId", "ImportBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_EcrituresNormalisees_TenantId_Date",
                table: "EcrituresNormalisees",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_TenantId_Type",
                table: "ImportBatches",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Mappings_TenantId_CodeClientSource",
                table: "Mappings",
                columns: new[] { "TenantId", "CodeClientSource" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeriesAgregees_TenantId_TypeSerie_Granularite_Periode",
                table: "SeriesAgregees",
                columns: new[] { "TenantId", "TypeSerie", "Granularite", "Periode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetsVotes");

            migrationBuilder.DropTable(
                name: "EcrituresBrutes");

            migrationBuilder.DropTable(
                name: "EcrituresNormalisees");

            migrationBuilder.DropTable(
                name: "Mappings");

            migrationBuilder.DropTable(
                name: "SeriesAgregees");

            migrationBuilder.DropTable(
                name: "ImportBatches");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
