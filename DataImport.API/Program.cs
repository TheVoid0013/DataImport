using DataImport.API.Configuration;
using DataImport.API.Queries; // wherever GetSanctionByIdQuery lives

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<GetSanctionByIdQuery>());
builder.Services.AddFusionCache(); 

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();