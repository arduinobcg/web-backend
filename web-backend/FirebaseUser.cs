using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
namespace web_backend
{
    public class FirebaseUser
    {
        [FirestoreProperty("user_id")]
        // [JsonPropertyName("user_id")]
        // [JsonIgnore]
        public string Uid { get; set; } = "";
        public string Email => GetEmail(Uid).Result;

        public async Task<string> GetEmail(string Uid)
        {
            UserRecord d = await FirebaseAuth.DefaultInstance.GetUserAsync(Uid);
            return d.Email;
        }
    }
}
