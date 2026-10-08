# JoFit API: implementation notes

What was built from `BACKEND-BRIEF.md` (284 endpoints), how it differs from the brief, and what to confirm.
All endpoints live under `api/v1/...` (the existing route style) and use `long` ids.

## Decisions that differ from the brief

| Brief | What was built | Why |
|---|---|---|
| Route `/v1/...` | `api/v1/...` | Matches the existing controllers. |
| String ids (`prod-x`, `JF-00001`) | `long` ids; references (`JF-00001`, `RB-`, `CB-`, `EN-`, `QR-`, `SC-`, `JB-`, `JA-`) are derived from the id | Matches the existing entities. |
| Admin cookie holds an opaque session | `jofit.admin` httpOnly cookie holds the staff JWT (12 h) | Reuses the existing JWT setup. CSRF is guarded by an Origin check. |
| Storefront token with a row per session | JWT with a `jti` backed by `AccountSessions`; revoking a row ends the token at once | Same effect: "sign out other devices" works. |
| Permissions `catalogue:write` | `Permissions.Catalogue.Write`; the admin session maps them back to `catalogue:write` | Matches the existing permission system. |
| Errors | `{ error: { code, message, fieldErrors } }` for every error, including 401, 403, 429 | Existing endpoints now answer this too (they used `{ errors }` with 400). |
| Output caching | Blanket 60 s response cache turned off | It showed stale availability and stock. Handlers cache what they need themselves. |

## Assumptions to confirm (the website and admin type files were not available)

- JSON field names and nested shapes are inferred from the brief.
- Course formats `in-person|online|hybrid`; levels `beginner|intermediate|advanced`.
- Rental conditions `new|like-new|good|fair`.
- Quotation subjects: team-kit, gym-equipment, facility, coaching-programme, education, event, other.
- Theme colour keys: the four contrast pairs are required (`primary/onPrimary`, `secondary/onSecondary`, `body/surface`, `title/surface`); other keys are accepted. The default theme is a guess.
- Article tags are plain lowercase slugs on the article (no tag endpoints exist).
- Public product list has no `facets` yet; the job list has them.
- Sitemap entries are `{ slug, lastModified }`.
- Coach working hours are fixed: Sunday to Thursday, 09:00 to 17:00 Amman, one-hour slots (no availability editor).
- Hourly rental pricing keeps the reference behaviour (quantity counted twice). Confirm with the client.
- Delivery fee 3.000 JOD and tax 16% are placeholders.

## Not built / left open (as in the brief)

- Password reset and email verification; email/SMS notifications.
- Staff passwords: a new staff member gets a one-time `temporaryPassword` (or the password sent in the request).
- Careers: employer charging, CV retention period, what verification checks.
- Coach certifications can be verified but there is no endpoint to add them.
- Members cannot post in the community (read and moderation only, as in the brief).
- Ratings on products, rental items and coaches (reviews exist; the aggregate is not shown on those records yet).

## Operational notes

- Uploaded images: `wwwroot/uploads/media`, served by `GET /media/{file}`. EXIF is stripped (JPEG, PNG, WebP). AVIF is stored as uploaded.
- CVs are private: `App_Data/private`, served only through the three CV endpoints. Both folders are in `.gitignore`.
- The app applies migrations and seeds on start (roles, permissions, 12 cities, community topics).
- Rate limit: 10 attempts per minute per address on sign-in, register and password endpoints (429 with `Retry-After`).
- Test accounts created while developing exist in the local database (customer `sara2@test.com`, staff `lina@`, `omar@`, `vic@jofit.test`, coach `coach2@coach.test`, employers `layla@ammanfit.jo`, `omar@rival.jo`). Remove them before a real deployment.
- `ImageSharp` (SixLabors) is used for image processing. Check its license terms for your use.
