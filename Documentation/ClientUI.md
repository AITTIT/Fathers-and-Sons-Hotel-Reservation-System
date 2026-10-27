## Customer views

### C1 – Room search

Start page. The search form is the main element; recommended rooms below it.

```text
┌─ C1 Room search ────┬────────────────────────────────────────────────────────┐
│ ▣ Hotel Reservation │                                                        │
│                     │                                [ ◍ English · EUR ▾ ]   │
│ ▶ ⌕ Find rooms      │ ────────────────────────────────────────────────────── │
│   ▦ My bookings     │                                                        │
│   ? Help & FAQ      │ FIND YOUR ROOM                                         │
│                     │ Search available rooms by city, dates and guests.      │
│                     │                                                        │
│                     │ ╭────────────────────────────────────────────────────╮ │
│                     │ │ City                   Check-in      Check-out     │ │
│                     │ │ [ Vaasa__________ ]    [03.11.26 ▦]  [05.11.26 ▦]  │ │
│                     │ │                                                    │ │
│                     │ │ Guests                                             │ │
│                     │ │ [ 2 guests ▾ ]                [[ ⌕ Search rooms ]] │ │
│                     │ ╰────────────────────────────────────────────────────╯ │
│                     │                                                        │
│                     │ Recommended rooms                       See all rooms ›│
│                     │ ┌───────────────┐ ┌───────────────┐ ┌───────────────┐  │
│                     │ │ ▓▓▓▓▓▓▓▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓▓▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓▓▓▓▓▓▓▓ │  │
│                     │ │ ▓▓▓ photo ▓▓▓ │ │ ▓▓▓ photo ▓▓▓ │ │ ▓▓▓ photo ▓▓▓ │  │
│                     │ │ ▓▓▓▓▓▓▓▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓▓▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓▓▓▓▓▓▓▓ │  │
│                     │ │ Double room   │ │ Suite         │ │ Single room   │  │
│                     │ │ Rantakatu · 2 │ │ Keskusta · 4  │ │ Rantakatu · 1 │  │
│ ─────────────────── │ │ 89 € / night  │ │ 159 € / night │ │ 59 € / night  │  │
│ Browsing as guest   │ └───────────────┘ └───────────────┘ └───────────────┘  │
│ [[ Sign in ]]       │                                                        │
└─────────────────────┴────────────────────────────────────────────────────────┘
```

### C2 – Search results

Filters on the left (price, room type, star rating), results on the right. The search bar stays at the top so the dates can be changed.

```text
┌─ C2 Search results ─┬────────────────────────────────────────────────────────┐
│ ▣ Hotel Reservation │                                                        │
│                     │ [Vaasa    ][03.11.26▦][05.11.26▦][2 ▾] [ Update ]      │
│ ▶ ⌕ Find rooms      │ ────────────────────────────────────────────────────── │
│   ▦ My bookings     │ ┌────────────────┐ 12 rooms · 2 nights                 │
│   ? Help & FAQ      │ │ FILTERS        │ Sort by [ Price: low → high ▾ ]     │
│                     │ │                │ ┌─────────────────────────────────┐ │
│                     │ │ Price / night  │ │ ▓▓▓▓▓▓ Double room              │ │
│                     │ │ [50 €]–[200 €] │ │ ▓photo ★★★★ 4-star hotel        │ │
│                     │ │                │ │ ▓▓▓▓▓▓ Rantakatu · Rm 214       │ │
│                     │ │ Room type      │ │        ≤2 guests · free cancel  │ │
│                     │ │ ☐ Single       │ │ 178 €  89 € × 2 nights          │ │
│                     │ │ ☑ Double       │ │              [[ View room ]]    │ │
│                     │ │ ☑ Suite        │ └─────────────────────────────────┘ │
│                     │ │                │ ┌─────────────────────────────────┐ │
│                     │ │ Hotel stars    │ │ ▓▓▓▓▓▓ Suite                    │ │
│                     │ │ ☐ ★★★          │ │ ▓photo ★★★★ 4-star hotel        │ │
│                     │ │ ☑ ★★★★         │ │ ▓▓▓▓▓▓ Keskusta · Rm 501        │ │
│                     │ │ ☐ ★★★★★        │ │        ≤4 guests · free cancel  │ │
│                     │ │                │ │ 318 €  159 € × 2 nights         │ │
│                     │ │ [ Clear ]      │ │              [[ View room ]]    │ │
│ ─────────────────── │ └────────────────┘ └─────────────────────────────────┘ │
│ Browsing as guest   │                              ‹  (1)  2   3  ›          │
│ [[ Sign in ]]       │                                                        │
└─────────────────────┴────────────────────────────────────────────────────────┘
```

