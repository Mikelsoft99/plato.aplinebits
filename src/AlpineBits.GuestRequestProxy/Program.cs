using AlpineBits.GuestRequestProxy.Data;
using AlpineBits.GuestRequestProxy.Data.Repositories;
using AlpineBits.GuestRequestProxy.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddDbContext<AlpineBitsDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddResponseCompression();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddHttpClient<IAsaClient, AsaClient>();
//builder.Services.AddSingleton<IWidgetRequestMapper, WidgetRequestMapper>();
builder.Services.AddScoped<ITenantRepository, TenantRepositoryFake>();
builder.Services.AddScoped<IGuestRequestLogRepository, GuestRequestLogRepositoryFake>();

var app = builder.Build();

//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AlpineBitsDbContext>();
//    db.Database.EnsureCreated();
//}

app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
