using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Unity.WebRTC.Samples;


namespace JWebRTC
{

    public class WRTCServerPeer : MonoBehaviour
    {
        RTCPeerConnection serverPeerConnection;
        List<RTCRtpSender> serverPeerSenders;
        DelegateOnIceCandidate serverOnIceCandidate;
        DelegateOnIceConnectionChange serverOnIceConnectionChange;
        DelegateOnNegotiationNeeded serverOnNegotiationNeeded;
        
        bool videoUpdateStarted;

        //
        string sdpPacketWRTC;
        
        string SdpMidWRTC, CandidateWRTC;
        int SdpMLineIndexWRTC;

        [SerializeField]
        string myKey;

        [SerializeField]
        int myIndex;

        public void SetIndexKey(string key, int index)
        {
            myKey = key;
            myIndex = index;
        }

        private void Awake()
        {
            videoUpdateStarted = false;

            serverPeerSenders = new List<RTCRtpSender>();

            serverOnIceCandidate = candidate => { OnIceCandidate(serverPeerConnection, candidate); };
            serverOnIceConnectionChange = state => { OnIceConnectionChange(serverPeerConnection, state); };

            // ���� ������ accept ����� �Ѵ�.
            serverOnNegotiationNeeded = () => { StartCoroutine(PeerNegotiationNeeded(serverPeerConnection)); };
        }


        #region ��Ʈ��ũ ��������
        // ��Ŷ���� RTCSessionDescription, RTCIceCandidate ����ü �ְ� �޾ƾ� ���� �˼� �ִ�.
        // signaling�� webRTC������  ���� �������� �ʴ´�.

        public void SendWRTCSetRemoteDescription()
        {
            //  11111111111111111111111111111111111111111111111111  ���� ������� sdpPacketWRTC
            if (sdpPacketWRTC != "")
            {
                WRTCCore.Instance.SignalingRTCSendSetRemoteDescriptionFromServer(myKey, myIndex, sdpPacketWRTC);
            }
        }

        public void RecvWRTCSetRemoteDescription(string sdpPacket)
        {
            //  44444444444444444444444444444444444444  >>>>>>>>>>>>>>>>>

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
                Debug.Log($"Server>> SetRemoteDescription complete");
            }
            else
            {
                var error = op2.Error;
                Debug.LogError($"Error Detail Type: {error.message}");
            }
        }


        // ���� AddIceCandidate
        public void RecvWRTCAddIceCandidate(string Candidate, string SdpMid, int SdpMLineIndex)
        {
            RTCIceCandidateInit init = new RTCIceCandidateInit();
            init.candidate = new string(Candidate);
            init.sdpMid = new string(SdpMid);
            init.sdpMLineIndex = SdpMLineIndex;
            
            serverPeerConnection.AddIceCandidate(new RTCIceCandidate(init));

            //
            // ������ Candidate������ ����
            WRTCCore.Instance.SignalingRTCSendCandidateFromServer(myKey, myIndex, CandidateWRTC, SdpMidWRTC, SdpMLineIndexWRTC);
        }

        // ������ �������ų� ���°� �����
        void SendChangedStaus(RTCIceConnectionState state)
        {
            WRTCCore.Instance.ChangeStatusServer_SC(1, state);
        }
        

        #endregion


        #region PeerNegotiationNeeded
        IEnumerator PeerNegotiationNeeded(RTCPeerConnection pc)
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

        IEnumerator OnCreateOfferSuccess(RTCPeerConnection pc, RTCSessionDescription desc)
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

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // Ŭ���̾�Ʈ���� ���� ��û.

            // ó�� WebRTC �м��Ҷ� . ���� ������ Ŀ������ ��� �κ����� �ñ��ߴµ�. ���Ⱑ �� �κ�
            // ���� �������� ���ϸ� accept�ϰ� Ŭ���̾�Ʈ �����ϴ� �κ�.
            // �ٸ� Ŭ���̾�Ʈ connection ���� �˾ƿ;� �Ѵ�.  �̰��� webRTC������ signaling ��� �Ѵ���.
            //
            // signaling�� webRTC������  ���� �������� �ʴ´�.  ������ �ְ� �ޱ� ���� �ٸ� ���� ���� �ʿ���.

            // �߿� ����Ʈ >>>>>>>>>>>
            // ���⼭ sdpPacket�� ������ ���ϴ� Ŭ���̾�Ʈ �鿡��. ������ ��ü �ñ׳θ��� �ϰ�.
            // ���� �ڵ弼��ŷ �۾��� �ϸ�. �Ǵ°���.


            //  11111111111111111111111111111111111111111111111111  >>>>>>>>>>>>>>>>>
            sdpPacketWRTC = new string(desc.sdp);
        }
        #endregion


       
      
        #region OnIceCandidate

        void OnIceCandidate(RTCPeerConnection pc, RTCIceCandidate candidate)
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

            SdpMidWRTC = new string(candidate.SdpMid);
            CandidateWRTC = new string(candidate.Candidate);
            SdpMLineIndexWRTC = (int)candidate.SdpMLineIndex;

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // Ŭ���̾�Ʈ���� ���� ���� ���� �ش�. 

            // ��� connection�� �����ϰ�.  AddIceCandidate ����� �Ѵ�.
            // �ٸ� Ŭ���̾�Ʈ connection ���� �˾ƿ;� �Ѵ�.  �̰��� webRTC������ signaling ��� �Ѵ���.
            //
        }

        #endregion


        #region OnIceConnectionChange

        void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state)
        {
            if (state == RTCIceConnectionState.Connected || state == RTCIceConnectionState.Completed)
            {
                StartCoroutine(CheckStats(pc));
            }
            else
            {
                SendChangedStaus(state);
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
        public void OnCreate(Camera cam, MediaStream sendStream)
        {
            var configuration = WRTCUtil.GetSelectedSdpSemantics();

            serverPeerConnection = new RTCPeerConnection(ref configuration);
            serverPeerConnection.OnIceCandidate = serverOnIceCandidate;
            serverPeerConnection.OnIceConnectionChange = serverOnIceConnectionChange;
            serverPeerConnection.OnNegotiationNeeded = serverOnNegotiationNeeded;

            AddTracks(sendStream);
        }

        public void OnClose()
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

        public void AddTracks(MediaStream sendStream)
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


}