### C3 – Room details

Photos, room facts from the `Rooms` table and a booking summary with live availability. **Book now** goes to sign-in if the user is not signed in.

```text
┌─ C3 Room details ───┬────────────────────────────────────────────────────────┐
│ ▣ Hotel Reservation │                                                        │
│                     │ ‹ Back to results                                      │
│ ▶ ⌕ Find rooms      │                                                        │
│   ▦ My bookings     │ ┌────────────────────────────────────┐ ┌────────────┐  │
│   ? Help & FAQ      │ │ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │ │ ▓▓ bath ▓▓ │  │
│                     │ │ ▓▓▓▓▓▓▓▓▓▓▓▓▓ main photo ▓▓▓▓▓▓▓▓▓ │ │ ▓▓ view ▓▓ │  │
│                     │ │ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓▓▓▓▓ │  │
│                     │ └────────────────────────────────────┘ └────────────┘  │
│                     │                                                        │
│                     │ ★★★★ 4-star hotel           ┏━━━━━━━━━━━━━━━━━━━━━━━━━┓│
│                     │ DOUBLE ROOM                 ┃ 89 € / night            ┃│
│                     │ ⌖ Hotel Rantakatu           ┃                         ┃│
│                     │   Rantakatu 4, 65100 Vaasa  ┃ CHECK-IN    CHECK-OUT   ┃│
│                     │                             ┃ [03.11.26▦] [05.11.26▦] ┃│
│                     │ [Room description from the  ┃ GUESTS [ 2 guests ▾ ]   ┃│
│                     │  database: view, what is    ┃                         ┃│
│                     │  included …]                ┃ ✓ Available             ┃│
│                     │                             ┃                         ┃│
│                     │ ┌──────────┬───────────┐    ┃ 89 € × 2 nights   178 € ┃│
│                     │ │ Type     │ Capacity  │    ┃ ─────────────────────── ┃│
│                     │ ├──────────┼───────────┤    ┃ Total             178 € ┃│
│                     │ │ Double   │ 2 guests  │    ┃                         ┃│
│                     │ └──────────┴───────────┘    ┃ [[     Book now      ]] ┃│
│                     │ ┌──────────┬───────────┐    ┃ Free cancellation 24 h  ┃│
│                     │ │ Room no. │ Per night │    ┃ before check-in.        ┃│
│                     │ ├──────────┼───────────┤    ┗━━━━━━━━━━━━━━━━━━━━━━━━━┛│
│ ─────────────────── │ │ 214      │ 89 €      │                               │
│ Browsing as guest   │ └──────────┴───────────┘                               │
│ [[ Sign in ]]       │                                                        │
└─────────────────────┴────────────────────────────────────────────────────────┘
```

### C4 – Sign in

One sign-in for all roles. The role returned by the API decides the first page. The left panel reminds the customer of the booking in progress.

