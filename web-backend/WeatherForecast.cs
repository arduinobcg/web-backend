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
    public class Db
    {
        public static string ConnectionString => "mongodb://10.0.0.7:27017/?replicaSet=rs0&directConnection=true";
        public static MongoClient Client
        {
            get
            {

                //var connectionString = Environment.GetEnvironmentVariable("MONGODB_URI");
                if (ConnectionString == null)
                {
                    Console.WriteLine("You must set your 'MONGODB_URI' environmental variable. See\n\t https://www.mongodb.com/docs/drivers/go/current/usage-examples/#environment-variable");
                    Environment.Exit(0);
                }

                var client = new MongoClient(ConnectionString);
                return client;
            }
        }
    }
#endif
}
