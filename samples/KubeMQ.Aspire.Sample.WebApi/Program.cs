var builder = WebApplication.CreateBuilder(args);
builder.AddKubeMQClient("messaging");
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