```text
┌─ C4 Sign in ────────┬────────────────────────────────────────────────────────┐
│ ▣ Hotel Reservation │                                                        │
│                     │       «  Sign in  »   Create account                   │
│                     │                                                        │
│ YOUR STAY IS ONE    │       WELCOME BACK                                     │
│ STEP AWAY.          │                                                        │
│                     │       Email                                            │
│ Staff and admins use│       [ maija.virtanen@example.com___ ]                │
│ the same sign-in.   │       Password                                         │
│                     │       [ ••••••••______________________ ]               │
│ ╭──────────────────╮│                                                        │
│ │ CONTINUING:      ││       ☐ Keep me signed in    Forgot password?          │
│ │ Double room      ││                                                        │
│ │ Rantakatu, Vaasa ││       [[          Sign in            ]]                │
│ │ 03.–05.11. 178 € ││                                                        │
│ ╰──────────────────╯│       ╔═════════════════════════════════╗              │
│                     │       ║ (!) Error state: Wrong email or ║              │
│                     │       ║     password. Never says which. ║              │
│                     │       ╚═════════════════════════════════╝              │
│                     │                                                        │
│                     │       By signing in you accept the                     │
│                     │       privacy policy (GDPR).                           │
│                     │                                                        │
└─────────────────────┴────────────────────────────────────────────────────────┘
```

### C7–C8 – Booking and payment

Step indicator at the top. The guest details from step 1 are shown as a summary; the user only picks a payment method and is redirected to the provider.

```text
┌─ C7–C8 Booking ─────┬────────────────────────────────────────────────────────┐
│ ▣ Hotel Reservation │                                                        │
│                     │ ✓ Guest details ── ② Payment ── ③ Confirmation         │
│ ▶ ⌕ Find rooms      │                                                        │
│   ▦ My bookings     │ CHOOSE HOW TO PAY               ┌───────────────────┐  │
│   ◯ Profile         │                                 │ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │  │
│   ? Help & FAQ      │ ┌─────────────────────────────┐ │ ▓▓▓ room photo ▓▓ │  │
│                     │ │ GUEST               [Edit]  │ │ Double room       │  │
│                     │ │ Maija Virtanen              │ │ Rantakatu · 214   │  │
│                     │ │ maija.virtanen@example.com  │ │                   │  │
│                     │ │ Note: late arrival ~22:00   │ │ In     03.11.2026 │  │
│                     │ └─────────────────────────────┘ │ Out    05.11.2026 │  │
│                     │ Payment method                  │ Guests          2 │  │
│                     │ ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓ │ 89 € × 2    178 € │  │
│                     │ ┃ ◉ Card · Visa, Mastercard   ┃ │ ───────────────── │  │
│                     │ ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛ │ TOTAL       178 € │  │
│                     │ ┌─────────────────────────────┐ │                   │  │
│                     │ │ ○ PayPal                    │ │ Free cancel until │  │
│                     │ └─────────────────────────────┘ │ 02.11. 15:00      │  │
│                     │ ┌─────────────────────────────┐ └───────────────────┘  │
│                     │ │ ○ MobilePay                 │                        │
│                     │ └─────────────────────────────┘                        │
│                     │ ⚿ Redirect to payment provider.                        │
│                     │   No card data in our app.                             │
│                     │   (Mock: success/pending/fail)                         │
│ ─────────────────── │                                                        │
│ (MV) Maija Virtanen │ [ Back ]        [[ Pay 178 € ]]                        │
│      Customer     ⇥ │                                                        │
└─────────────────────┴────────────────────────────────────────────────────────┘
```

### C10 – My bookings

Shown after a successful payment, with a confirmation banner. Cancelling opens a confirmation dialog that shows the cancellation rules.

