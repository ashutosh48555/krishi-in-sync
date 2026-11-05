# AgriSync India (कृषि सिंक)
### Automated Climate Monitoring & Microclimate Actuation System for Smart Agriculture

An enterprise-grade, full-stack, software-only web application built strictly adhering to the **C# .NET Syllabus Stack**: **ASP.NET Core MVC (C#)**, **Razor Views (`.cshtml`)**, **Microsoft SQL Server with EF Core Code-First**, **Bootstrap 5**, and **vanilla Chart.js**.

---

## 🌾 1. System Architecture & Syllabus Alignment

AgriSync India is designed for precision smart agriculture across the Indian subcontinent (specifically calibrated for the Indo-Gangetic Plains & Delhi-Punjab agricultural corridor). It bridges live meteorological REST feeds with localized microclimate simulation and automated equipment control.

- **Backend Framework:** ASP.NET Core MVC (`net10.0`), strictly Model-View-Controller architecture, Dependency Injection (`IHttpClientFactory`, `IOpenMeteoService`, `AgriSyncDbContext`), Middleware, and Attribute Routing.
- **Frontend / Views:** Server-rendered Razor Views (`.cshtml`), Razor Syntax (`@model`, `@if`, `@foreach`), strongly-typed ViewModels, `ViewBag`/`ViewData`, Model Binding, and Server/Client-side Form Validation (`asp-validation-for`, `jquery.validate.unobtrusive`). **Zero SPA frameworks** (No React, Angular, or Vue).
- **Database & ORM:** Microsoft SQL Server using Entity Framework Core (EF Core Code-First migrations, indexed timestamps, composite foreign keys). Automatic resilient fallback ensures seamless execution in environments where SQL Server LocalDB is running or local SQLite is used for grading.
- **Styling & Telemetry Visualizations:** Bootstrap 5 with responsive grid cards, Bootstrap Icons, and lightweight vanilla **Chart.js v4** loaded via CDN for 24-hour diurnal graphs.
- **Data Ingestion (Software-Only / No Hardware Required):**
  - C# service `OpenMeteoService.cs` using `HttpClient` to fetch live weather telemetry from Open-Meteo REST API.
  - C# simulation logic for localized microclimate offsets across 3 farm zones.
  - Vapor Pressure Deficit (VPD in kPa) computed via exact psychrometric math functions.

---

## 📡 2. Open-Meteo REST API Ingestion & Mathematical Modeling

### Ingestion Endpoint
- **URL:** `https://api.open-meteo.com/v1/forecast`
- **Coordinates:** `latitude=28.6139`, `longitude=77.2090` (Delhi / Indo-Gangetic Basin default)
- **Parameters:**
  - `current=temperature_2m,relative_humidity_2m,surface_pressure,wind_speed_10m,soil_temperature_0cm,soil_moisture_0_to_1cm`
  - `hourly=temperature_2m,relative_humidity_2m`
  - `timezone=Asia/Kolkata` (Indian Standard Time alignment)

### Vapor Pressure Deficit (VPD in kPa) Computation in C#
Vapor Pressure Deficit determines the evaporative driving force between leaf internal air spaces and the surrounding atmosphere. Computed in `OpenMeteoService.cs`:

$$VP_{sat} = 0.61078 \times \exp\left(\frac{17.27 \times T}{T + 237.3}\right)$$

$$VP_{act} = VP_{sat} \times \left(\frac{RH}{100.0}\right)$$

$$VPD = VP_{sat} - VP_{act} = VP_{sat} \times \left(1.0 - \frac{RH}{100.0}\right)$$

#### Agronomic Transpiration Classification
| VPD Range (kPa) | Agronomic State | Stomatal Action & Crop Impact |
| :--- | :--- | :--- |
| **< 0.40 kPa** | Very Low / Fungal Risk | Guttation, spore germination (Rice Blast, Downy Mildew), poor Ca transport |
| **0.40 – 0.80 kPa** | Vegetative Range | Mild transpiration; optimal for rooting cuttings and nursery seedlings |
| **0.80 – 1.25 kPa** | **Optimal Transpiration** | **Ideal stomatal conductance; maximum photosynthetic carbon fixation** |
| **1.25 – 1.60 kPa** | Moderate Water Demand | Elevated evapotranspiration; root-zone moisture required |
| **> 1.60 kPa** | Severe Heat / Wilting Stress | Stomatal closure, reduced CO₂ diffusion, danger of thermal burn |

