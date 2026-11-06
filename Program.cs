using Microsoft.EntityFrameworkCore;
using MidtransQrisApi.Data;
using MidtransQrisApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Controller
builder.Services.AddControllers();

// Database SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        )
    )
);

// Midtrans HTTP Client
builder.Services.AddHttpClient<MidtransService>();

var app = builder.Build();

// Buat database untuk development awal
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    db.Database.EnsureCreated();
}

app.MapControllers();

app.Run();