```text
┌─ C10 My bookings ───┬────────────────────────────────────────────────────────┐
│ ▣ Hotel Reservation │                                                        │
│                     │ ╭────────────────────────────────────────────────────╮ │
│   ⌕ Find rooms      │ │ ✓ Booking confirmed · #HB-10234                    │ │
│ ▶ ▦ My bookings     │ │   Payment received. Confirmation sent to email.    │ │
│   ◯ Profile         │ ╰────────────────────────────────────────────────────╯ │
│   ? Help & FAQ      │                                                        │
│                     │ MY BOOKINGS   «Upcoming (2)»  Past   Cancelled         │
│                     │                                                        │
│                     │ ┌────────────────────────────────────────────────────┐ │
│                     │ │ ▓▓▓▓▓ Double room · Rantakatu    (● Confirmed)     │ │
│                     │ │ ▓▓▓▓▓ 03.11.–05.11.2026 · 2 nights · 2 guests      │ │
│                     │ │ ▓▓▓▓▓ #HB-10234 · Paid 178 € by card               │ │
│                     │ │               [ Details ]  [ Cancel booking ]      │ │
│                     │ └────────────────────────────────────────────────────┘ │
│                     │ ┌────────────────────────────────────────────────────┐ │
│                     │ │ ▓▓▓▓▓ Suite · Keskusta     (◐ Pending payment)     │ │
│                     │ │ ▓▓▓▓▓ 18.12.–20.12.2026 · 2 nights · 3 guests      │ │
│                     │ │ ▓▓▓▓▓ #HB-10251 · 318 € · not paid                 │ │
│                     │ │       [[ Complete payment ]] [ Cancel booking ]    │ │
│                     │ └────────────────────────────────────────────────────┘ │
│                     │                                                        │
│                     │ Confirmation dialog (shown on Cancel):                 │
│                     │ ╔════════════════════════════════════════════╗         │
│                     │ ║ CANCEL BOOKING #HB-10234?                  ║         │
│                     │ ║                                            ║         │
│                     │ ║ You are within the free cancellation       ║         │
│                     │ ║ period: 178 € will be refunded.            ║         │
│                     │ ║ This can't be undone.                      ║         │
│                     │ ║                                            ║         │
│ ─────────────────── │ ║        [ Keep booking ]  [[ Yes, cancel ]] ║         │
│ (MV) Maija Virtanen │ ╚════════════════════════════════════════════╝         │
│      Customer     ⇥ │                                                        │
└─────────────────────┴────────────────────────────────────────────────────────┘
```

---

## Hotel staff views

### S1 – Staff dashboard

```text
┌─ S1 Staff dashboard ┬────────────────────────────────────────────────────────┐
│ ▣ Hotel Reservation │                                                        │
│   ⟨STAFF⟩           │ Tuesday 27.10.2026                                     │
│                     │ GOOD EVENING, JUHA        [[ + New reservation ]]      │
│ ▶ ▤ Dashboard       │                                                        │
│   ▦ Reservations    │ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┏━━━━━━━━━━┓    │
│   ⌂ Rooms           │ │ Check-in │ │ Checkout │ │ Free now │ ┃ Unpaid   ┃    │
│   ◎ Customers       │ │ 8        │ │ 5        │ │ 14       │ ┃ 2  (!)   ┃    │
│                     │ │ 5 done   │ │ all done │ │ of 40    │ ┃ Review › ┃    │
│                     │ └──────────┘ └──────────┘ └──────────┘ ┗━━━━━━━━━━┛    │
│                     │                                                        │
│                     │ Today's arrivals                   All reservations ›  │
│                     │ ┌─────────┬──────────────┬──────────┬──────────────┐   │
│                     │ │ Booking │ Guest        │ Room     │ Status       │   │
│                     │ ├─────────┼──────────────┼──────────┼──────────────┤   │
│                     │ │ #10228  │ A. Mäkinen   │ 108 Sgl  │ ● In         │   │
│                     │ │ #10231  │ S. Korhonen  │ 214 Dbl  │ ● Confirmed  │   │
│ ─────────────────── │ │ #10233  │ M. Nieminen  │ 305 Dbl  │ ◐ Pending    │   │
│ (JL) Juha Laine     │ └─────────┴──────────────┴──────────┴──────────────┘   │
│      Rantakatu    ⇥ │                                                        │
└─────────────────────┴────────────────────────────────────────────────────────┘
```

