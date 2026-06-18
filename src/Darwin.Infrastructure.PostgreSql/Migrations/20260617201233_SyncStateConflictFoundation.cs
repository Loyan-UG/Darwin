using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class SyncStateConflictFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncStates",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SyncScope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LastSuccessfulSyncAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRetryAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastErrorSummary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    RemoteVersion = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LocalVersion = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncStates_ExternalSystems_ExternalSystemId",
                        column: x => x.ExternalSystemId,
                        principalSchema: "Integration",
                        principalTable: "ExternalSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncConflicts",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SyncStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Resolution = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ConflictKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FieldPath = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DarwinValueSummary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ExternalValueSummary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ResolutionSummary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DetectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncConflicts_ExternalSystems_ExternalSystemId",
                        column: x => x.ExternalSystemId,
                        principalSchema: "Integration",
                        principalTable: "ExternalSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncConflicts_SyncStates_SyncStateId",
                        column: x => x.SyncStateId,
                        principalSchema: "Integration",
                        principalTable: "SyncStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflicts_EntityType_EntityId",
                schema: "Integration",
                table: "SyncConflicts",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflicts_State_Status",
                schema: "Integration",
                table: "SyncConflicts",
                columns: new[] { "SyncStateId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflicts_System_Status_Detected",
                schema: "Integration",
                table: "SyncConflicts",
                columns: new[] { "ExternalSystemId", "Status", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_SyncConflicts_System_Entity_Key",
                schema: "Integration",
                table: "SyncConflicts",
                columns: new[] { "ExternalSystemId", "EntityType", "EntityId", "ConflictKey" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_SyncStates_EntityType_EntityId",
                schema: "Integration",
                table: "SyncStates",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncStates_System_Status_NextRetry",
                schema: "Integration",
                table: "SyncStates",
                columns: new[] { "ExternalSystemId", "Status", "NextRetryAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_SyncStates_System_Entity_Direction_Scope",
                schema: "Integration",
                table: "SyncStates",
                columns: new[] { "ExternalSystemId", "EntityType", "EntityId", "Direction", "SyncScope" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncConflicts",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "SyncStates",
                schema: "Integration");
        }
    }
}
