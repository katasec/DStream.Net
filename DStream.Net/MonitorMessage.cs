namespace DStream.Net
{
    public class MonitoringMessage
    {
        public string TableName { get; }
        public string EventType { get; }  // e.g., "ChangeDetected", "Error", "PollUpdate"
        public string Message { get; }    // Detailed message or error info

        public MonitoringMessage(string tableName, string eventType, string message)
        {
            TableName = tableName;
            EventType = eventType;
            Message = message;
        }
    }
}
