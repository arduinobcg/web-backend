using System.Text;
using System.Text.Json.Nodes;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace web_backend
{
    public class Rabbit
    {
        private readonly IHubContext<Realtime> _hubContext;
        private readonly Task _completedTask = Task.CompletedTask;
        private readonly ILogger<RabbitHostedService> _logger;
        private CancellationTokenSource canceltokensrc = RabbitHostedService.canceltokensrc;
        public EventingBasicConsumer Consumer { get; set; }
        public IConnection Connection { get; set; }
        public IModel Channel { get; set; }
        public EventHandler<BasicDeliverEventArgs> Handler { get; set; }
        public Rabbit(IHubContext<Realtime> hubContext,ILogger<RabbitHostedService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
            Connection = Factory.CreateConnection();
            Channel = Connection.CreateModel();
            Consumer = new EventingBasicConsumer(Channel);
            // Task.Run(Rabbit_init);
            Console.WriteLine("rabbit uwu");
        }

        //
        // public static EventingBasicConsumer? Consumer { get; set; }

        public static ConnectionFactory Factory { get
        {
            ConnectionFactory factory = new ConnectionFactory();
            string? rabbitUri = Environment.GetEnvironmentVariable("RABBIT_URI");
            if (rabbitUri is null)
            {
                Console.WriteLine("You must set your 'RABBIT_URI' environmental variable.");
                Environment.Exit(-1);
            }

            factory.Uri = new Uri(rabbitUri);
            return factory;
        }}



                public void refreshrabbit()
                {
                    canceltokensrc.Cancel();
                    canceltokensrc = new CancellationTokenSource();
                    //Rabbit_init(canceltokensrc.Token).Start();
                    Task.Run(() => Rabbit_init(canceltokensrc.Token), canceltokensrc.Token);
                }
                public Task Rabbit_init(CancellationToken canceltoken)
                {
                    Console.WriteLine("rabbit hi");


                    string? exchange = Environment.GetEnvironmentVariable("RABBIT_EXCHANGE");
                    if (exchange is null)
                    {
                        Console.WriteLine("You must set your 'RABBIT_EXCHANGE' environmental variable.");
                        Environment.Exit(-1);
                    }

                    var s =Db.Client.GetDatabase("arduinoBCG").GetCollection<Device>("device").AsQueryable();
                    var names = s.Select(s => s.QueueName).ToList();
                    foreach (var name in names)
                    {
                        Console.WriteLine($"list: {name}");
                        Channel.ExchangeDeclare(exchange:"amq.topic", type: ExchangeType.Topic,durable:true);

                        string queueName = Channel.QueueDeclare(name,autoDelete:false,exclusive:false).QueueName;
                        Console.WriteLine("queueName: {0}",queueName);
                        List<string> bindingKeys = new List<string>(){queueName}; //get from db "users" (mqtt_topic)
                        foreach (string bindingKey in bindingKeys)
                        {
                            Channel.QueueBind(queue: queueName,
                                exchange: "amq.topic",
                                routingKey: bindingKey);
                        }
                        Channel.BasicConsume(queue: queueName,
                            autoAck: true,
                            consumer: Consumer);
                    }
                    //Consumer = new EventingBasicConsumer(Channel);
                    EventHandler<BasicDeliverEventArgs> handler = null;
                    Handler = (model, ea) =>
                    {
                        if (canceltoken.IsCancellationRequested)
                        {
                            Consumer.Received -= handler;
                        }
                        byte[] body = ea.Body.ToArray();
                        string message = Encoding.UTF8.GetString(body);
                        string routingKey = ea.RoutingKey;
                        bool isException = false;
                        try
                        {
                            JsonSerializer.Deserialize<JsonNode>(message);
                        _logger.LogInformation($" [x] Received '{routingKey}':'{message}'",args:JsonSerializer.Deserialize<JsonNode>(message));
                        }
                        catch (JsonException e)
                        {
                            isException = true;
                            _logger.LogWarning($" [x] Received invalid json '{routingKey}':'{message}'");
                            var f = new JsonObject();
                            f.Add("Error", e.Message);
                            f.Add("Path",e.Path);
                            f.Add("LineNumber",e.LineNumber);
                            message = f.ToString();
                        }

                        //_ = _hubContext.Clients.All.SendAsync("hi",new rabbitMsg() {QueueName = routingKey,Message=message});
                        if (!isException)
                        {
                            var d = Db.Client.GetDatabase("arduinoBCG").GetCollection<DeviceTimeline>("timeline")
                                .InsertOneAsync(

                                    new DeviceTimeline()
                                    {
                                        QueueName = routingKey,
                                        Data = BsonSerializer.Deserialize<BsonDocument>(message),
                                        Date = DateTime.Now
                                    }

                        //     // new Device()
                        //     // {
                        //     //
                        //     // }
                        );
                        // Console.WriteLine("hi");\
                        Task.Run(() => d);
                        }

                    };
                    Consumer.Received += Handler;

                    //channel.ExchangeDeclare(exchange:exchange, type: ExchangeType.Topic,durable:true);

                    // string queueName = channel.QueueDeclare("hi",autoDelete:true,exclusive:false).QueueName;
                    // Console.WriteLine("queueName: {0}",queueName);
                    // List<string> bindingKeys = new List<string>(){"d","e","hello.hi"}; //get from db "users" (mqtt_topic)
                    // foreach (string bindingKey in bindingKeys)
                    // {
                    //     channel.QueueBind(queue: queueName,
                    //         exchange: exchange,
                    //         routingKey: bindingKey);
                    // }

                    while (true)
                    {
                    canceltoken.ThrowIfCancellationRequested();
                    } // prevent task from exiting
                }


                //
                // Consumer.Received += (model, ea) =>
                // {
                //     byte[] body = ea.Body.ToArray();
                //     string message = Encoding.UTF8.GetString(body);
                //     string routingKey = ea.RoutingKey;
                //     Console.WriteLine($" [x] Received '{routingKey}':'{message}'");
                //     var realtime = _hubContext.Clients.All.SendAsync("hi",new rabbitMsg() {queueName = routingKey,message=message});
                // };
                // channel.BasicConsume(queue: queueName,
                //     autoAck: true,
                //     consumer: Consumer);


            //channel.ExchangeDeclare(exchange:exchange, type: ExchangeType.Topic,durable:true);

            // string queueName = channel.QueueDeclare("hi",autoDelete:true,exclusive:false).QueueName;
            // Console.WriteLine("queueName: {0}",queueName);
            // List<string> bindingKeys = new List<string>(){"d","e","hello.hi"}; //get from db "users" (mqtt_topic)
            // foreach (string bindingKey in bindingKeys)
            // {
            //     channel.QueueBind(queue: queueName,
            //         exchange: exchange,
            //         routingKey: bindingKey);
            // }
                    public class rabbitMsg
                    {
                        public string QueueName { get; set; }
                        public string Message { get; set; }
                    }

                    public class DeviceTimeline
                    {
                        [BsonId]
                        [JsonIgnore]
                        [BsonElement("_id")]
                        public ObjectId Id { get; set; }
                        [BsonElement("date")]
                        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
                        public DateTime Date { get; set; }
                        [BsonElement("queueName")]
                        public string QueueName { get; set; }
                        [BsonElement("data")]
                        public BsonDocument Data { get; set; }

                    }

    }
}
