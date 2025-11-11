using System.Text.Json;
using AnodyneSharp.Registry;

namespace AnodyneArchipelago.Menu
{
    public class ConnectionDetails(string apServer, string apSlot, string apPassword) : IEquatable<ConnectionDetails>
    {
        public readonly string ApServer = apServer;
        public readonly string ApSlot = apSlot;
        public readonly string ApPassword = apPassword;

        public override bool Equals(object? obj)
        {
            return obj != null && Equals(obj as ConnectionDetails);
        }

        public bool Equals(ConnectionDetails? other)
        {
            return other != null && (ApServer == other.ApServer) && (ApSlot == other.ApSlot) && (ApPassword == other.ApPassword);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ApServer, ApSlot, ApPassword);
        }
    }

    public class ArchipelagoSettings
    {
        public static JsonSerializerOptions serializerOptions = new()
        {
            IncludeFields = true
        };

        public List<ConnectionDetails> ConnectionDetails = [];

        public string PlayerSprite = "young_player";
        public MatchDifferentWorldItem MatchDifferentWorldItem = MatchDifferentWorldItem.MatchExtra;
        public bool HideTrapItems = true;
        public bool ColorPuzzleHelp = true;

        public static string GetFilePath() => string.Format("{0}Saves/ap_settings.dat", GameConstants.SavePath);

        public static ArchipelagoSettings? Load()
        {
            try
            {
                string s = File.ReadAllText(GetFilePath());
                return JsonSerializer.Deserialize<ArchipelagoSettings>(s, serializerOptions);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Save()
        {
            File.WriteAllText(GetFilePath(), JsonSerializer.Serialize(this, serializerOptions));
        }

        public void AddConnection(ConnectionDetails connectionDetails)
        {
            if (ConnectionDetails.Contains(connectionDetails))
            {
                ConnectionDetails.Remove(connectionDetails);
            }

            ConnectionDetails.Insert(0, connectionDetails);

            if (ConnectionDetails.Count > 9)
            {
                ConnectionDetails.RemoveAt(ConnectionDetails.Count - 1);
            }
        }
    }
}
