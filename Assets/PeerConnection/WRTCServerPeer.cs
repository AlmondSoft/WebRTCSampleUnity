using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Unity.WebRTC.Samples;



public class WRTCServerPeer : MonoBehaviour
{
    public static WRTCServerPeer Instance = null;




    private void Awake()
    {
        Instance = this;
    }


    public void OnStart(Camera cam)
    {
        if (PeerConnectionSample.Instance.sendStream == null)
        {
            PeerConnectionSample.Instance.sendStream = cam.CaptureStream(WebRTCSettings.StreamSize.x, WebRTCSettings.StreamSize.y);
        }
    }

    public void OnCall()
    {
        var configuration = PeerConnectionSample.GetSelectedSdpSemantics();

        PeerConnectionSample.Instance.serverPeerConnection = new RTCPeerConnection(ref configuration);
        PeerConnectionSample.Instance.serverPeerConnection.OnIceCandidate = PeerConnectionSample.Instance.serverOnIceCandidate;
        PeerConnectionSample.Instance.serverPeerConnection.OnIceConnectionChange = PeerConnectionSample.Instance.serverOnIceConnectionChange;
        PeerConnectionSample.Instance.serverPeerConnection.OnNegotiationNeeded = PeerConnectionSample.Instance.serverOnNegotiationNeeded;

        PeerConnectionSample.Instance.AddTracks();
    }

    public void OnHangUp()
    {
        PeerConnectionSample.Instance.RemoveTracks();

        PeerConnectionSample.Instance.serverPeerConnection.Close();
        PeerConnectionSample.Instance.serverPeerConnection.Dispose();
        PeerConnectionSample.Instance.serverPeerConnection = null;
    }

    public void OnRestartIce()
    {
        PeerConnectionSample.Instance.serverPeerConnection.RestartIce();
    }
}
