# Production Deployment

**Last reviewed:** 2026-08-03

This document records the current production shape for the UBEMTEM library
admin application.

## Live Services

| Component | Provider | URL |
| --- | --- | --- |
| Admin SPA | Vercel | `https://biblio.ubemtem.org` |
| API | Render | `https://books-lib-yy5q.onrender.com` |
| Database | Render PostgreSQL | Internal Render connection URL |
| DNS | Squarespace | `ubemtem.org` zone |

The public UBEMTEM site continues to be managed separately from this repository.
The admin SPA uses the `biblio` subdomain so it does not require rewrites or
proxy rules in the main site repository.

## Frontend: Vercel

The Vercel project should import this GitHub repository with:

```text
Root Directory: apps/web
Framework Preset: Vite
Build Command: npm run build
Output Directory: dist
Install Command: npm install
```

Required production environment variable:

```bash
VITE_API_BASE_URL=https://books-lib-yy5q.onrender.com
```

After changing `VITE_API_BASE_URL`, redeploy the Vercel project.

## DNS: Squarespace

The `ubemtem.org` DNS zone is managed in Squarespace. The subdomain points to
Vercel with a CNAME record similar to:

```text
Type: CNAME
Host: biblio
Value: cname.vercel-dns.com
```

Use the exact DNS value shown by Vercel if it differs. Do not change the root
or `www` records for `ubemtem.org`; those belong to the public UBEMTEM site.

## Backend: Render Web Service

The current production backend is a manually configured Render Web Service, not
a Render Blueprint deployment. This avoids accidentally recreating resources or
triggering billing prompts during routine updates.

Recommended service settings:

```text
Runtime: Docker
Root Directory: blank / repository root
Dockerfile Path: apps/api/Dockerfile
Docker Build Context Directory: blank / .
Health Check Path: /health/ready
```

Required environment variables:

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
DATABASE_URL=postgresql://...
Cors__AllowedOrigins__0=https://biblio.ubemtem.org
Jwt__SigningKey=replace-with-a-strong-random-secret
Bootstrap__Email=replace-with-admin-email
Bootstrap__Password=replace-with-temporary-first-login-password
```

`DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false` is required on the Render free
instance to avoid Linux `inotify` watcher limits during startup.

## Database: Render PostgreSQL

The API requires PostgreSQL. When using Render PostgreSQL with the API also on
Render, use the database's **Internal Database URL** as `DATABASE_URL`.

The API accepts both:

```text
postgresql://user:password@host:5432/database
```

and Npgsql-style connection strings:

```text
Host=...;Port=5432;Database=...;Username=...;Password=...
```

EF Core migrations run automatically at API startup while
`Database:AutoMigrate` remains enabled.

## First Login

At startup, the API creates the bootstrap user only when that email does not
already exist. Changing `Bootstrap__Password` after the user exists does not
reset the database password.

The first successful login returns a token with `passwordChangeRequired=true`.
The user must change the temporary password before catalog write operations are
allowed. The new password should be different from the current password.

## Smoke Test

After backend deploy:

```text
https://books-lib-yy5q.onrender.com/health/live
https://books-lib-yy5q.onrender.com/health/ready
```

After frontend deploy:

```text
https://biblio.ubemtem.org
```

Then verify:

- login succeeds with the configured bootstrap account or current admin account
- first-login password change succeeds when required
- the book list loads through the production API
- authenticated catalog writes work after password change

## Logs

The API writes logs to standard output through Serilog's console sink. In
Render, log persistence and retention are provided by Render's service log
stream. This repository does not configure a file sink, database sink, or
external log provider.

Request logs intentionally avoid request bodies, query strings,
`Authorization` headers, passwords, and JWT values.

## Optional Blueprint

[render.yaml](../render.yaml) remains as infrastructure-as-code documentation
and can be used to create a Render Blueprint if desired. The active production
deployment, however, is the manually configured Render Web Service described
above.
