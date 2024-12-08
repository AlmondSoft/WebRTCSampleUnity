using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.WebRTC;
using Unity.WebRTC.Samples;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;



class PeerConnectionSample : MonoBehaviour
{
    public static PeerConnectionSample Instance = null;

    enum ProtocolOption
    {
        Default,
        UDP,
        TCP
    }
    
    
    public DelegateOnNegotiationNeeded serverOnNegotiationNeeded;

    //
    public RTCPeerConnection _pc2;
    private MediaStream receiveStream;

    private DelegateOnIceConnectionChange pc2OnIceConnectionChange;
    private DelegateOnIceCandidate pc2OnIceCandidate;
    private DelegateOnTrack pc2Ontrack;


    private bool videoUpdateStarted;

    private void Awake()
    {
        Instance = this;

        receiveStream = new MediaStream();
    }

    private void Start()
    {
        serverOnNegotiationNeeded = () => { StartCoroutine(PeerNegotiationNeeded( WRTCServerPeer.Instance.serverPeerConnection)); };


        pc2OnIceConnectionChange = state => { OnIceConnectionChange(_pc2, state); };
        pc2OnIceCandidate = candidate => { OnIceCandidate(_pc2, candidate); };
        pc2Ontrack = e =>
        {
            receiveStream.AddTrack(e.Track);
        };
      
        receiveStream.OnAddTrack = e =>
        {
            if (e.Track is VideoStreamTrack track)
            {
                track.OnVideoReceived += tex =>
                {
                    TestUI.Instance.SetReceiveImage(tex);
                };
            }
        };
    }

    
    private void Update()
    {
    }

    public static RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };

        return config;
    }

    private void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state)
    {
        Debug.Log($"{GetName(pc)} IceConnectionState: {state}");

        if (state == RTCIceConnectionState.Connected || state == RTCIceConnectionState.Completed)
        {
            StartCoroutine(CheckStats(pc));
        }
    }

    // Display the video codec that is actually used.
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

        Debug.Log($"{GetName(pc)} candidate stats Id:{remoteCandidateStats.Id}, Type:{remoteCandidateStats.candidateType}");
        var updateText = GetName(pc) == "pc1" ? TestUI.Instance.localCandidateId : TestUI.Instance.remoteCandidateId;
        updateText.text = remoteCandidateStats.Id;
    }

    IEnumerator PeerNegotiationNeeded(RTCPeerConnection pc)
    {
        var op = pc.CreateOffer();
        yield return op;

        if (!op.IsError)
        {
            if (pc.SignalingState != RTCSignalingState.Stable)
            {
                Debug.LogError($"{GetName(pc)} signaling state is not stable.");
                yield break;
            }

            yield return StartCoroutine(OnCreateOfferSuccess(pc, op.Desc));
        }
        else
        {
            Debug.LogError($"Error Detail Type: {op.Error.message}");
        }
    }

    public void AddTracks()
    {
        if (!videoUpdateStarted)
        {
            StartCoroutine(WebRTC.Update());
            videoUpdateStarted = true;
        }
    }

    public void RemoveTracks()
    {
        var tracks = receiveStream.GetTracks().ToArray();
        foreach (var track in tracks)
        {
            receiveStream.RemoveTrack(track);
        }
    }


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
        GetOtherPc(pc).AddIceCandidate(candidate);

        Debug.Log($"{GetName(pc)} ICE candidate:\n {candidate.Candidate}");
    }

    private string GetName(RTCPeerConnection pc)
    {
        return (pc == WRTCServerPeer.Instance.serverPeerConnection) ? "pc1" : "pc2";
    }

    private RTCPeerConnection GetOtherPc(RTCPeerConnection pc)
    {
        return (pc == WRTCServerPeer.Instance.serverPeerConnection) ? _pc2 : WRTCServerPeer.Instance.serverPeerConnection;
    }

    private IEnumerator OnCreateOfferSuccess(RTCPeerConnection pc, RTCSessionDescription desc)
    {

        Debug.Log($"Offer from {GetName(pc)}\n{desc.sdp}");
        Debug.Log($"{GetName(pc)} setLocalDescription start");
        var op = pc.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            Debug.Log($"{GetName(pc)} SetLocalDescription complete");
        }
        else
        {
            var error = op.Error;
            Debug.LogError($"Error Detail Type: {error.message}");
            yield break;
        }

        var otherPc = GetOtherPc(pc);
        Debug.Log($"{GetName(otherPc)} setRemoteDescription start");
        var op2 = otherPc.SetRemoteDescription(ref desc);
        yield return op2;
        if (!op2.IsError)
        {
            Debug.Log($"{GetName(otherPc)} SetRemoteDescription complete");
        }
        else
        {
            var error = op2.Error;
            Debug.LogError($"Error Detail Type: {error.message}");
            yield break;
        }

        Debug.Log($"{GetName(otherPc)} createAnswer start");
        // Since the 'remote' side has no media stream we need
        // to pass in the right constraints in order for it to
        // accept the incoming offer of audio and video.

        var op3 = otherPc.CreateAnswer();
        yield return op3;
        if (!op3.IsError)
        {
            yield return OnCreateAnswerSuccess(otherPc, op3.Desc);
        }
        else
        {
            Debug.LogError($"Error Detail Type: {op3.Error.message}");
        }
    }


    IEnumerator OnCreateAnswerSuccess(RTCPeerConnection pc, RTCSessionDescription desc)
    {
        
        Debug.Log($"Answer from {GetName(pc)}:\n{desc.sdp}");
        Debug.Log($"{GetName(pc)} setLocalDescription start");
        var op = pc.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            Debug.Log($"{GetName(pc)} SetLocalDescription complete");
        }
        else
        {
            var error = op.Error;
            Debug.LogError($"Error Detail Type: {error.message}");
        }

        var otherPc = GetOtherPc(pc);
        Debug.Log($"{GetName(otherPc)} setRemoteDescription start");

        var op2 = otherPc.SetRemoteDescription(ref desc);
        yield return op2;
        if (!op2.IsError)
        {
            Debug.Log($"{GetName(otherPc)} SetRemoteDescription complete");
        }
        else
        {
            var error = op2.Error;
            Debug.LogError($"Error Detail Type: {error.message}");
        }
        
    }

    //
    public void OnStart(Camera cam)
    {
    }

    public void OnCall()
    {
        var configuration = PeerConnectionSample.GetSelectedSdpSemantics();

        _pc2 = new RTCPeerConnection(ref configuration);
        _pc2.OnIceCandidate = pc2OnIceCandidate;
        _pc2.OnIceConnectionChange = pc2OnIceConnectionChange;
        _pc2.OnTrack = pc2Ontrack;

        //AddTracks();
        if (!videoUpdateStarted)
        {
            StartCoroutine(WebRTC.Update());
            videoUpdateStarted = true;
        }
    }

    public void OnHangUp()
    {
        RemoveTracks();

        _pc2.Close();
        _pc2.Dispose();
        _pc2 = null;
    }

    public void OnRestartIce()
    {
    }

}
