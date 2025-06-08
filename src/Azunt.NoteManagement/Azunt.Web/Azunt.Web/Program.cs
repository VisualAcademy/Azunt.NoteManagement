using Azunt.NoteManagement;
using Azunt.Web.Client.Pages;
using Azunt.Web.Components;
using Azunt.Web.Components.Account;
using Azunt.Web.Components.Pages.Notes.Services;
using Azunt.Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// HttpClient ���
// HttpClient �ν��Ͻ��� DI(Dependency Injection) �����̳ʿ� ����Ͽ� ���뼺�� ����
builder.Services.AddHttpClient();

// Fluent UI Blazor library add: �ݵ�� AddHttpClient() Ȯ�� �޼��� ������ ��ġ�� ��
builder.Services.AddFluentUIComponents();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddControllers(); // Web API

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();




#region NoteManagement
builder.Services.AddDependencyInjectionContainerForNoteApp(connectionString, Azunt.Models.Enums.RepositoryMode.EfCore);
builder.Services.AddTransient<NoteDbContextFactory>();
builder.Services.AddScoped<INoteStorageService, NoOpNoteStorageService>();
#endregion




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Azunt.Web.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// ������ DB ��� �ʱ�ȭ
NotesTableBuilder.Run(app.Services, forMaster: true);

app.MapControllers(); // Web API

app.Run();
