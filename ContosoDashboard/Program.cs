using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddServerSideBlazor();

// Add authentication state provider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Mock Authentication (Cookie-based for training purposes)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("TeamLead", policy => policy.RequireRole("TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("ProjectManager", policy => policy.RequireRole("ProjectManager", "Administrator"));
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<DocumentValidationService>();
builder.Services.AddScoped<DocumentAuthorizationService>();
builder.Services.AddScoped<DocumentAuditService>();
builder.Services.AddScoped<TaskDocumentService>();
builder.Services.AddSingleton<LocalScanQueue>();
builder.Services.AddSingleton<IScanQueue>(services => services.GetRequiredService<LocalScanQueue>());
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IMalwareScanningService, LocalMalwareScanningService>();
builder.Services.AddHostedService<LocalDocumentScanWorker>();

// Add HttpContextAccessor for accessing user claims
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
            EnsureDocumentTables(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

static void EnsureDocumentTables(ApplicationDbContext context)
{
    context.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS Documents (
            DocumentId INTEGER NOT NULL CONSTRAINT PK_Documents PRIMARY KEY AUTOINCREMENT,
            Title TEXT NOT NULL,
            Description TEXT NULL,
            Category TEXT NOT NULL,
            Tags TEXT NULL,
            FileName TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            FileType TEXT NOT NULL,
            FileSize INTEGER NOT NULL,
            Status TEXT NOT NULL,
            UploaderId INTEGER NOT NULL,
            ProjectId INTEGER NULL,
            UploadedDate TEXT NOT NULL,
            ScannedDate TEXT NULL,
            CONSTRAINT FK_Documents_Users_UploaderId FOREIGN KEY (UploaderId) REFERENCES Users (UserId) ON DELETE RESTRICT,
            CONSTRAINT FK_Documents_Projects_ProjectId FOREIGN KEY (ProjectId) REFERENCES Projects (ProjectId) ON DELETE RESTRICT
        );
        CREATE TABLE IF NOT EXISTS DocumentShares (
            DocumentShareId INTEGER NOT NULL CONSTRAINT PK_DocumentShares PRIMARY KEY AUTOINCREMENT,
            DocumentId INTEGER NOT NULL,
            SharedWithUserId INTEGER NULL,
            SharedWithTeam TEXT NULL,
            SharedByUserId INTEGER NOT NULL,
            SharedDate TEXT NOT NULL,
            CONSTRAINT FK_DocumentShares_Documents_DocumentId FOREIGN KEY (DocumentId) REFERENCES Documents (DocumentId) ON DELETE CASCADE
        );
        CREATE TABLE IF NOT EXISTS TaskDocuments (
            TaskDocumentId INTEGER NOT NULL CONSTRAINT PK_TaskDocuments PRIMARY KEY AUTOINCREMENT,
            TaskId INTEGER NOT NULL,
            DocumentId INTEGER NOT NULL,
            AttachedByUserId INTEGER NOT NULL,
            AttachedDate TEXT NOT NULL,
            CONSTRAINT FK_TaskDocuments_Tasks_TaskId FOREIGN KEY (TaskId) REFERENCES Tasks (TaskId) ON DELETE CASCADE,
            CONSTRAINT FK_TaskDocuments_Documents_DocumentId FOREIGN KEY (DocumentId) REFERENCES Documents (DocumentId) ON DELETE CASCADE
        );
        CREATE TABLE IF NOT EXISTS DocumentAuditEvents (
            DocumentAuditEventId INTEGER NOT NULL CONSTRAINT PK_DocumentAuditEvents PRIMARY KEY AUTOINCREMENT,
            DocumentId INTEGER NULL,
            ActorUserId INTEGER NOT NULL,
            Action TEXT NOT NULL,
            Outcome TEXT NOT NULL,
            OccurredDate TEXT NOT NULL,
            Details TEXT NULL,
            CONSTRAINT FK_DocumentAuditEvents_Documents_DocumentId FOREIGN KEY (DocumentId) REFERENCES Documents (DocumentId) ON DELETE SET NULL
        );
        CREATE INDEX IF NOT EXISTS IX_Documents_ProjectId_UploadedDate ON Documents (ProjectId, UploadedDate);
        CREATE INDEX IF NOT EXISTS IX_Documents_Status_Category ON Documents (Status, Category);
        CREATE INDEX IF NOT EXISTS IX_Documents_UploaderId_UploadedDate ON Documents (UploaderId, UploadedDate);
        CREATE INDEX IF NOT EXISTS IX_DocumentShares_DocumentId_SharedWithUserId ON DocumentShares (DocumentId, SharedWithUserId);
        CREATE INDEX IF NOT EXISTS IX_TaskDocuments_DocumentId ON TaskDocuments (DocumentId);
        CREATE UNIQUE INDEX IF NOT EXISTS IX_TaskDocuments_TaskId_DocumentId ON TaskDocuments (TaskId, DocumentId);
        CREATE INDEX IF NOT EXISTS IX_DocumentAuditEvents_DocumentId ON DocumentAuditEvents (DocumentId);");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Use HSTS even in development for training purposes
    app.UseHsts();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy for Blazor Server
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: ws:;";
    
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapControllers();
app.MapFallbackToPage("/_Host");

app.Run();
