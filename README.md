# Job Application Tracker API

![CI](https://github.com/cayden10may-ops/job-tracker-api/actions/workflows/ci.yml/badge.svg)

A REST API for tracking job applications, built with C# and ASP.NET Core. Users register, log in, and manage their own companies, applications and notes. Every status change follows enforced workflow rules and is recorded, which powers a stats endpoint.

**Live API docs:** [LIVE-URL](https://job-tracker-api-rc8n.onrender.com/scalar/v1)

> Hosted on a free tier, so the first request after a quiet period can take up to a minute. Demo data resets when the server restarts.

## Features
- JWT authentication with BCrypt password hashing
- Per-user data isolation: users can never see or change each other's data
- Companies, applications and notes (relational data model with EF Core)
- Status workflow rules, with every change stored in a history table
- Filtering, search, sorting and pagination
- Stats endpoint: counts by status, interview rate, offer rate, average days to interview
- Input validation with clear error responses (RFC 7807 problem details)
- Interactive API docs (OpenAPI + Scalar)
- Automated unit and integration tests, run by GitHub Actions on every push

## Status workflow
| From | Allowed next status |
|---|---|
| Applied | Interview, Rejected |
| Interview | Offer, Rejected |
| Offer | Rejected |
| Rejected | none (final) |

## Endpoints
| Method | Route | Description |
|---|---|---|
| POST | `/api/auth/register` | Create an account, returns a JWT |
| POST | `/api/auth/login` | Log in, returns a JWT |
| GET / POST | `/api/companies` | List or create companies |
| GET / PUT / DELETE | `/api/companies/{id}` | Read, update or delete a company |
| GET / POST | `/api/applications` | List (filter, search, sort, page) or create applications |
| GET / PUT / DELETE | `/api/applications/{id}` | Read, update or delete an application |
| PATCH | `/api/applications/{id}/status` | Move an application through the workflow |
| GET | `/api/applications/{id}/history` | Status change history |
| GET / POST | `/api/applications/{id}/notes` | List or add notes |
| DELETE | `/api/applications/{id}/notes/{noteId}` | Delete a note |
| GET | `/api/stats` | Your application statistics |

Example query: `GET /api/applications?status=Interview&search=developer&sort=company&page=1&pageSize=10`

## Tech stack
- C# and ASP.NET Core (.NET 10), controller-based Web API
- Entity Framework Core with SQLite
- JWT bearer authentication, BCrypt
- xUnit and `WebApplicationFactory` for unit and integration tests
- OpenAPI and Scalar for documentation
- GitHub Actions for CI, Docker for deployment

## Design decisions
- **DTOs** keep the API's shape separate from the database entities.
- **Ownership checks on every query.** Asking for another user's data returns 404, so the API doesn't reveal that it exists.
- **Workflow rules live in one small class** (`ApplicationWorkflow`), which makes them easy to unit test.
- **A status history table** keeps statistics accurate, because an application that was rejected after an interview still counts as having reached one.
- **The signing key is never committed.** It comes from user secrets locally and from environment variables in production.

## Running locally
```
git clone https://github.com/cayden10may-ops/job-tracker-api.git
cd job-tracker-api/JobTracker.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "any-random-string-of-at-least-32-characters"
dotnet ef database update
dotnet run
```
Then open the address shown in the terminal with `/scalar/v1` on the end.

## Running the tests
```
cd job-tracker-api
dotnet test
```
The integration tests start the API in memory with a temporary database, so nothing touches real data.

## What I learned
- Designing a relational model and evolving it with EF Core migrations
- Implementing authentication, password hashing and authorization by ownership
- Enforcing business rules in code and proving them with tests
- Writing integration tests against a real HTTP pipeline
- Containerising an API and deploying it with environment-based configuration