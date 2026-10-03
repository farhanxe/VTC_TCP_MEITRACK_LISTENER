using System;
using System.Threading;

namespace FX_TCP.Class
{
    /// <summary>
    /// Global shared counters accessible across forms.
    /// All fields are manipulated with Interlocked for thread safety.
    /// </summary>
    public static class PublicClass
    {
        // Active TCP connections on port 6062
        public static int ActiveConnection_6062 = 0;

        // Active TCP connections on port 6063 (MeiTrack T711L)
        public static int ActiveConnection_6063 = 0;

        // Active TCP connections on port 6066 (VT200L)
        public static int ActiveConnection_6066 = 0;
    }
}
