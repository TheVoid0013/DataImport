using DataImport.API.Configuration;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.Run();