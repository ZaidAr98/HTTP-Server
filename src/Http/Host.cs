

namespace src.Http
{

    public class Host {
        public  Host(string hostName, int port, string protocol)
        {
        HostName = hostName;
        Port = port;
        Protocol = protocol;
    }

        public string HostName { get; set; }
        public int Port { get; set; }
        public string Protocol { get; set; }


    } }