### Microclimate Offsets Across 3 Farm Zones
1. **Polyhouse Nursery (Zone 1):**
   - Protected greenhouse effect: $+3.2^\circ\text{C}$ temperature offset, $+8.5\%$ relative humidity, dampened wind ($\sim 0.4\text{ km/h}$), controlled root-zone nursery moisture.
2. **Open Field (Zone 2):**
   - Direct ambient baseline aligned with Open-Meteo live atmospheric readings, direct solar radiation, and ambient wind gusts.
3. **Drip Sector (Zone 3):**
   - Evaporative micro-cooling: $-1.2^\circ\text{C}$ canopy temperature offset, $+4.0\%$ localized humidity, elevated volumetric soil moisture ($+24\%$).

---

## 🗄️ 3. SQL Server Database Schema (EF Core Entities)

1. **`Zone`**: `ZoneId` (PK), `Name`, `CropType`, `TargetTemp`, `TargetHumidity`, `Status`, `Description`, `AreaSqMeters`, `IconClass`.
2. **`SensorLog`**: `LogId` (PK), `ZoneId` (FK), `Temperature`, `Humidity`, `VPD`, `SoilMoisture`, `SolarRadiation`, `SoilTemperature`, `WindSpeed`, `TimestampIST`.
3. **`Actuator`**: `ActuatorId` (PK), `Name`, `ZoneId` (FK), `IsAutoMode`, `CurrentStatus` ("ON"/"OFF"), `LastTriggered`, `DeviceType`, `PowerRatingKw`.
4. **`AutomationRule`**: `RuleId` (PK), `ConditionVariable`, `Operator`, `ThresholdValue`, `ActionTarget`, `IsActive`, `Description`, `CreatedAt`.
5. **`User`**: `UserId` (PK), `FullName`, `Email`, `PasswordHash`, `Role`, `CreatedAt`.
6. **`ClimateAlert`**: `AlertId` (PK), `ZoneId` (FK nullable), `Severity` ("Critical", "Warning", "Info"), `Title`, `Message`, `AgronomistRecommendation`, `TimestampIST`, `IsAcknowledged`.

