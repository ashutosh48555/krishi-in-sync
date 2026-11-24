# Krishi Sync (कृषि सिंक / ਕ੍ਰਿਸ਼ੀ ਸਿੰਕ)
### Autonomous Precision Microclimate Monitoring & Actuation Platform

[![.NET 10](https://img.shields.io/badge/.NET-10.0_LTS-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# 13](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core-MVC-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/mvc)
[![Entity Framework Core](https://img.shields.io/badge/EF_Core-10.0-512BD4)](https://learn.microsoft.com/ef/core/)
[![OpenAPI / Swagger](https://img.shields.io/badge/OpenAPI-3.0_Swagger-85EA2D?logo=swagger&logoColor=black)](https://krishi-in-sync.onrender.com/swagger)
[![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Render](https://img.shields.io/badge/Render-Live_Production-46E3B7?logo=render&logoColor=black)](https://krishi-in-sync.onrender.com)
[![WCAG 2.1 AA](https://img.shields.io/badge/Accessibility-WCAG_2.1_AA-005A9C)](https://www.w3.org/WAI/WCAG21/quickref/)
[![Tests](https://img.shields.io/badge/Tests-120%2F120_Passing-brightgreen)](#-testing--quality-assurance)

> **Live Production Deployment:** [https://krishi-in-sync.onrender.com](https://krishi-in-sync.onrender.com)  
> **Interactive Swagger API Explorer:** [https://krishi-in-sync.onrender.com/swagger](https://krishi-in-sync.onrender.com/swagger)  
> **Production Healthcheck:** [https://krishi-in-sync.onrender.com/healthz](https://krishi-in-sync.onrender.com/healthz)  
> **GitHub Repository:** [https://github.com/ashutosh48555/krishi-in-sync](https://github.com/ashutosh48555/krishi-in-sync)

---

## 🌾 Overview

**Krishi Sync** is an enterprise-grade agricultural microclimate intelligence and autonomous actuation platform calibrated for the Indo-Gangetic Plains and North Indian agricultural corridor. It bridges live meteorological data with psychrometric microclimate agronomy, enabling farm managers, agronomists, and researchers to:

- Calculate and visualize real-time **Vapor Pressure Deficit (VPD)** to prevent fungal spore germination (Rice Blast, Downy Mildew).
- Autonomously trigger physical microclimate actuators (exhaust fans, high-pressure misting pumps, drip solenoid valves) through an unattended **15-minute background evaluation worker**.
- Ingest live meteorological feeds for northern Indian agricultural zones via the **Open-Meteo REST API**.
- Access full telemetry and automation management via **RESTful APIs** with interactive **Swagger / OpenAPI** documentation.
- Deliver culturally contextual, trilingual accessibility (**English**, **हिन्दी / Hindi**, and **ਪੰਜਾਬੀ / Punjabi**) with zero text leakage and full **WCAG 2.1 AA** compliance.

---

## 📸 Platform Showcase

| View | Dark Theme (Production) | Light Theme |
| :--- | :--- | :--- |
| **3D Interactive Hero** | ![Home Dark](docs/screens/p3b-home-1440-dark.png) | ![Home Light](docs/screens/p3b-home-1440-light.png) |
| **Executive Climate Dashboard** | ![Dashboard Dark](docs/screens/p3b-dash-1440-dark.png) | ![Dashboard Light](docs/screens/p3b-dash-1440-light.png) |
| **Microclimate Zones & VPD Gauges** | ![Zones Dark](docs/screens/p3c-zones-1440-dark.png) | ![Zones Light](docs/screens/p3c-zones-1440-light.png) |
| **Autonomous Rules & Presets** | ![Automation Dark](docs/screens/p3c-automation-1440-dark.png) | ![Automation Light](docs/screens/p3c-automation-1440-light.png) |
| **Interactive Swagger API UI** | ![Swagger UI](docs/screens/swagger-ui.png) | *(OpenAPI 3.0 Specification)* |

---

## 🏗️ System Design & Architecture

Krishi Sync is designed around a multi-tier, decoupled architecture prioritizing operational resilience, high performance, strict security, and offline evaluation capability.

### High-Level Architectural Layers

```mermaid
flowchart TD
    subgraph Client_Tier["Client Presentation Layer"]
        UI_Web["Modern Browser Client (Desktop / Mobile)"]
        Three_Canvas["Three.js Interactive 3D Farm Canvas (Self-Hosted r128)"]
        Charts["Chart.js 4.4 Data Visualizations + Accessible Table Fallbacks"]
        Theme_i18n["Bento Grid UI • Light/Dark Mode • Trilingual Engine (EN / HI / PA)"]
    end

    subgraph Gateway_Tier["API & Middleware Gateway Layer"]
        CSP["Strict Content Security Policy (Zero CDNs, Zero Inline Scripts)"]
        Auth_MW["Cookie Auth + Clerk OAuth Identity Gateway"]
        Lang_MW["Language Negotiation Middleware (Cookie / Query String)"]
        Swagger_MW["Swagger OpenAPI 3.0 Documentation UI (/swagger)"]
    end

    subgraph App_Tier["Application & Domain Service Layer"]
        MVC_Ctrl["ASP.NET Core 10 MVC Controllers"]
        REST_API["RESTful Web API Controllers (/api/zones, /api/rules)"]
        VPD_Engine["Psychrometric VPD Engine (Tetens Saturation Formulations)"]
        Rule_Engine["Autonomous IF-THEN Automation Rule Evaluator"]
        Clock_Service["IST Time Calibration Service (UTC+5:30)"]
    end

    subgraph Background_Tier["Autonomous Background Worker"]
        Hosted_Worker["WeatherPollingBackgroundService (IHostedService)"]
        Periodic_Timer["PeriodicTimer (15-Minute Unattended Polling Interval)"]
        Worker_Scope["Scoped DI Container Resolver (IServiceScopeFactory)"]
    end

    subgraph Data_Tier["Persistence & Data Access Layer"]
        EF_Core["Entity Framework Core 10 (Code-First Migrations)"]
        Postgres_DB[("PostgreSQL Database (Neon / Supabase Connection Pooling)")]
        SQLite_DB[("SQLite Database (Zero-Config Resilient Fallback)")]
    end

    subgraph External_Tier["External Cloud Services"]
        OpenMeteo["Open-Meteo Global Meteorological REST API"]
        Clerk_Cloud["Clerk User & Session Management"]
    end

    UI_Web --> CSP
    CSP --> Auth_MW
    Auth_MW --> Lang_MW
    Lang_MW --> MVC_Ctrl
    Lang_MW --> REST_API
    Lang_MW --> Swagger_MW

    MVC_Ctrl --> VPD_Engine
    MVC_Ctrl --> EF_Core
    REST_API --> Rule_Engine
    REST_API --> EF_Core

    Hosted_Worker --> Periodic_Timer
    Periodic_Timer --> Worker_Scope
    Worker_Scope --> OpenMeteo
    Worker_Scope --> VPD_Engine
    Worker_Scope --> Rule_Engine
    Worker_Scope --> EF_Core

    EF_Core --> Postgres_DB
    EF_Core -. Fallback .-> SQLite_DB
    Auth_MW -. OAuth Sync .-> Clerk_Cloud
```

### Autonomous Telemetry Ingestion & Rule Evaluation Lifecycle

```mermaid
sequenceDiagram
    autonumber
    participant Scheduler as Background Worker (IHostedService)
    participant MetAPI as Open-Meteo REST API
    participant Engine as Psychrometric VPD Engine
    participant DB as EF Core Database Context
    participant Actuator as Farm Actuator Controller
    participant Client as Web / REST Client

    loop Every 15 Minutes Unattended
        Scheduler->>MetAPI: Ingest live weather (Temp, RH, Radiation) for Delhi (28.61° N, 77.20° E)
        MetAPI-->>Scheduler: Return meteorological JSON payload
        Scheduler->>Engine: Calculate microclimate deltas & Vapor Pressure Deficit (VPD)
        Engine-->>Scheduler: Return saturation & actual vapor pressure (kPa)
        Scheduler->>DB: Persist time-series SensorLog entry across all 3 farm zones
        Scheduler->>DB: Fetch active Automation Rules (VPD, Temp, Humidity, Soil Moisture)
        Scheduler->>Engine: Evaluate condition thresholds (e.g., VPD < 0.45 kPa)
        alt Threshold Breached
            Scheduler->>Actuator: Trigger simulated actuator (Turn ON Exhaust Fans / Solenoids)
            Scheduler->>DB: Dispatch ClimateAlert record with Severity & Timestamp
        end
    end
    Client->>DB: Query /api/zones or /Climate/Dashboard
    DB-->>Client: Stream fresh telemetry, actuator states, and 24h diurnal trends
```

---

## 💻 Technology Stack Details

### Backend & Cloud Infrastructure
- **Framework:** ASP.NET Core 10.0 LTS (C# 13, .NET 10 Runtime)
- **Web Server:** Kestrel (asynchronous non-blocking I/O)
- **Object-Relational Mapper (ORM):** Entity Framework Core 10.0 (Code-First migrations, Linq pipelines)
- **Dual-Engine Database Architecture:**
  - **Production:** Cloud PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL` with connection pooling and SSL encryption.
  - **Local / Evaluator:** Embedded SQLite (`agrisync.db`) with Write-Ahead Logging (WAL) for zero-configuration instant runs.
- **Background Job Execution:** Autonomous `IHostedService` (`WeatherPollingBackgroundService`) utilizing `PeriodicTimer` (15-minute cycles) with startup grace intervals.
- **Containerization & Deployment:** Multi-stage `Dockerfile` (SDK 10.0 build → Alpine/Debian ASP.NET 10 runtime), automated Git deployment on Render.

### API & Interoperability
- **RESTful Endpoints:** Strongly-typed controllers (`[ApiController]`, `[Route("api/[controller]")]`) with full CRUD support.
- **Interactive Documentation:** Swashbuckle OpenAPI 3.0 (`/swagger`) with interactive schema testing.
- **External Integration:** Typed `HttpClient` with retry policies for the Open-Meteo Meteorological API.

### Security & Compliance
- **Content Security Policy (CSP):** Strict CSP (`script-src 'self'`, `style-src 'self'`). Zero inline scripts, zero inline styles, and zero third-party CDN runtime dependencies.
- **Authentication & Authorization:** Dual authentication engine:
  - Form-based cookie authentication with pre-seeded demo accounts.
  - Hosted Clerk OAuth integration with cryptographic JWT token verification (`IClerkKeyProvider`).
  - Role-Based Access Control (`[Authorize(Roles = "...")]`, `Permissions.cs`).
- **Protection:** Antiforgery token validation (`[ValidateAntiForgeryToken]`) on state-mutating requests, HTML entity encoding against XSS.

### Frontend, UX & Accessibility
- **CSS Architecture:** Zero heavy frameworks (no Bootstrap/Tailwind runtime bloat); bespoke CSS variables (`tokens.css`), Bento grid layouts, hairline borders, and fluid typography.
- **3D Visualization:** Self-hosted Three.js r128 WebGL interactive farm diorama with ambient daylight/night transitions.
- **Data Charting:** Self-hosted Chart.js 4.4.1 (UMD) rendering diurnal temperature, humidity, and optimal VPD target bands.
- **Accessible Table Fallbacks:** Every visual chart includes an interactive `<button>` that toggles a fully semantic, screen-reader-accessible `<table>` for assistive tech.
- **Accessibility:** 100% WCAG 2.1 AA certified with 0 critical or serious violations verified via `axe-core`.
- **Localization (i18n):** Deeply localized dictionary architecture supporting English, Hindi (हिन्दी), and Punjabi (ਪੰਜਾਬੀ).

---

## 🔬 Mathematical Modeling & Psychrometric Equations

### Vapor Pressure Deficit (VPD in kPa)
Vapor Pressure Deficit determines crop transpiration rates and fungal susceptibility. The mathematical engine implements Tetens' psychrometric formulation:

#### 1. Saturation Vapor Pressure ($VP_{sat}$)
$$VP_{sat} = 0.61078 \times \exp\left(\frac{17.27 \times T}{T + 237.3}\right)$$
*where $T$ is temperature in degrees Celsius (°C).*

#### 2. Actual Vapor Pressure ($VP_{act}$)
$$VP_{act} = VP_{sat} \times \left(\frac{RH}{100.0}\right)$$
*where $RH$ is relative humidity percentage (%).*

#### 3. Vapor Pressure Deficit ($VPD$)
$$VPD = VP_{sat} - VP_{act} = VP_{sat} \times \left(1.0 - \frac{RH}{100.0}\right)$$

### Agronomic Crop Stress Bands

| VPD Range | Microclimate State | Agronomic Implication & Action |
| :--- | :--- | :--- |
| **< 0.40 kPa** | **Critical / High Fungal Risk** | Boundary layer saturated; transpirational shutdown; rapid spore germination (Rice Blast, Downy Mildew). **Action:** Trigger exhaust fans to evacuate moisture. |
| **0.40 – 0.80 kPa** | **Low Transpiration** | Minimal water movement; acceptable for young seedling propagation. |
| **0.80 – 1.25 kPa** | **Optimal Photosynthetic Band** | Stomata open at maximum conductance; peak nutrient transport without moisture stress. Highlighted in emerald green in charts and gauges. |
| **1.25 – 1.60 kPa** | **Moderate Transpiration** | Increased moisture pull; elevated irrigation requirement. |
| **> 1.60 kPa** | **Severe Heat / Water Stress** | Stomata close defensively; wilting risk; cellular desiccation. **Action:** Trigger high-pressure misting pump. |

---

## 📡 RESTful Web API Specification

Krishi Sync exposes standard REST endpoints for programmatic integration:

| Method | Endpoint | Description | Sample Response / Status |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/zones` | Retrieve all zones with latest telemetry & VPD | `200 OK` (JSON array of zone objects) |
| `GET` | `/api/rules` | Retrieve all active automation rules | `200 OK` (JSON array of rule objects) |
| `POST` | `/api/rules` | Create a new microclimate rule | `201 Created` (`Location: /api/rules/{id}`) |
| `PUT` | `/api/rules/{id}` | Update rule threshold or target actuator | `200 OK` (Updated rule object) |
| `DELETE` | `/api/rules/{id}` | Delete an automation rule | `204 No Content` |
| `GET` | `/healthz` | System healthcheck and sensor counts | `200 OK` (`{"zones":3,"rules":4,...}`) |

### Interactive Swagger UI
Explore and execute API calls directly in your browser at:  
👉 **[https://krishi-in-sync.onrender.com/swagger](https://krishi-in-sync.onrender.com/swagger)**

---

## ⚡ Quickstart & Running Locally

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- (Optional) [Docker](https://www.docker.com/) for containerized execution

### 1. Clone the Repository
```bash
git clone https://github.com/ashutosh48555/krishi-in-sync.git
cd krishi-in-sync
```

### 2. Build the Application
```bash
dotnet build -warnaserror
```
*Builds with zero warnings and zero errors.*

### 3. Run Automated Tests
```bash
dotnet test tests/AgriSyncIndia.Tests
```
*Executes all 120 unit, integration, and security tests (100% green).*

### 4. Run the Web Server
```bash
dotnet run
```
The application will launch on `http://localhost:5000` (or `https://localhost:5001`).

### 5. Run with Docker
```bash
# Build the production Docker image
docker build -t krishi-sync .

# Run container mapping port 10000
docker run -p 10000:10000 krishi-sync
```
Navigate to `http://localhost:10000`.

---

## 🔑 Pre-Seeded Evaluation Credentials

For seamless offline grading and evaluation:

| Role | Email Address | Password | Permissions |
| :--- | :--- | :--- | :--- |
| **Farm Operations Manager** | `dr.sharma@agrisync.in` | `AgriSync@123` | Full access: toggle actuators, manage alerts, create/edit/delete rules. |
| **Senior Agronomist** | `priya.verma@agrisync.in` | `AgriSync@123` | Operational access: acknowledge alerts, toggle actuators, create rules. |
| **Demo Quick Login** | `/Account/Login?demo=true` | *(One-Click)* | Prefills manager credentials for immediate evaluation. |

---

## 📁 Repository Directory Structure

```
krishi-in-sync/
├── Controllers/                 # ASP.NET Core MVC & REST API Controllers
│   ├── Api/                     # REST API Controllers (RulesApiController, ZonesApiController)
│   ├── AccountController.cs     # Cookie authentication & Clerk identity handling
│   ├── AlertsController.cs      # Microclimate alert management
│   ├── AutomationController.cs  # Automation rule CRUD & presets
│   ├── ClimateController.cs     # Dashboard, Topology & Historical Analytics
│   └── FarmClusterController.cs # Multi-site cluster management
├── Data/                        # EF Core DbContext, Seed Data & DbInitializer
├── Migrations/                  # Code-First database schema migrations
├── Models/                      # Strongly-typed domain models & entities
├── Properties/                  # Environment launch settings
├── Scripts/                     # SQL Server stored procedures & DDL scripts
├── Services/                    # Domain services, psychrometric math & background workers
│   ├── WeatherPollingBackgroundService.cs # Autonomous 15-min unattended scheduler
│   ├── ClimateRules.cs          # VPD calculation & agronomic thresholds
│   ├── OpenMeteoService.cs      # Weather API ingestion service
│   ├── Localizer.cs             # Multilingual dictionary provider (EN, HI, PA)
│   └── SecurityHeaders.cs       # Strict CSP middleware configuration
├── ViewModels/                  # Strongly-typed presentation view models
├── Views/                       # Razor views (.cshtml) and reusable components
│   ├── Climate/                 # Dashboard, Topology, Analytics views
│   ├── Automation/              # Automation rules & preset controls
│   └── Shared/                  # Master layout, navigation, VPD modal & SVG icons
├── tests/                       # Comprehensive xUnit test suite (120 tests)
├── tools/                       # Quality gate & i18n alignment audit scripts
├── wwwroot/                     # Static assets (self-hosted fonts, Three.js, Chart.js, CSS)
├── Dockerfile                   # Production multi-stage Docker build specification
├── render.yaml                  # Infrastructure-as-Code Render deployment spec
└── README.md                    # Primary project documentation
```

---

## 📄 License & Attribution
- Self-hosted Lucide Icons under ISC License (see `wwwroot/lib/NOTICE.txt`).
- Three.js r128 under MIT License.
- Chart.js 4.4.1 under MIT License.
- Meteorological data courtesy of the Open-Meteo API under CC BY 4.0.
