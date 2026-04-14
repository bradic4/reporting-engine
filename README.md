# Gaming DW - Reporting Engine

A high-performance Marketing Analytics Workbench designed to offer daily reports, KPIs, comparison insights, and comprehensive team tracking, originally designed as an enterprise web application. 

This repository serves as a modernized version containing a cleanly modularized Minimal API backend in **.NET 8** paired with a vanilla Javascript/CSS frontend. The codebase prioritizes software engineering standards, utilizing clean architectural boundaries, robust type safety, comprehensive auditing, and a visually engaging modern design.

## Features

- **Soft Delete & Full Audit Trails**: Action histories (create, update, delete) are comprehensively tracked across all core entities using an `IAuditService`. No data is ever truly lost on deletion.
- **Strictly Typed API Contracts**: Comprehensive request and response Data Transfer Objects (DTOs) replace legacy object/dynamic results.
- **Fluent Validation**: Complete input validation on all create/update endpoints integrated seamlessly into the Minimal API pipeline using a generic validation filter.
- **Modern Centralized JS API**: All frontend communication is handled by a unified API wrapper (`api.js`) that handles errors gracefully via a custom, decoupled custom toast notification system (`toast.js`).
- **Flexible Database Contexts**: Supports both SQLite (for local development/testing) and PostgreSQL (for production/Docker).
- **Pagination & Sorting**: Report lookups respect standard `page`, `pageSize`, `sortBy`, and `sortDesc` parameters avoiding overwhelming payloads.
- **Interactive Dashboards**: Real-time chart visualization for key trends (using Chart.js) alongside target/progress tracking dashboards.
- **Role-based Authentication**: Secure `cookie` authentication natively bundled.

## Getting Started

### Local Setup (SQLite)
1. **Clone the repository**: `git clone <repo>`
2. **Navigate**: `cd "src/GamingDW.WebApp"`
3. **Run Migrations**: `dotnet ef database update`
4. **Boot App**: `dotnet run` 

Default credentials dynamically load (typically `admin` / `admin`).

### Production/Docker (PostgreSQL)
1. **Setup Env**: Copy `.env.example` to `.env` and set your passwords!
2. **Launch Services**: `docker-compose up -d` 
3. The app is automatically available at `http://localhost:8080`. Docker orchestrates the backend app and the postgres instance natively.

## Project Structure
- `Program.cs` - Concise bootstrap script passing responsibility to dedicated infrastructure extension methods.
- `Auth/` - Password hashing and claim injection.
- `Endpoints/` - Encapsulated routing paths logically split by domain (Reports, Target, Live, Audit, etc).
- `Services/` - Core business logic orchestrating db calls, business logic, DTO mapping, and audit logging. 
- `Validation/` - All core validation rules logically segregated from the endpoints. 
- `wwwroot/` - Complete UI client.

## Security Practices Followed
- No hardcoded runtime credentials stored internally anywhere. 
- Custom JWT claims validate API boundaries at runtime via `.RequireAuthorization(policy)`.
- Global error handler ensures exceptions are not leaked to external viewers.
