using System.Text.Json;

public class SkillMessage
{
    public Dictionary<string, object> message { get; set; }
    public Dictionary<string, object> messageData { get; set; }

    public static string KEY_CREATED_AT = "createdAt";
    public static string KEY_DATA = "data";

    public SkillMessage()
    {
        this.message = new Dictionary<string, object>();
        this.messageData = new Dictionary<string, object>();
        this.message.Add(KEY_CREATED_AT, (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond));
        this.message.Add(KEY_DATA, this.messageData);
    }

    public SkillMessage addData(string key, object value)
    {
        this.messageData.Add(key, value);
        return this;
    }

    public string toJsonString()
    {
        return JsonSerializer.Serialize(this.message);
    }
}