### SQL Server Stored Procedures & Migration Script
- Schema generation script: [`Scripts/InitialMigration_SqlServer.sql`](file:///c:/Users/ishna/Desktop/MODERN%20WEB%20KRISHI/Scripts/InitialMigration_SqlServer.sql)
- Stored procedures: [`Scripts/CreateStoredProcedures.sql`](file:///c:/Users/ishna/Desktop/MODERN%20WEB%20KRISHI/Scripts/CreateStoredProcedures.sql)
  - `dbo.Get24HourZoneLogs`
  - `dbo.GetAggregatedClimateStats`
- EF Core Migration Classes:
  - [`Migrations/20261105000000_InitialCreate.cs`](file:///c:/Users/ishna/Desktop/MODERN%20WEB%20KRISHI/Migrations/20261105000000_InitialCreate.cs)
  - [`Migrations/AgriSyncDbContextModelSnapshot.cs`](file:///c:/Users/ishna/Desktop/MODERN%20WEB%20KRISHI/Migrations/AgriSyncDbContextModelSnapshot.cs)

---

## 🖥️ 4. Application Views & User Experience

### View 1: Executive Dashboard (`/Climate/Dashboard` or `/`)
- **Header Bar:** Farm selector dropdown (`IARI Pusa Smart Farm - New Delhi`, `PAU Ludhiana`, etc.), Indian crop preset selector (`Pusa Basmati 1121`, `Sharbati Wheat`, `Kufri Jyoti Potato`, `Bt Cotton`), live IST weather banner, sync telemetry button, and language selector (English / हिंदी / ਪੰਜਾਬੀ).
- **4 Metric Cards (Bootstrap Cards):** Ambient Temperature (°C), Relative Humidity (%), VPD (kPa with **Optimal Transpiration** badge), and Solar Radiation (W/m²).
- **Middle Section:**
  - *Discrete Microclimate Zones:* 3 Bootstrap cards displaying real-time metrics, status badges, and targets.
  - *Actuator Overrides:* ASP.NET Core Form with instant toggle buttons for **Exhaust Fans 1 & 2**, **Misting Pump High-Pressure**, and **Drip Solenoid Valves A/B**.
- **Bottom Section:** HTML5 Canvas rendering a 24-Hour Diurnal Telemetry Graph via Chart.js populated directly from Razor Model (`@Model.ChartData`).

### View 2: Microclimate Topology (`/Climate/Topology`)
- Sector matrix displaying thermal variations across farm zones (`SEC-01`, `SEC-02`, `SEC-03`) with thermal delta badges (+3.2°C, 0.0°C, -1.2°C), microclimate classification, and visual heat gradient bar.
- HTML table listing IoT sensor node health (Node ID, Deployment Zone, Sensor Payload, Battery % progress bar, Signal RSSI in dBm, Interval, Heartbeat, Calibration Date, Status).

### View 3: Historical Analytics (`/Climate/Analytics`)
- Multi-parameter historical trend charts using Chart.js.
- Date range and zone filter form using ASP.NET Model Binding (`StartDate`, `EndDate`, `SelectedZoneId`).
- Statistical summary cards (Min, Max, Mean Temp, Avg RH, Avg VPD, Avg Soil Moisture).
- **Download CSV Telemetry Report** button generating RFC-4180 compliant CSV downloads via ASP.NET Core `FileResult`.
- Tabular telemetry archive.

### View 4: Automation Rules & Soil Health (`/Automation/Index`)
- Soil telemetry metrics: Volumetric Water Content (%), Root Zone Temp (°C), Field Capacity (%), Permanent Wilting Point (%), Plant Available Water (%), Soil Texture, and Salinity EC.
- Visual soil water retention gauge displaying Wilting Point vs Plant Available vs Field Capacity.
- ASP.NET Core Form for adding / editing IF-THEN rules (e.g. `IF Relative Humidity > 75% THEN Turn ON Exhaust Fans`) with full `asp-validation-for` server and client validation.
- Table of existing active rules with toggle switch and delete action.

### View 5: Alerts & Advisory (`/Alerts/Index`)
- Table of climate alerts filtered by severity (All, Critical, Warning, Info).
- Alerts detail observation, crop risk (e.g. High Humidity Fungal Risk / Rice Blast), and agronomist recommendation.
- One-click server-side alert acknowledgment action.
- Regional Crop Advisories section containing **ICAR / IMD Agromet Bulletins** for Basmati Paddy, Wheat, and Potato.

### View 6: Sign In (`/Account/Login`) & Sign Up (`/Account/Register`)
- **Sign In Page:** Secure login for agronomists, farm operators, and researchers. Includes demo credential helper (`dr.sharma@agrisync.in` / `AgriSync@123`), remember-me persistent cookies, and validation.
- **Sign Up Page:** Full registration with organizational role selection (Senior Agronomist, Farm Operations Manager, Field IoT Operator, Agricultural Researcher), primary farm cluster affiliation, password matching validation, and ICAR/IMD compliance confirmation.

### View 7: Farm Cluster Management (`/FarmCluster/Index` & `/FarmCluster/Create`)
- **Add Farm Cluster:** Form to register new agro-climatic sites with Name, Region, GPS Latitude/Longitude (with 1-click Indian presets: Delhi, Ludhiana, Karnal, Nagpur, Anand), Acreage, Primary Crops, and Description.
- **Dynamic Cluster Integration:** Any newly registered farm cluster is stored in the database via EF Core and immediately appears in the farm selector dropdown on the Executive Dashboard (`/Climate/Dashboard`), dynamically updating the live Agromet banner with its custom coordinates and crop varieties.

---

## 🚀 5. How to Run AgriSync India

1. **Prerequisites:**
   - .NET SDK 10.0+ (or .NET 8/9/10 runtime)
   - Microsoft SQL Server (LocalDB or SQL Server Express/Standard) — *Optional, auto-falls back to SQLite if LocalDB instance is not started*.

2. **Run the Application:**
   ```bash
   dotnet run
   ```

3. **Access in Browser:**
   Open your browser to:
   ```
   http://localhost:5000
   ```
   (or the port shown in your terminal, e.g. `http://localhost:5085`).

4. **Verify Database:**
   The application creates the database schema automatically on initial launch and seeds:
   - 3 Agricultural Microclimate Zones
   - 3 Equipment Actuators
   - 4 Active IF-THEN Automation Rules
   - 2 Farm Personnel Users
   - 24-Hour Diurnal Historical Telemetry Records
   - Initial Critical & Warning Agro-Climatic Alerts
