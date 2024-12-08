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

        // ���� ������ accept ����� �Ѵ�.
        serverOnNegotiationNeeded = () => { StartCoroutine( PeerNegotiationNeeded(serverPeerConnection)); };

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

        //Debug.Log($"Offer from Server >> \n{desc.sdp}");
        //Debug.Log($"Server >> setLocalDescription start");
        var op = pc.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            //Debug.Log($"Server >> SetLocalDescription complete");
        }
        else
        {
            var error = op.Error;
            Debug.LogError($"Error Detail Type: {error.message}");
            yield break;
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Ŭ���̾�Ʈ ���� ó��

        // ó�� WebRTC �м��Ҷ� . ���� ������ Ŀ������ ��� �κ����� �ñ��ߴµ�. ���Ⱑ �� �κ�
        // ���� �������� ���ϸ� accept�ϰ� Ŭ���̾�Ʈ �����ϴ� �κ�.
        // �ٸ� Ŭ���̾�Ʈ connection ���� �˾ƿ;� �Ѵ�.  �̰��� webRTC������ signaling ��� �Ѵ���.
        //
        // signaling�� webRTC������  ���� �������� �ʴ´�.

        // �켱�� ���� ȣ��  Ŭ���̾�Ʈ ���� 
        string sdpPacket = new string(desc.sdp);
        WRTCClientPeer.Instance.RecvWRTCSetRemoteDescription(sdpPacket);
        
    }
    #endregion


    #region Server-Client Protocol
    // ��Ŷ���� RTCSessionDescription, RTCIceCandidate ����ü �ְ� �޾ƾ� ���� �˼� �ִ�.
    // signaling�� webRTC������  ���� �������� �ʴ´�.

    public void RecvWRTCSetRemoteDescription(string  sdpPacket)
    {
        RTCSessionDescription desc = new RTCSessionDescription();
        desc.sdp = sdpPacket;
        desc.type = RTCSdpType.Answer;

        StartCoroutine(WRTCSetRemoteDescription(desc));
    }

    IEnumerator WRTCSetRemoteDescription(RTCSessionDescription desc)
    {
        //Debug.Log($"Server>> setRemoteDescription start");

        var op2 = serverPeerConnection.SetRemoteDescription(ref desc);
        yield return op2;
        if (!op2.IsError)
        {
            //Debug.Log($"Server>> SetRemoteDescription complete");
        }
        else
        {
            var error = op2.Error;
            Debug.LogError($"Error Detail Type: {error.message}");
        }
    }

    public void RecvWRTCAddIceCandidate(RTCIceCandidate candidate)
    {
        serverPeerConnection.AddIceCandidate(candidate);
        //Debug.Log($"Server >> ICE candidate:\n {candidate.Candidate}");
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



        // ��� connection�� �����ϰ�.  AddIceCandidate ����� �Ѵ�.
        // �ٸ� Ŭ���̾�Ʈ connection ���� �˾ƿ;� �Ѵ�.  �̰��� webRTC������ signaling ��� �Ѵ���.
        //

        // �켱�� ���� ȣ��  Ŭ���̾�Ʈ ���� 
        WRTCClientPeer.Instance.RecvWRTCAddIceCandidate(candidate);
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



    #region �ܺ� UI �������̽�
    public void OnStart(Camera cam)
    {
        if (sendStream == null)
        {
            sendStream = cam.CaptureStream(WebRTCSettings.StreamSize.x, WebRTCSettings.StreamSize.y);
        }
    }

    // ���Ⱑ ��� ����.
    public void OnCall()
    {
        var configuration = WRTCUtil.GetSelectedSdpSemantics();

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
