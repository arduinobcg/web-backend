using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
namespace web_backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            List<X509SecurityKey> issuerSigningKeys = FirebaseAuthConfig.GetIssuerSigningKeys();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                //options.Authority = "https://securetoken.google.com/iswork-d8ed0";
                //options.Audience = "iswork-d8ed0";
                options.TokenValidationParameters = new TokenValidationParameters

                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://securetoken.google.com/arduinobcg",
                    ValidateAudience = true,
                    ValidAudience = "arduinobcg",
                    ValidateLifetime = true,
                    IssuerSigningKeys = issuerSigningKeys,
                    IssuerSigningKeyResolver = (arbitrarily, declaring, these, parameters) => issuerSigningKeys
                };

            });

            // Add services to the container.
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireClaim("Admin", "true"));
            });

            FirebaseApp firebaseApp = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("./arduinobcg-firebase-adminsdk-pk1xy-49a00f4fe8.json"),
            });

            var claims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            String uid = "IH2mF4O1BkZBqXKOfu2b1z9Anro1"; // bookshorse was here
            FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, claims).Wait();

            #if true
            string customToken = FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid).Result;
            // Send token back to client
            Console.WriteLine($"TestToken: {customToken}");
            #endif

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSignalR(f => { f.EnableDetailedErrors = true; });
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    policy =>
                    {
                        policy.AllowAnyOrigin();
                        policy.AllowAnyMethod();
                    });
            });
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();



            BsonClassMap.RegisterClassMap<Item>(cm =>
            {
                cm.AutoMap();
            });
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            var summaries = new[]
            {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
            app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi();


            app.MapPost("/TemperatureData", (Item item) =>
            {
                var docs = Db.Client.GetDatabase("test").GetCollection<BsonDocument>("test");
                docs.InsertOne(item.ToBsonDocument());
            });


            app.MapHub<Realtime>("/test");
            Task.Run(Rabbit.Rabbit_init);
            app.Run();
        }
    }
}
