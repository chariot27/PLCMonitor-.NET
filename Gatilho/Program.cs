using ModBus.Client;
using ModBus.Interfaces;
using ModBus.MapeamentoDeRegistradores;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Modbus DI
builder.Services.AddSingleton<IModBusClient, ModBusTcpClient>();
builder.Services.AddSingleton<IRegisterMapper, DeltaPlcMapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
