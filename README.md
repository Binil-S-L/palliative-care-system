# 🏥 Palliative Care Management System

> A full-stack **ASP.NET Core 8 MVC** web application for managing palliative care patients — built with C#, Entity Framework Core, SQL Server, and Hangfire for background reminders.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-11-239120?style=flat-square&logo=csharp)
![SQL Server](https://img.shields.io/badge/SQL%20Server-EF%20Core-CC2927?style=flat-square&logo=microsoftsqlserver)
![ASP.NET](https://img.shields.io/badge/ASP.NET-MVC-0078D7?style=flat-square)

---

## ✨ Features

| Module | What it does |
|--------|-------------|
| **Dashboard** | Live stats — active patients, today's appointments, pending reminders, charts |
| **Patient Registration** | Full profiles with personal, medical, emergency contact info |
| **Appointment Scheduling** | Calendar view, conflict detection, auto-reminder creation |
| **Medical Records** | Structured clinical notes — vitals, pain scores, treatment plans, medications |
| **Doctor & Staff** | Multi-role team management (Doctor, Nurse, Carer, Social Worker) |
| **Reminders** | Auto-generated appointment reminders + manual custom alerts |
| **Auth & Roles** | ASP.NET Identity — Admin, Doctor, Nurse, Receptionist roles |
| **Background Jobs** | Hangfire — hourly reminder auto-creation, job dashboard at `/hangfire` |

---

## 🗂️ Project Structure

```
PalliativeCare/
│
├── Controllers/
│   └── Controllers.cs          # Home, Patient, Doctor, Appointment, MedicalRecord, Reminder
│
├── Models/
│   └── Models.cs               # Patient, Doctor, Appointment, MedicalRecord, Reminder, AuditLog
│
├── Data/
│   └── ApplicationDbContext.cs # EF Core DbContext, Identity, seed data
│
├── Services/
│   └── Services.cs             # All service interfaces + implementations + DashboardStats
│
├── Views/
│   ├── Home/Index.cshtml       # Dashboard with charts
│   ├── Patient/                # Index, Details (tabbed), Create/Edit
│   ├── Doctor/                 # Index (cards), Create/Edit
│   ├── Appointment/            # Index, Create/Edit
│   ├── MedicalRecord/          # Index, Details, Create
│   ├── Reminder/               # Index, Create
│   └── Shared/_Layout.cshtml   # Sidebar navigation layout
│
├── Program.cs                  # DI setup, middleware, Hangfire, seed
├── appsettings.json
└── PalliativeCare.csproj
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (included with Visual Studio)

### Run Locally

```bash
# 1. Clone the repo
git clone https://github.com/binil-sanil-liby/palliative-care-system
cd palliative-care-system

# 2. Restore packages
dotnet restore

# 3. Apply database migrations (auto-runs on startup too)
dotnet ef database update

# 4. Run the app
dotnet run

# 5. Open in browser
# https://localhost:5001
```

### Default Login
```
Email:    admin@palliativecare.nhs.uk
Password: Admin@1234!
```

---

## 🏗️ Architecture & Design Decisions

### Why ASP.NET Core MVC (not Blazor or Razor Pages)?
- MVC is the most transferable pattern — used across NHS Digital, EMIS, TPP, and major healthcare vendors
- Clean separation of concerns (Models, Views, Controllers)
- Easy to extend with a REST API layer later

### Why SQL Server + EF Core?
- SQL Server is the NHS standard database
- EF Core Code-First migrations make schema changes trackable in Git
- LINQ queries are readable, type-safe, and testable

### Why Hangfire for reminders?
- Persistent background jobs — survive app restarts
- Built-in dashboard for monitoring job history at `/hangfire`
- Hourly job auto-creates reminders for appointments in the next 24 hours

### Domain Model Highlights
- **Patient → Doctor**: Many-to-one (a patient has one assigned doctor)
- **Appointment conflict detection**: Server-side check before saving
- **Log-transform target**: N/A — this is a web app not ML 😄
- **Soft deletion**: Status-based (Active/Inactive/Discharged/Deceased) — data is never hard-deleted
- **Audit trail**: `AuditLog` entity ready to capture who changed what

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8 MVC |
| Language | C# 11 |
| ORM | Entity Framework Core 8 |
| Database | SQL Server (LocalDB for dev) |
| Auth | ASP.NET Identity |
| Background Jobs | Hangfire |
| Logging | Serilog (console + rolling file) |
| Frontend | Bootstrap 5, Chart.js, Font Awesome |

---

## 📸 Key Pages

- **`/`** — Dashboard: stats cards, today's appointments table, reminder list, two Chart.js charts
- **`/Patient`** — Searchable, filterable patient table
- **`/Patient/Details/{id}`** — Tabbed view: Profile, Appointments, Medical Records, Reminders
- **`/Appointment`** — Date-range filtered appointment list with conflict detection
- **`/Doctor`** — Card-based staff directory
- **`/Reminder`** — Alert list with overdue highlighting and one-click mark-sent/dismiss
- **`/hangfire`** — Hangfire background job dashboard

---

## 👤 Author

**Binil Sanil Liby**  
MSc Advanced Computer Science — University of Hertfordshire  
🌐 [Portfolio](https://my-portfolio-binil.netlify.app) · 📧 official.binilsl@gmail.com  
🔗 [LinkedIn](https://linkedin.com/in/binil-sanil-liby) · [GitHub](https://github.com/binil-sanil-liby)
