using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Unity.WebRTC.Samples;



public class WRTCServerPeer : MonoBehaviour
{
    public static WRTCServerPeer Instance = null;


    public RTCPeerConnection serverPeerConnection;
    public List<RTCRtpSender> serverPeerSenders;
    public DelegateOnIceCandidate serverOnIceCandidate;
    public DelegateOnIceConnectionChange serverOnIceConnectionChange;


    public MediaStream sendStream;

    private bool videoUpdateStarted;


    public RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };

        return config;
    }


    private void Awake()
    {
        Instance = this;
        videoUpdateStarted = false;
    }

    private void Start()
    {
        serverPeerSenders = new List<RTCRtpSender>();

        serverOnIceCandidate = candidate => { OnIceCandidate(serverPeerConnection, candidate); };
        serverOnIceConnectionChange = state => { OnIceConnectionChange(serverPeerConnection, state); };

    }

    RTCPeerConnection GetOtherClient()
    {
        return PeerConnectionSample.Instance._pc2;
    }

    #region OnIceCandidate

    private void OnIceCandidate(RTCPeerConnection pc, RTCIceCandidate candidate)
    {
        /*
        switch ((ProtocolOption)dropDownProtocol.value)
        {
            case ProtocolOption.Default:
                break;
            case ProtocolOption.UDP:
                if (candidate.Protocol != RTCIceProtocol.Udp)
                    return;
                break;
            case ProtocolOption.TCP:
                if (candidate.Protocol != RTCIceProtocol.Tcp)
                    return;
                break;
        }*/


        // dkf
        GetOtherClient().AddIceCandidate(candidate);

        Debug.Log($"Server>> ICE candidate:\n {candidate.Candidate}");
    }

    #endregion


    #region OnIceConnectionChange

    private void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state)
    {
        Debug.Log($"Server>> IceConnectionState: {state}");

        if (state == RTCIceConnectionState.Connected || state == RTCIceConnectionState.Completed)
        {
            StartCoroutine(CheckStats(pc));
        }
    }

    IEnumerator CheckStats(RTCPeerConnection pc)
    {
        yield return new WaitForSeconds(0.1f);
        if (pc == null)
            yield break;

        var op = pc.GetStats();
        yield return op;
        if (op.IsError)
        {
            Debug.LogErrorFormat("RTCPeerConnection.GetStats failed: {0}", op.Error);
            yield break;
        }

        RTCStatsReport report = op.Value;
        RTCIceCandidatePairStats activeCandidatePairStats = null;
        RTCIceCandidateStats remoteCandidateStats = null;

        foreach (var transportStatus in report.Stats.Values.OfType<RTCTransportStats>())
        {
            if (report.Stats.TryGetValue(transportStatus.selectedCandidatePairId, out var tmp))
            {
                activeCandidatePairStats = tmp as RTCIceCandidatePairStats;
            }
        }

        if (activeCandidatePairStats == null || string.IsNullOrEmpty(activeCandidatePairStats.remoteCandidateId))
        {
            yield break;
        }

        foreach (var iceCandidateStatus in report.Stats.Values.OfType<RTCIceCandidateStats>())
        {
            if (iceCandidateStatus.Id == activeCandidatePairStats.remoteCandidateId)
            {
                remoteCandidateStats = iceCandidateStatus;
            }
        }

        if (remoteCandidateStats == null || string.IsNullOrEmpty(remoteCandidateStats.Id))
        {
            yield break;
        }

        Debug.Log($"Server>> candidate stats Id:{remoteCandidateStats.Id}, Type:{remoteCandidateStats.candidateType}");

        var updateText = TestUI.Instance.localCandidateId;
        updateText.text = remoteCandidateStats.Id;
    }

    #endregion



    #region 외부 UI 인터페이스
    public void OnStart(Camera cam)
    {
        if (sendStream == null)
        {
            sendStream = cam.CaptureStream(WebRTCSettings.StreamSize.x, WebRTCSettings.StreamSize.y);
        }
    }

    public void OnCall()
    {
        var configuration = GetSelectedSdpSemantics();

        serverPeerConnection = new RTCPeerConnection(ref configuration);
        serverPeerConnection.OnIceCandidate = serverOnIceCandidate;
        serverPeerConnection.OnIceConnectionChange = serverOnIceConnectionChange;
        serverPeerConnection.OnNegotiationNeeded = PeerConnectionSample.Instance.serverOnNegotiationNeeded;

        AddTracks();
    }

    public void OnHangUp()
    {
        RemoveTracks();

        serverPeerConnection.Close();
        serverPeerConnection.Dispose();
        serverPeerConnection = null;
    }

    public void OnRestartIce()
    {
        serverPeerConnection.RestartIce();
    }

    public void AddTracks()
    {
        foreach (var track in sendStream.GetTracks())
        {
            serverPeerSenders.Add(serverPeerConnection.AddTrack(track, sendStream));
        }

        if (WebRTCSettings.UseVideoCodec != null)
        {
            var codecs = new[] { WebRTCSettings.UseVideoCodec };
            foreach (var transceiver in serverPeerConnection.GetTransceivers())
            {
                if (serverPeerSenders.Contains(transceiver.Sender))
                {
                    transceiver.SetCodecPreferences(codecs);
                }
            }
        }

        if (!videoUpdateStarted)
        {
            StartCoroutine(WebRTC.Update());
            videoUpdateStarted = true;
        }
    }

    public void RemoveTracks()
    {
        foreach (var sender in serverPeerSenders)
        {
            serverPeerConnection.RemoveTrack(sender);
        }

        serverPeerSenders.Clear();
    }
    #endregion
}
