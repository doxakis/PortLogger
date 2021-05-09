using CommandLine;

namespace PortLogger
{
    public class Config
    {
        [Option("in", HelpText = "Incoming port", Required = true)]
        public int IncomingPort { get; set; }

        [Option("host", HelpText = "Outgoing host", Required = true)]
        public string OutgoingHost { get; set; }
        
        [Option("out", HelpText = "Outgoing port", Required = true)]
        public int OutgoingPort { get; set; }
        
        [Option("destination", HelpText = "Destination folder", Required = true)]
        public string DestinationFolder { get; set; }
    }
}