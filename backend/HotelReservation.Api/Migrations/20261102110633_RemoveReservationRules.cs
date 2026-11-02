using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservation.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReservationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Booking rules are deferred (see backend/README.md). These were raw SQL, so EF cannot generate this.
            migrationBuilder.Sql("""DROP TRIGGER IF EXISTS "TR_Reservations_Capacity" ON "Reservations";""");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS check_reservation_capacity();");
            migrationBuilder.Sql("""ALTER TABLE "Reservations" DROP CONSTRAINT IF EXISTS "EX_Reservations_NoOverlap";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Reservations" ADD CONSTRAINT "EX_Reservations_NoOverlap" EXCLUDE USING gist (
                    "RoomId" WITH =,
                    daterange("CheckInDate", "CheckOutDate", '[)') WITH &&
                ) WHERE ("Status" IN ('Pending', 'Confirmed'));
                """);
            migrationBuilder.Sql("""
                CREATE FUNCTION check_reservation_capacity() RETURNS trigger AS $$
                BEGIN
                    IF NEW."GuestCount" > (SELECT "Capacity" FROM "Rooms" WHERE "Id" = NEW."RoomId") THEN
                        RAISE EXCEPTION 'GuestCount % exceeds capacity of room %', NEW."GuestCount", NEW."RoomId";
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_Reservations_Capacity"
                    BEFORE INSERT OR UPDATE OF "GuestCount", "RoomId" ON "Reservations"
                    FOR EACH ROW EXECUTE FUNCTION check_reservation_capacity();
                """);
        }
    }
}
