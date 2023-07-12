using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.AspNetCore.SignalR;
namespace web_backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            List<X509SecurityKey> issuerSigningKeys = FirebaseAuthConfig.GetIssuerSigningKeys();
            IdentityModelEventSource.ShowPII = true;
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                // options.Events = new JwtBearerEvents()
                // {
                //     // OnAuthenticationFailed = c => Console.WriteLine(c.ToString())
                // }
                //options.Authority = "https://securetoken.google.com/iswork-d8ed0";
                //options.Audience = "iswork-d8ed0";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // TryAllIssuerSigningKeys = true,
                    ValidateIssuer = true,
                    ValidIssuer = "https://securetoken.google.com/arduinobcg",
                    ValidateAudience = true,
                    ValidAudience = "arduinobcg",
                    ValidateLifetime = true,
                    // ValidateIssuerSigningKey = false,
                    IssuerSigningKeys = issuerSigningKeys,
                    IssuerSigningKeyResolver = (arbitrarily, declaring, these, parameters) => issuerSigningKeys
                };
                options.Authority = "https://securetoken.google.com/arduinobcg";

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/test")))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
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

            // var claims = new Dictionary<string, object>()
            // {
            //     { "admin", true },
            // };

            string uid = "IH2mF4O1BkZBqXKOfu2b1z9Anro1"; // bookshorse was here
            // FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, claims).Wait();

            #if true

            string customToken = FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid).Result;
            Console.WriteLine(FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(customToken).ToJson());
            // Send token back to client

            // not work, need token from frontend
            Console.WriteLine($"TestToken: {customToken}");
            #endif

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    policy =>
                    {
                        policy.WithOrigins("https://bcg.sencha.moe", "https://bcgapi.sencha.moe");
                        policy.AllowAnyMethod();
                        policy.AllowAnyHeader();
                    });
            });
            builder.Services.AddSignalR(f => { f.EnableDetailedErrors = true; });


            var f = builder.Services.AddSingleton<Rabbit>();
            builder.Services.AddSingleton<RabbitHostedService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<RabbitHostedService>());
            // f.ConfigureOptions<Rabbit>();

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
            // app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            // {
            //     var forecast = Enumerable.Range(1, 5).Select(index =>
            //         new WeatherForecast
            //         {
            //             Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            //             TemperatureC = Random.Shared.Next(-20, 55),
            //             Summary = summaries[Random.Shared.Next(summaries.Length)]
            //         })
            //         .ToArray();
            //     return forecast;
            // })
            // .WithName("GetWeatherForecast")
            // .WithOpenApi();


            app.MapPost("/AddDevice",Webapi.AddDevice).WithOpenApi();
            app.MapPost("/SendMessage",Webapi.SendMessage).WithOpenApi();
            app.MapGet("/GetDevice",Webapi.GetDevice).WithOpenApi();
            app.MapGet("/GetDeviceHistory",Webapi.GetDeviceHistory).WithOpenApi();

            app.MapDelete("/DeleteDevice",Webapi.DeleteDevice).WithOpenApi();
            app.MapHub<Realtime>("/test");

            // Task.Run(Rabbit.Rabbit_init);
            app.Run();
        }
    }
}
