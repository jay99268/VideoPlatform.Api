using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;
using System.Data;
using System.Text;
using VideoPlatform.Api.Middleware;
using VideoPlatform.Api.Services;
using DbType = SqlSugar.DbType;

var builder = WebApplication.CreateBuilder(args);

// 配置 CORS
ConfigureCors(builder.Services, builder.Configuration);

// 配置 SqlSugar
ConfigureSqlSugar(builder.Services, builder.Configuration, builder.Logging);

// 配置 JWT 认证
ConfigureJwtAuthentication(builder.Services, builder.Configuration);

// 其他服务配置
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IEmailService, MockEmailService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 全局异常处理
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowSpecificOrigin");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// 配置 CORS 的方法
void ConfigureCors(IServiceCollection services, IConfiguration configuration)
{
    services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigin", builder =>
        {
            var corsOrigins = configuration["CorsOrigins"];
            if (!string.IsNullOrEmpty(corsOrigins))
            {
                builder.WithOrigins(corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries))
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            }
            else
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }
        });
    });
}

// 配置 SqlSugar 的方法
void ConfigureSqlSugar(IServiceCollection services, IConfiguration configuration, ILoggingBuilder logging)
{
    var connectionString = configuration["ConnectionStrings:MySql"];
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Missing database connection string in configuration.");
    }

    services.AddScoped<ISqlSugarClient>(s =>
    {
        var config = new ConnectionConfig()
        {
            ConnectionString = connectionString,
            DbType = DbType.MySql,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        };
        var db = new SqlSugarClient(config);
        var logger = logging.Services.BuildServiceProvider().GetRequiredService<ILogger<SqlSugarClient>>();
        db.Aop.OnLogExecuting = (sql, pars) => logger.LogDebug("SQL: {Sql}", sql);
        return db;
    });
}

// 配置 JWT 认证的方法
void ConfigureJwtAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var issuer = configuration["Jwt:Issuer"];
    var audience = configuration["Jwt:Audience"];
    var secret = configuration["Jwt:Secret"];

    if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(secret))
    {
        throw new InvalidOperationException("Missing JWT configuration in appsettings.");
    }

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };
        });
}