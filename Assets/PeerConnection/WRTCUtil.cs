using Unity.WebRTC;
using UnityEngine;


namespace JWebRTC
{
    public class WRTCUtil
    {
        enum ProtocolOption
        {
            Default,
            UDP,
            TCP
        }

        static ProtocolOption protocolOption = ProtocolOption.Default;
        ProtocolOption GetProtocolOption() => protocolOption;

        static string[] iceUrls = new[] { "stun:stun.l.google.com:19302" };
        string[] GetIceUrls() => iceUrls;


        public static RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration config = default;
            config.iceServers = new[] { new RTCIceServer { urls = iceUrls } };

            return config;
        }



    }


}