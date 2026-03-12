using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PalliativeCare.Data;
using PalliativeCare.Services;
using Hangfire;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ───────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/palliativecare-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// ── Hangfire (Background jobs for reminders) ──────────────────────────────────
builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseInMemoryStorage());
builder.Services.AddHangfireServer();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPpsService, PpsService>();
builder.Services.AddScoped<IFamilyPortalService, FamilyPortalService>();

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── Hangfire Dashboard ────────────────────────────────────────────────────────
app.UseHangfireDashboard("/hangfire");

// ── Recurring Jobs ────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();
    RecurringJob.AddOrUpdate(
        "auto-appointment-reminders",
        () => reminderService.AutoCreateAppointmentRemindersAsync(),
        Cron.Hourly
    );
}

// ── Routes ────────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// ── DB Migration & Seed on startup ───────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    // Seed roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Doctor", "Nurse", "Receptionist" })
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    // Seed admin user
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (await userManager.FindByEmailAsync("admin@palliativecare.nhs.uk") == null)
    {
        var admin = new ApplicationUser
        {
            UserName = "admin@palliativecare.nhs.uk",
            Email    = "admin@palliativecare.nhs.uk",
            FullName = "System Administrator",
            EmailConfirmed = true,
        };
        await userManager.CreateAsync(admin, "Admin@1234!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}

app.Run();
