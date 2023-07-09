using System.Text;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.AspNetCore.SignalR;

namespace web_backend
{
    public class RabbitHostedService:IHostedService
    {
        private readonly IHubContext<Realtime> _hubContext;
        private readonly Task _completedTask = Task.CompletedTask;
        private readonly ILogger<RabbitHostedService> _logger;
        private readonly Rabbit _rabbit;
        public static CancellationTokenSource canceltokensrc = new CancellationTokenSource();
        public RabbitHostedService(IHubContext<Realtime> hubContext,ILogger<RabbitHostedService> logger,Rabbit rabbit)
        {
            _rabbit = rabbit;
            _hubContext = hubContext;
            _logger = logger;
            //Rabbit_init(canceltokensrc.Token).Start();
            //Task.Run(Rabbit_init,canceltokensrc.Token);
            Console.WriteLine("hihihihi");
        }

        public static EventingBasicConsumer? Consumer { get; set; }

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



        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Service} is running.", nameof(RabbitHostedService));
            //Rabbit_init(canceltokensrc.Token).Start();

            Task.Run(() => _rabbit.Rabbit_init(canceltokensrc.Token), canceltokensrc.Token);
            return _completedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "{Service} is stopping.", nameof(RabbitHostedService));
            canceltokensrc.Cancel();
            return _completedTask;
        }

        // public void refreshrabbit()
        // {
        //     canceltokensrc.Cancel();
        //     canceltokensrc = new CancellationTokenSource();
        //     //Rabbit_init(canceltokensrc.Token).Start();
        //     Task.Run(Rabbit_init, canceltokensrc.Token);
        // }
        // public Task Rabbit_init()
        // {
        //
        //
        //     using var connection = Factory.CreateConnection();
        //     using var channel = connection.CreateModel();
        //     string? exchange = Environment.GetEnvironmentVariable("RABBIT_EXCHANGE");
        //     if (exchange is null)
        //     {
        //         Console.WriteLine("You must set your 'RABBIT_EXCHANGE' environmental variable.");
        //         Environment.Exit(-1);
        //     }
        //
        //     var s =Db.Client.GetDatabase("arduinoBCG").GetCollection<Device>("device").AsQueryable();
        //     var names = s.Select(s => s.QueueName).ToList();
        //     foreach (var name in names)
        //     {
        //         Console.WriteLine($"list: {name}");
        //         channel.ExchangeDeclare(exchange:"amq.topic", type: ExchangeType.Topic,durable:true);
        //
        //         string queueName = channel.QueueDeclare(name,autoDelete:true,exclusive:false).QueueName;
        //         Console.WriteLine("queueName: {0}",queueName);
        //         List<string> bindingKeys = new List<string>(){queueName}; //get from db "users" (mqtt_topic)
        //         foreach (string bindingKey in bindingKeys)
        //         {
        //             channel.QueueBind(queue: queueName,
        //                 exchange: "amq.topic",
        //                 routingKey: bindingKey);
        //         }
        //
        //         Consumer = new EventingBasicConsumer(channel);
        //
        //         Consumer.Received += (model, ea) =>
        //         {
        //             byte[] body = ea.Body.ToArray();
        //             string message = Encoding.UTF8.GetString(body);
        //             string routingKey = ea.RoutingKey;
        //             Console.WriteLine($" [x] Received '{routingKey}':'{message}'");
        //             var realtime = _hubContext.Clients.All.SendAsync("hi",new rabbitMsg() {queueName = routingKey,message=message});
        //         };
        //         channel.BasicConsume(queue: queueName,
        //             autoAck: true,
        //             consumer: Consumer);
        //     }
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

            // while (true)
            // {
            // } // prevent task from exiting
        //}

        public class rabbitMsg
        {
            public string queueName { get; set; }
            public string message { get; set; }
        }
    }
}
