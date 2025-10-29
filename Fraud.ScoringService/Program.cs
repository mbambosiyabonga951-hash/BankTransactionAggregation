using Microsoft.AspNetCore.Builder;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI; // Add this using directive

var b=WebApplication.CreateBuilder(args); 
b.Services.AddControllers(); b.Services.AddEndpointsApiExplorer();
b.Services.AddSwaggerGen(); 
var app=b.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