### S2 – Reservations

```text
┌─ S2 Reservations ───┬────────────────────────────────────────────────────────┐
│ ▣ Hotel Reservation │                                                        │
│   ⟨STAFF⟩           │ RESERVATIONS              [[ + New reservation ]]      │
│                     │                                                        │
│   ▤ Dashboard       │ ┌────────────────────────────────────────────────────┐ │
│ ▶ ▦ Reservations    │ │ [ ⌕ Booking no., guest name or email_______ ]      │ │
│   ⌂ Rooms           │ │ [ All statuses ▾] [ All types ▾] [27.10.–30.11.▦]  │ │
│   ◎ Customers       │ └────────────────────────────────────────────────────┘ │
│                     │ ┌─────────┬───────────┬───────┬───────┬───────────┐    │
│                     │ │ Booking │ Guest     │ Room  │ In    │ Status    │    │
│                     │ ├─────────┼───────────┼───────┼───────┼───────────┤    │
│                     │ │ #10234  │ Virtanen  │ 214 D │ 03.11 │ ● Confirm │    │
│                     │ │ #10233  │ Nieminen  │ 305 D │ 27.10 │ ◐ Pending │    │
│                     │ │ #10231  │ Korhonen  │ 214 D │ 27.10 │ ● Confirm │    │
│                     │ │ #10229  │ Heikkinen │ 501 S │ 01.11 │ ○ Cancel  │    │
│                     │ │ #10228  │ Mäkinen   │ 108 S │ 27.10 │ ● Confirm │    │
│ ─────────────────── │ └─────────┴───────────┴───────┴───────┴───────────┘    │
│ (JL) Juha Laine     │ Each row opens S3 · Showing 1–5 of 48  [‹ Prev][Next ›]│
│      Rantakatu    ⇥ │                                                        │
└─────────────────────┴────────────────────────────────────────────────────────┘
```

### S3 – Edit reservation

The fields match the `Reservations` table. The availability check warns before a double booking is saved.

```text
┌─ S3 Edit booking ───┬────────────────────────────────────────────────────────┐
│ ▣ Hotel Reservation │                                                        │
│   ⟨STAFF⟩           │ ‹ Reservations                                         │
│                     │ RESERVATION #HB-10233   (◐ Pending)                    │
│   ▤ Dashboard       │                                                        │
│ ▶ ▦ Reservations    │ Guest (customer account)        ┌────────────────────┐ │
│   ⌂ Rooms           │ [ ⌕ Mikko Nieminen_________ ]   │ PAYMENT            │ │
│   ◎ Customers       │ Room                            │ 3 × 99 €    297 €  │ │
│                     │ [ 305 · Double · 99 €     ▾ ]   │ ────────────────── │ │
│                     │ Check-in        Check-out       │ Total       297 €  │ │
│                     │ [27.10.26 ▦]    [30.10.26 ▦]    │ Status   Not paid  │ │
│                     │ Guests          Status          │                    │ │
│                     │ [ 2 ] max 2     [ Pending ▾]    │ [[Mark as paid]]   │ │
│                     │                                 └────────────────────┘ │
│                     │ ╔══════════════════════════════╗┌────────────────────┐ │
│                     │ ║ (!) Room 305 is already      ║│ HISTORY            │ │
│                     │ ║     booked on 29.10.         ║│ 26.10 18:42        │ │
│                     │ ║     Shorten the stay or pick ║│   created by guest │ │
│                     │ ║     306 (free all nights).   ║│ 27.10 09:05        │ │
│                     │ ╚══════════════════════════════╝│   note: J. Laine   │ │
│                     │ Notes                           └────────────────────┘ │
│                     │ [ Late arrival, called desk ]                          │
│ ─────────────────── │                                                        │
│ (JL) Juha Laine     │ [Cancel res.] [Discard] [[Save]]                       │
│      Rantakatu    ⇥ │                                                        │
└─────────────────────┴────────────────────────────────────────────────────────┘
```

