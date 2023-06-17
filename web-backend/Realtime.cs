using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.IO;

namespace web_backend
{
    public class Realtime : Hub
    {
        public async Task test()
        {
            var i = 0;
            while (true)
            {
                Console.WriteLine("hi");
                await Clients.All.SendAsync("hi", $"hello {i}");
                i++;
                await Task.Delay(1000);
            }
        }



        public async IAsyncEnumerable<int> Counter(
    int count,
    int delay,
    [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            for (var i = 0; i < count; i++)
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



        public async IAsyncEnumerable<string> database(
[EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            // Check the cancellation token regularly so that the server will stop
            // producing items if the client disconnects.
            var a = new BsonArray
{
    new BsonDocument("$match",
    new BsonDocument("hi",
    new BsonDocument("$exists", true)))
};
            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>().Match("{ operationType: { $in: [ 'insert'] } }");
            var docs = Db.Client.GetDatabase("test").GetCollection<BsonDocument>("test").Watch(pipeline, cancellationToken: cancellationToken); //.Watch();

            var enumerator = docs.ToEnumerable(cancellationToken: cancellationToken).GetEnumerator();
            while (enumerator.MoveNext())
            {
                ChangeStreamDocument<BsonDocument> doc = enumerator.Current;
                // Do something here with your document
                
                Console.WriteLine(doc.DocumentKey);
                //var jsonWritersetting = new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson };
                yield return doc.FullDocument.ToJson();
            }
        }


    }

    public class Item
    {
        [BsonId]
        public MongoDB.Bson.ObjectId _id { get; set; }
        [BsonElement("id")]
        public int Id { get; set; }
        [BsonElement("hi")]
        public string Hi { get; set; }

#if false
        [BsonConstructor]
        public Item(string hi)
        {
            Hi = hi;
        } 
#endif
    }


}

