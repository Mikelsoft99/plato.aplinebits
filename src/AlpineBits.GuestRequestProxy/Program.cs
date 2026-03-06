using AlpineBits.GuestRequestProxy.Options;
using AlpineBits.GuestRequestProxy.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AlpineBitsOptions>(builder.Configuration.GetSection(AlpineBitsOptions.SectionName));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<IAsaClient, AsaClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
