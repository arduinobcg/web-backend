using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace web_backend
{
    public static class Rabbit
    {
        public static void Rabbit_init()
        {

            ConnectionFactory factory = new ConnectionFactory();
            string? rabbitUri = Environment.GetEnvironmentVariable("RABBIT_URI");
            if (rabbitUri is null)
            {
                Console.WriteLine("You must set your 'RABBIT_URI' environmental variable.");
                Environment.Exit(-1);
            }

            factory.Uri = new Uri(rabbitUri);
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            string? exchange = Environment.GetEnvironmentVariable("RABBIT_EXCHANGE");
            if (exchange is null)
            {
                Console.WriteLine("You must set your 'RABBIT_EXCHANGE' environmental variable.");
                Environment.Exit(-1);
            }
            channel.ExchangeDeclare(exchange:exchange, type: ExchangeType.Topic,durable:true);
            string queueName = channel.QueueDeclare("hi",autoDelete:true,exclusive:false).QueueName;
            Console.WriteLine("queueName: {0}",queueName);
            List<string> bindingKeys = new List<string>(){"d","e","hello.hi"}; //get from db "users" (mqtt_topic)
            foreach (string bindingKey in bindingKeys)
            {
                channel.QueueBind(queue: queueName,
                    exchange: exchange,
                    routingKey: bindingKey);
            }
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);
                string routingKey = ea.RoutingKey;
                Console.WriteLine($" [x] Received '{routingKey}':'{message}'");
            };
            channel.BasicConsume(queue: queueName,
                autoAck: true,
                consumer: consumer);
            while (true) {} // prevent task from exiting
        }
    }
}
