using EmployeeManagementSystem.Services;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// MVC
// =====================================================

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<EmailService>();


// =====================================================
// Razor Pages
// =====================================================

builder.Services.AddRazorPages();


// =====================================================
// Database Connection
// =====================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));


// =====================================================
// ASP.NET CORE IDENTITY
// =====================================================

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();


// =====================================================
// CUSTOM LOGIN PAGE
// =====================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    // If user is not logged in,
    // redirect to our custom login page
    options.LoginPath = "/Auth/Login";

    // If user doesn't have permission
    options.AccessDeniedPath = "/Auth/Login";
});

builder.WebHost.UseUrls("http://0.0.0.0:5236");
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles =
    {
        "Admin",
        "HR",
        "Manager",
        "Employee"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(
                new IdentityRole(role));
        }
    }
}


// =====================================================
// HTTP REQUEST PIPELINE
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


//app.UseHttpsRedirection();//

app.UseStaticFiles();

app.UseRouting();


// =====================================================
// AUTHENTICATION
// =====================================================

app.UseAuthentication();


// =====================================================
// AUTHORIZATION
// =====================================================

app.UseAuthorization();


// =====================================================
// DEFAULT ROUTE
// =====================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}"
);


// =====================================================
// IDENTITY RAZOR PAGES
// =====================================================

app.MapRazorPages();


app.Run();