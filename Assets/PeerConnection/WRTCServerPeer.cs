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
    public DelegateOnNegotiationNeeded serverOnNegotiationNeeded;


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

        // 기존 소켓의 accept 기능을 한다.
        serverOnNegotiationNeeded = () => { StartCoroutine( PeerNegotiationNeeded(serverPeerConnection)); };

    }

    RTCPeerConnection GetOtherClient()
    {
        return WRTCClientPeer.Instance.clientPeerConnection;
    }


    #region PeerNegotiationNeeded
    public IEnumerator PeerNegotiationNeeded(RTCPeerConnection pc)
    {
        var op = pc.CreateOffer();
        yield return op;

        if (!op.IsError)
        {
            if (pc.SignalingState != RTCSignalingState.Stable)
            {
                Debug.LogError($"Server >> signaling state is not stable.");
                yield break;
            }

            yield return StartCoroutine(OnCreateOfferSuccess(pc, op.Desc));
        }
        else
        {
            Debug.LogError($"Error Detail Type: {op.Error.message}");
        }
    }

    public IEnumerator OnCreateOfferSuccess(RTCPeerConnection pc, RTCSessionDescription desc)
    {

        Debug.Log($"Offer from Server >> \n{desc.sdp}");
        Debug.Log($"Server >> setLocalDescription start");
        var op = pc.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            Debug.Log($"Server >> SetLocalDescription complete");
        }
        else
        {
            var error = op.Error;
            Debug.LogError($"Error Detail Type: {error.message}");
            yield break;
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // 클라이언트 응답 처리

        // 처음 WebRTC 분석할때 . 접속 정보나 커낵션이 어느 부분인지 궁금했는데. 여기가 그 부분
        // 기존 소켓으로 비교하면 accept하고 클라이언트 연결하는 부분.
        // 다른 클라이언트 connection 정보 알아와야 한다.  이것을 webRTC에서는 signaling 라고 한다함.
        //
        // signaling을 webRTC에서는  공식 지원하지 않는다.
        var clientPeer = GetOtherClient();

        Debug.Log($"Client>>  setRemoteDescription start");
        var op2 = clientPeer.SetRemoteDescription(ref desc);
        yield return op2;
        if (!op2.IsError)
        {
            Debug.Log($"Client>>  SetRemoteDescription complete");
        }
        else
        {
            var error = op2.Error;
            Debug.LogError($"Error Detail Type: {error.message}");
            yield break;
        }


        //////////////////////////////////////////////////////////////////////////
        //
        // 클라이언트 응답 처리 계속
        Debug.Log($"Client>>  createAnswer start");
        // Since the 'remote' side has no media stream we need
        // to pass in the right constraints in order for it to
        // accept the incoming offer of audio and video.

        var op3 = clientPeer.CreateAnswer();
        yield return op3;
        if (!op3.IsError)
        {
            yield return OnCreateAnswerSuccess(clientPeer, op3.Desc);
        }
        else
        {
            Debug.LogError($"Error Detail Type: {op3.Error.message}");
        }
    }

    
    public IEnumerator OnCreateAnswerSuccess(RTCPeerConnection clientPeer, RTCSessionDescription desc)
    {
        // 클라이언트 응답 처리 계속
        // 파라미터 - RTCPeerConnection이 클라이언트 이다.

        Debug.Log($"Answer from Client >> :\n{desc.sdp}");
        Debug.Log($"Client >>  setLocalDescription start");
        var op = clientPeer.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            Debug.Log($"Client >> SetLocalDescription complete");
        }
        else
        {
            var error = op.Error;
            Debug.LogError($"Error Detail Type: {error.message}");
        }


        ////////////////////////////////////////////////////////////////////////
        // 서버 응답 처리.
        Debug.Log($"Server>> setRemoteDescription start");

        var op2 = serverPeerConnection.SetRemoteDescription(ref desc);
        yield return op2;
        if (!op2.IsError)
        {
            Debug.Log($"Server>> SetRemoteDescription complete");
        }
        else
        {
            var error = op2.Error;
            Debug.LogError($"Error Detail Type: {error.message}");
        }

    }
    #endregion


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



        // 모든 connection이 성공하고.  AddIceCandidate 해줘야 한다.
        // 다른 클라이언트 connection 정보 알아와야 한다.  이것을 webRTC에서는 signaling 라고 한다함.
        //
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

    // 여기가 모든 시작.
    public void OnCall()
    {
        var configuration = GetSelectedSdpSemantics();

        serverPeerConnection = new RTCPeerConnection(ref configuration);
        serverPeerConnection.OnIceCandidate = serverOnIceCandidate;
        serverPeerConnection.OnIceConnectionChange = serverOnIceConnectionChange;
        serverPeerConnection.OnNegotiationNeeded = serverOnNegotiationNeeded;

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
