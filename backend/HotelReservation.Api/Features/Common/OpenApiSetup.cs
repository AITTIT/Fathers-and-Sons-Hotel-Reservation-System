using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace HotelReservation.Api.Features.Common;

public static class OpenApiSetup
{
    private const string Auth = "Requires login.";
    private const string Staff = "Requires Staff or Admin.";
    private const string Admin = "Requires Admin.";

    // (HTTP method, route) -> (summary, access / behaviour notes)
    private static readonly Dictionary<(string, string), (string Summary, string Notes)> Docs = new()
    {
        [("POST", "/api/auth/register")] = ("Register a customer account", "Public. Always creates a Customer. 409 if the email is taken. Rate limited per IP."),
        [("POST", "/api/auth/login")] = ("Log in and get a JWT", "Public. Send the returned token as `Authorization: Bearer <token>`. Rate limited per IP."),
        [("GET", "/api/auth/me")] = ("Current user", Auth),
        [("POST", "/api/auth/forgot-password")] = ("Request a password reset token", "Public. Always answers the same whether or not the email exists. There is no email delivery yet; in Development the token is returned as `devToken`. Rate limited per IP."),
        [("POST", "/api/auth/reset-password")] = ("Set a new password with a reset token", "Public. Tokens are single use and expire. Rate limited per IP."),

        [("GET", "/api/users")] = ("List users", "Staff see customers only, Admin sees everyone. Filters: `role`, `search`. Paged."),
        [("PUT", "/api/users/{id}")] = ("Update a user's profile", Auth + " You can edit yourself; Admin can edit anyone. Only an Admin can change `role`, and not their own."),
        [("POST", "/api/users/{id}/change-password")] = ("Change a password", Auth + " Yourself: `currentPassword` required. Admin changing another user: not required. Rate limited per IP."),

        [("GET", "/api/hotels")] = ("List hotels", "Public. Filters: `city`, `country`. Paged."),
        [("GET", "/api/hotels/{id}")] = ("Get a hotel", "Public."),
        [("POST", "/api/hotels")] = ("Create a hotel", Admin),
        [("PUT", "/api/hotels/{id}")] = ("Update a hotel", Admin),
        [("DELETE", "/api/hotels/{id}")] = ("Delete a hotel", Admin + " Also deletes its rooms. 409 if any room has reservations."),

        [("GET", "/api/rooms")] = ("List rooms", "Public. Filters: `hotelId`, `roomType`, `minCapacity`, `maxPrice`. Inactive rooms are only returned to Staff/Admin with `includeInactive=true`. Paged."),
        [("GET", "/api/rooms/{id}")] = ("Get a room", "Public. Inactive rooms are hidden from non-staff (404)."),
        [("POST", "/api/rooms")] = ("Create a room", Staff + " `roomNumber` must be unique within the hotel."),
        [("PUT", "/api/rooms/{id}")] = ("Update a room", Staff),
        [("DELETE", "/api/rooms/{id}")] = ("Delete a room", Admin + " 409 if it has reservations; deactivate it instead."),

        [("GET", "/api/reservations")] = ("List reservations", Auth + " Customers only see their own; Staff/Admin see all. Filters: `userId`, `hotelId`, `roomId`, `status`, `from`, `to`. Paged."),
        [("GET", "/api/reservations/{id}")] = ("Get a reservation", Auth + " Customers get 404 for other people's reservations."),
        [("POST", "/api/reservations")] = ("Create a reservation", Auth + " Starts as Pending. The server sets the hotel and the total price. Staff/Admin may pass `userId` to book for someone else. 409 if the room is already booked for those dates (the check-out day is free); 400 if `guestCount` exceeds capacity."),
        [("PUT", "/api/reservations/{id}")] = ("Change dates or guest count", Auth + " Re-checks overlap and capacity and recalculates the price."),
        [("POST", "/api/reservations/{id}/confirm")] = ("Confirm a pending reservation", Staff),
        [("POST", "/api/reservations/{id}/cancel")] = ("Cancel a reservation", Auth + " Owner or Staff/Admin. Cancelled bookings free the room."),

        [("GET", "/api/payments")] = ("List payments", Auth + " Customers only see payments for their own reservations. Filters: `reservationId`, `status`. Paged."),
        [("GET", "/api/payments/{id}")] = ("Get a payment", Auth),
        [("POST", "/api/payments")] = ("Start a payment for a reservation", Auth + " The amount is the amount still owed, in EUR, and is set by the server."),
        [("POST", "/api/payments/{id}/complete")] = ("Mark a payment succeeded or failed", Staff + " Simulates the payment provider's callback. Success confirms a pending reservation."),
        [("POST", "/api/payments/{id}/cancel")] = ("Cancel a pending payment", Auth),
    };

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Hotel Reservation API",
                    Version = "v1",
                    Description = "Client-agnostic REST API (MAUI, browser, ...). Log in at `/api/auth/login`, then send "
                        + "`Authorization: Bearer <token>`. List endpoints return `{ items, total, page, pageSize }` "
                        + "(default page size 25, max 100). Errors are `{ error }` or a validation problem document."
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                };
                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var path = "/" + System.Text.RegularExpressions.Regex
                    .Replace(context.Description.RelativePath?.Split('?')[0] ?? "", @"\{(\w+):\w+\}", "{$1}").Trim('/');
                var method = context.Description.HttpMethod ?? "";

                // Tag by the second path segment: /api/hotels/... -> Hotels
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 2 && segments[0] == "api")
                    operation.Tags = [new OpenApiTag { Name = char.ToUpper(segments[1][0]) + segments[1][1..] }];

                if (Docs.TryGetValue((method, path), out var doc))
                {
                    operation.Summary = doc.Summary;
                    operation.Description = doc.Notes;
                }

                var needsAuth = context.Description.ActionDescriptor.EndpointMetadata
                    .OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any();
                if (needsAuth)
                {
                    operation.Security =
                    [
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
                        }
                    ];
                }
                return Task.CompletedTask;
            });
        });
        return services;
    }

    // Dev only: the document and UI describe every route, so they are not exposed in other environments.
    public static WebApplication MapApiDocumentation(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        app.MapOpenApi();
        app.MapScalarApiReference(o => o.WithTitle("Hotel Reservation API"));
        return app;
    }
}
