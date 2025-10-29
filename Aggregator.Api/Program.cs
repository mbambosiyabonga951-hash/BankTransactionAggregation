using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog.Web;
using Aggregator.Infrastructure;
using Aggregator.Application.Services;
using Aggregator.Application.Abstractions;
using Aggregator.Identity;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders(); 
builder.Host.UseNLog();
builder.Services.AddControllers(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var cs = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing DefaultConnection");
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(cs));
builder.Services.AddDbContext<AppIdentityDbContext>(opt => opt.UseSqlServer(cs));
builder.Services.AddIdentityCore<ApplicationUser>(o=>{o.Password.RequireDigit=true;o.Password.RequiredLength=8;o.Password.RequireNonAlphanumeric=false;o.Password.RequireUppercase=false;o.Password.RequireLowercase=true;})
    .AddRoles<IdentityRole>().AddEntityFrameworkStores<AppIdentityDbContext>().AddSignInManager();

builder.Services.AddHostedService<IdentitySeeder>();

builder.Services.AddScoped<ITransactionsRepository, TransactionsRepository>();
builder.Services.AddScoped<IFraudStore, FraudStore>();
builder.Services.AddScoped<ITransactionQueries, TransactionQueries>();

var jwt = builder.Configuration.GetSection("Jwt");
var issuer = jwt["Issuer"] ?? "BankAgg"; 
var audience = jwt["Audience"] ?? "BankAgg.Clients"; 
var key = jwt["Key"] ?? "change_this_dev_only_secret_at_least_32_chars!!";

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
builder.Services.AddAuthentication(o=>{o.DefaultAuthenticateScheme=JwtBearerDefaults.AuthenticationScheme;o.DefaultChallengeScheme=JwtBearerDefaults.AuthenticationScheme;})
    .AddJwtBearer(o=>{o.TokenValidationParameters=new TokenValidationParameters{ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,ValidIssuer=issuer,ValidAudience=audience,IssuerSigningKey=signingKey,ClockSkew=TimeSpan.FromSeconds(30)};});

builder.Services.AddAuthorization(o=>{o.AddPolicy("api.read",p=>p.RequireClaim("scope","api.read"));o.AddPolicy("api.write",p=>p.RequireClaim("scope","api.write"));o.AddPolicy("analyst",p=>p.RequireRole("Analyst","Admin"));o.AddPolicy("admin",p=>p.RequireRole("Admin"));});

var app = builder.Build();
app.UseSwagger(); 
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
