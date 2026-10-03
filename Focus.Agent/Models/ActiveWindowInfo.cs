namespace Focus.Agent.Models
{
    public class ActiveWindowInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "";
        public string WindowTitle { get; set; } = "";
    }
}