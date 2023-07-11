using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using RabbitMQ.Client;
using System.Security.Claims;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace web_backend
{
    public class Realtime : Hub
    {
        // private readonly Rabbit _rabbit;
        //
        public Realtime(ILogger<Rabbit> logger)
        {
            _logger = logger;
        }

        private readonly ILogger<Rabbit> _logger;

        public async IAsyncEnumerable<int> Counter(
    int count,
    int delay,
    [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            for (int i = 0; i < count; i++)
            {
                // Check the cancellation token regularly so that the server will stop
                // producing items if the client disconnects.
                cancellationToken.ThrowIfCancellationRequested();

                yield return i;

                // Use the cancellationToken in other APIs that accept cancellation
                // tokens so the cancellation can flow down to them.
                await Task.Delay(delay, cancellationToken);
            }
        }

        public async IAsyncEnumerable<string> Database(
        [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            // Check the cancellation token regularly so that the server will stop
            // producing items if the client disconnects.
            //             var a = new BsonArray
            // {
            //     new BsonDocument("$match",
            //     new BsonDocument("hi",
            //     new BsonDocument("$exists", true)))
            // };
            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<Item>>().Match("{ operationType: { $in: [ 'insert'] } }");
            var docs = await Db.Client.GetDatabase("test").GetCollection<Item>("test").WatchAsync(pipeline, cancellationToken: cancellationToken); //.Watch();

            using var enumerator = docs.ToEnumerable(cancellationToken: cancellationToken).GetEnumerator() ;
            while (enumerator.MoveNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                ChangeStreamDocument<Item> doc = enumerator.Current;
                // Do something here with your document

                Console.WriteLine(doc.DocumentKey);
                var jsonWritersetting = new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson };
                yield return doc.FullDocument.ToJson(jsonWritersetting);
            }



        }

        public async Task NewMessage() {
            Console.WriteLine(Context.User.FindFirstValue("user_id"));
            var userid = Context.User.FindFirstValue("user_id");
            await Clients.Client(Context.ConnectionId).SendAsync("messageReceived", Context.User.FindFirstValue("user_id"));
        }



        [Authorize]
        public ChannelReader<RabbitHostedService.rabbitMsg> LiveRabbit(
            [EnumeratorCancellation] CancellationToken cancellationToken,[FromServices]Rabbit rabbit)
        {
            var dev= Db.Client.GetDatabase("arduinoBCG").GetCollection<Device>("device").AsQueryable();
            var userid = Context.User.FindFirstValue("user_id");


            var msgchannel = Channel.CreateUnbounded<RabbitHostedService.rabbitMsg>();
            // _ = msgchannel.Writer.WriteAsync(
            //     new RabbitHostedService.rabbitMsg() { queueName = "hi", message = "h" }, cancellationToken);
            if (rabbit.Consumer is null)
            {
                Console.WriteLine("rabbit is null");
            }
                EventHandler<BasicDeliverEventArgs> handler = null;
                handler= (model, ea) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        rabbit.Consumer.Received -= handler;
                    }
                byte[] body = ea.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);
                string routingKey = ea.RoutingKey;
                bool isException = false;
                try
                {
                    JsonSerializer.Deserialize<JsonNode>(message);
                    _logger.LogInformation($" x[x] Received '{routingKey}':'{message}'",args:JsonSerializer.Deserialize<JsonNode>(message));
                }
                catch (JsonException e)
                {
                    isException = true;
                    _logger.LogWarning($" x[x] Received invalid json '{routingKey}':'{message}'");
                    var f = new JsonObject();
                        f.Add("Error", e.Message);
                        f.Add("Path",e.Path);
                        f.Add("LineNumber",e.LineNumber);
                    //message = f.ToString();
                }


                // Console.WriteLine($" x[x] Received '{routingKey}':'{message}'");
                //a.Add(new msg(){model=model,ea=ea});
                var d = dev.Where(x => x.QueueName == routingKey && x.Owner == userid);
                // Console.WriteLine($"hi {d.ToList()[0]}");

                if (d.ToList().Count == 1 && isException == false)
                {
                    _ = msgchannel.Writer.WriteAsync(
                        new RabbitHostedService.rabbitMsg() { queueName = routingKey, message = JsonSerializer.Deserialize<JsonNode>(message) }, cancellationToken);
                }
                // yield return Encoding.UTF8.GetString(body);
            };
                rabbit.Consumer.Received += handler;
            return msgchannel.Reader;
            // while (true)
            // {
            //
            // } // prevent task from exiting
        }


        }


public class msg
{
    public object? model
    {
        get;
        set;
    }
    public BasicDeliverEventArgs ea
    {
        get;
        set;
    }
}

[BsonIgnoreExtraElements]
    public class Item
    {
        [JsonIgnore]
        [BsonIgnore]
        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("hi")]
        public int Hi { get; set; }
        [BsonElement("date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Date { get; set; }

#if false
        [BsonConstructor]
        public Item(string hi)
        {
            Hi = hi;
        }
#endif
    }


}
