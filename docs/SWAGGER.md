# Swagger / OpenAPI

**UI:** http://localhost:8000/swagger  
**Spec:** http://localhost:8000/swagger/v1/swagger.json

Configuration lives in `app/src/Configs/SwaggerConfiguration.cs`.

**Package:** `Swashbuckle.AspNetCore` **10.x** (required for .NET 10 — older 6.x produces an empty `paths` object).

**OpenAPI version:** Serialized as **3.0.x** (not 3.0.4) so the bundled Swagger UI can render the spec. If you see *“does not specify a valid version field”*, rebuild the API image after pulling latest `SwaggerConfiguration.cs`.

## What Swagger includes

| Feature | Description |
|---------|-------------|
| **Tags** | One group per HR module via `[ApiExplorerSettings(GroupName = SwaggerGroups.*)]` (tag label only; `DocInclusionPredicate` maps every route into doc `v1`) |
| **Bearer JWT** | Authorize button — paste `Bearer <token>` from Trovesuite |
| **Tenant headers** | `X-Tenant-Id` and `X-Org-Id` on every `/api/v1/*` operation (pre-filled with dev defaults) |
| **Standard errors** | 400, 401, 404, 409, 500 documented on each operation |
| **Schemas** | Request/response DTOs from controllers |
| **XML comments** | Controller summaries from `///` docs (`GenerateDocumentationFile` in `.csproj`) |

## When you add or change an endpoint

1. Implement the controller action.
2. Add the route to `NavigationController` (`NavigationMapResponse`).
3. Set `[ApiExplorerSettings(GroupName = SwaggerGroups.<Module>)]` on the controller (or create a new constant in `SwaggerGroups.cs`).
4. Add `/// <summary>` on the action if the operation name is not obvious.
5. Rebuild and open Swagger — confirm the path, method, body schema, and tags appear.
6. Optionally diff path count:  
   `curl -s localhost:8000/swagger/v1/swagger.json | jq '.paths | keys | length'`

## Try it out (local)

1. Start API: `docker compose up -d api`
2. Open http://localhost:8000/swagger
3. Expand a module (e.g. **Employees**)
4. For a GET: add headers `X-Tenant-Id: demo-tenant`, `X-Org-Id: demo-org` (pre-filled)
5. For JWT mode: click **Authorize**, enter `Bearer <token>`, then call endpoints

Health routes (`/health`) do not require tenant headers.
