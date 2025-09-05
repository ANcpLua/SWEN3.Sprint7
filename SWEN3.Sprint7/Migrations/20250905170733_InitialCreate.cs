using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWEN3.Sprint7.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ticker");

            migrationBuilder.CreateTable(
                name: "CronTickers",
                schema: "ticker",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Expression = table.Column<string>(type: "text", nullable: true),
                    Request = table.Column<byte[]>(type: "bytea", nullable: true),
                    Retries = table.Column<int>(type: "integer", nullable: false),
                    RetryIntervals = table.Column<int[]>(type: "integer[]", nullable: true),
                    Function = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    InitIdentifier = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CronTickers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeTickers",
                schema: "ticker",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LockHolder = table.Column<string>(type: "text", nullable: true),
                    Request = table.Column<byte[]>(type: "bytea", nullable: true),
                    ExecutionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Exception = table.Column<string>(type: "text", nullable: true),
                    ElapsedTime = table.Column<long>(type: "bigint", nullable: false),
                    Retries = table.Column<int>(type: "integer", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    RetryIntervals = table.Column<int[]>(type: "integer[]", nullable: true),
                    BatchParent = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchRunCondition = table.Column<int>(type: "integer", nullable: true),
                    Function = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    InitIdentifier = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeTickers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeTickers_TimeTickers_BatchParent",
                        column: x => x.BatchParent,
                        principalSchema: "ticker",
                        principalTable: "TimeTickers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CronTickerOccurrences",
                schema: "ticker",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LockHolder = table.Column<string>(type: "text", nullable: true),
                    ExecutionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CronTickerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Exception = table.Column<string>(type: "text", nullable: true),
                    ElapsedTime = table.Column<long>(type: "bigint", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CronTickerOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CronTickerOccurrences_CronTickers_CronTickerId",
                        column: x => x.CronTickerId,
                        principalSchema: "ticker",
                        principalTable: "CronTickers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_document_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    log_date = table.Column<DateOnly>(type: "date", nullable: false),
                    access_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_document_access", x => x.id);
                    table.ForeignKey(
                        name: "FK_daily_document_access_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CronTickerOccurrence_CronTickerId",
                schema: "ticker",
                table: "CronTickerOccurrences",
                column: "CronTickerId");

            migrationBuilder.CreateIndex(
                name: "IX_CronTickerOccurrence_ExecutionTime",
                schema: "ticker",
                table: "CronTickerOccurrences",
                column: "ExecutionTime");

            migrationBuilder.CreateIndex(
                name: "IX_CronTickerOccurrence_Status_ExecutionTime",
                schema: "ticker",
                table: "CronTickerOccurrences",
                columns: new[] { "Status", "ExecutionTime" });

            migrationBuilder.CreateIndex(
                name: "UQ_CronTickerId_ExecutionTime",
                schema: "ticker",
                table: "CronTickerOccurrences",
                columns: new[] { "CronTickerId", "ExecutionTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CronTickers_Expression",
                schema: "ticker",
                table: "CronTickers",
                column: "Expression");

            migrationBuilder.CreateIndex(
                name: "IX_TimeTicker_ExecutionTime",
                schema: "ticker",
                table: "TimeTickers",
                column: "ExecutionTime");

            migrationBuilder.CreateIndex(
                name: "IX_TimeTicker_Status_ExecutionTime",
                schema: "ticker",
                table: "TimeTickers",
                columns: new[] { "Status", "ExecutionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeTickers_BatchParent",
                schema: "ticker",
                table: "TimeTickers",
                column: "BatchParent");

            migrationBuilder.CreateIndex(
                name: "ix_daily_document_access_document_date",
                table: "daily_document_access",
                columns: new[] { "document_id", "log_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_daily_document_access_log_date",
                table: "daily_document_access",
                column: "log_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CronTickerOccurrences",
                schema: "ticker");

            migrationBuilder.DropTable(
                name: "TimeTickers",
                schema: "ticker");

            migrationBuilder.DropTable(
                name: "daily_document_access");

            migrationBuilder.DropTable(
                name: "CronTickers",
                schema: "ticker");

            migrationBuilder.DropTable(
                name: "documents");
        }
    }
}
