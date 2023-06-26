using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace web_backend
{
    public class Realtime : Hub
    {
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

