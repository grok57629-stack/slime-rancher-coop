using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkExchangeAcceptor : MonoBehaviour
    {
        public int ID;
        public ExchangeAcceptor Acceptor;
        public NetworkRegion Region;
    }
}
