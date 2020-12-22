using System.Collections.Generic;
using System.Threading;

namespace Transport.Host
{
    class Program
    {
        private static readonly List<TestPeer> _peers;
        
        static Program()
        {
            _peers = new List<TestPeer>();
        }

        static void Main(string[] args)
        {
            Log.InitForConsole();
            _peers.Add(new TestPeer(true));
            _peers.Add(new TestPeer(false));

            while(true)
            {
                foreach (var peer in _peers)
                {
                    peer.Update();
                }
                Thread.Sleep(15);
            }
        }
    }
}