---

## Administrator views

### A1 – User management

```text
┌─ A1 User management ┬────────────────────────────────────────────────────────┐
│ ▣ Hotel Reservation │                                                        │
│   ⟨ADMIN⟩           │ USERS                   [[ + Create staff account ]]   │
│                     │                                                        │
│ ▶ ◎ Users           │ [ ⌕ Search by name or email_____________________ ]     │
│   ≣ Application logs│ «All 214»  Customers 198   Staff 14   Admins 2         │
│   ◈ Audit trail     │ ┌─────────────┬───────────┬──────────┬────────────────┐│
│                     │ │ Name        │ Role      │ Status   │ Actions        ││
│                     │ ├─────────────┼───────────┼──────────┼────────────────┤│
│                     │ │ M. Virtanen │ Custom. ▾ │ ● Active │ Reset · Lock   ││
│                     │ │ J. Laine    │ Staff   ▾ │ ● Active │ Reset · Lock   ││
│                     │ │ M. Nieminen │ Custom. ▾ │ ○ Locked │ Reset · Unlock ││
│                     │ │ S. Korhonen │ Custom. ▾ │ ● Active │ Reset · Lock   ││
│ ─────────────────── │ └─────────────┴───────────┴──────────┴────────────────┘│
│ (IT) IT Support     │ Role changes and resets go to the audit trail.         │
│      Administrator ⇥│                                                        │
└─────────────────────┴────────────────────────────────────────────────────────┘
```

---

## Mobile (Android / iOS)

On mobile the side menu becomes bottom tabs (MAUI Shell `TabBar`), and forms stack into one column.

```text
┌─ Mobile search ────────────┐    ┌─ Mobile bookings ──────────┐
│                            │    │                            │
│ ▣ Hotel Reservation    [△] │    │ MY BOOKINGS                │
│                            │    │                            │
│ FIND YOUR ROOM             │    │ «Upcoming» Past Cancelled  │
│ ╭────────────────────────╮ │    │                            │
│ │ City                   │ │    │ ┌────────────────────────┐ │
│ │ [ Vaasa_____________ ] │ │    │ │ ▓▓▓▓▓▓▓ room photo ▓▓▓ │ │
│ │ Check-in   Check-out   │ │    │ │ Double room            │ │
│ │ [03.11▦]   [05.11▦]    │ │    │ │ (● Confirmed)          │ │
│ │ Guests                 │ │    │ │ Rantakatu, Vaasa       │ │
│ │ [ 2 guests       ▾ ]   │ │    │ │ 03.–05.11.2026 · 178 € │ │
│ │ [[  Search rooms  ]]   │ │    │ │ [ Details ] [ Cancel ] │ │
│ ╰────────────────────────╯ │    │ └────────────────────────┘ │
│ Recommended                │    │ ┌────────────────────────┐ │
│ ┌────────────────────────┐ │    │ │ Suite                  │ │
│ │ ▓▓▓ Double room        │ │    │ │ (◐ Pending payment)    │ │
│ │ ▓▓▓ Rantakatu · 89 €   │ │    │ │ 18.–20.12.2026 · 318 € │ │
│ └────────────────────────┘ │    │ │ [[Complete payment]]   │ │
│ ┌────────────────────────┐ │    │ └────────────────────────┘ │
│ │ ▓▓▓ Suite              │ │    │                            │
│ │ ▓▓▓ Keskusta · 159 €   │ │    │                            │
│ └────────────────────────┘ │    │                            │
│                            │    │                            │
│                            │    │                            │
│ ────────────────────────── │    │ ────────────────────────── │
│ ⌕«Search»  ▦ Bookings  ◯   │    │ ⌕ Search  ▦«Bookings»  ◯   │
│                            │    │                            │
└────────────────────────────┘    └────────────────────────────┘
```
