using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Google.Cloud.Firestore;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Microsoft.Extensions.Identity.Core;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace web_backend
{
    public class Webapi
    {

        private static readonly IMongoDatabase database = Db.Client.GetDatabase("arduinoBCG");
        [Authorize]
        public static async Task<IResult> AddDevice(HttpRequest req, DeviceAddRequest deviceAddRequest,[FromServices]ILogger<Webapi> logger, [FromServices]Rabbit rabbit,[FromServices]RabbitHostedService rabbithosted)
        {
            if (req.HttpContext.User.FindFirstValue("user_id") is null)
            {
                Results.StatusCode(418);
            }

            Console.WriteLine(deviceAddRequest.ToJson());
            var newguid = Guid.NewGuid();
            var device = new Device()
            {
                Name = deviceAddRequest.Name,
                Guid = newguid,
                QueueName = $"{newguid}.{deviceAddRequest.Name.ToLower().Trim()}",
                Owner = req.HttpContext.User.FindFirstValue("user_id"),
                User = new List<string>(),
                Icon = deviceAddRequest.Icon
            };
            var a = Rabbit.Factory.CreateConnection();
            var v = a.CreateModel();


            var f =v.QueueDeclare(device.QueueName, durable: false,exclusive:false,autoDelete:false,null).QueueName;
            v.QueueBind(queue:f,exchange:"amq.topic",routingKey:device.QueueName,null);
            // rabbit.refreshrabbit();
            rabbithosted.StopAsync(new CancellationToken());
            rabbithosted.StartAsync(new CancellationToken());
            rabbit.refreshrabbit();
            await database.GetCollection<Device>("device").InsertOneAsync(device);
            //Results.StatusCode(405)
            return Results.StatusCode(200);
        }
        [Authorize]
        public static async Task<List<DeviceResponse>> GetDevice(HttpRequest req)
        {
            var user = req.HttpContext.User.FindFirstValue("user_id");

            BsonDocument[] pipelineStage1 =
            {
                new BsonDocument
                {
                    {
                        "$match", new BsonDocument
                        {
                            { "owner", user }
                        }
                    }
                }
            };


            var owneddevice = await database.GetCollection<Device>("device").AggregateAsync<Device>(pipelineStage1);
            return owneddevice.ToList().Select(doc => new DeviceResponse() {Name = doc.Name,QueueName = doc.QueueName,Icon=doc.Icon,Guid=doc.Guid}).ToList();
        }

        public static async Task<DeleteResult> DeleteDevice(HttpRequest req,Guid guid,[FromServices]ILogger<Webapi> logger, [FromServices]Rabbit rabbit)
        {
            var user = req.HttpContext.User.FindFirstValue("user_id");

            var a = Rabbit.Factory.CreateConnection();
            var v = a.CreateModel();
            var device = database.GetCollection<Device>("device").AsQueryable().Where(x => x.Guid == guid).ToList()[0];
            v.QueueUnbind(queue:device.QueueName,exchange:"amq.topic",routingKey:device.QueueName,null);
            v.QueueDelete(queue: device.QueueName, false, false);

            rabbit.refreshrabbit();


            return await database.GetCollection<Device>("device").DeleteOneAsync(Builders<Device>.Filter.And(
                Builders<Device>.Filter.Eq(x => x.Guid,guid),Builders<Device>.Filter.Eq(x => x.Owner,user)));
        }
    }

    public class DeviceAddRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("icon")]
        public Icon Icon { get; set; }
    }
    public class DeviceResponse {
    public string Name { get; set; }
    public Guid Guid { get; set; }
    public string QueueName { get; set; }
    public Icon Icon { get; set; }
    }


    public class Device
    {
        // [JsonIgnore]
        // [BsonIgnore]
        [BsonId]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("guid")]
        public Guid Guid { get; set; }
        [BsonElement("queuename")]
        public string QueueName { get; set; }
        [BsonElement("owner")]
        public string Owner { get; set; }
        [BsonElement("user")]
        public List<string> User { get; set; }
        [BsonElement("icon")]
        public Icon Icon { get; set; }
    }

    public enum Icon
    {
        Aircon,
        Alarm,
        Chip,
        Electric,
        Faucet,
        Home,
        Wifi,
    }
}
