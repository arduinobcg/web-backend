using MongoDB.Driver;

namespace web_backend
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }
    }

#if true
    public static class Db
    {
        private static string? connectionString => Environment.GetEnvironmentVariable("MONGODB_URI");
        public static MongoClient Client
        {
            get
            {
                if (connectionString is null)
                {
                    Console.WriteLine("You must set your 'MONGODB_URI' environmental variable. See\n\t https://www.mongodb.com/docs/drivers/go/current/usage-examples/#environment-variable");
                    Environment.Exit(-1);
                }

                var client = new MongoClient(connectionString);
                return client;
            }
        }
    }
#endif
}
