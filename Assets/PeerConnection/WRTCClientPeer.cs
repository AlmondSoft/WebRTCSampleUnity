using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.WebRTC;
using UnityEngine;


namespace JWebRTC
{
    class WRTCClientPeer : MonoBehaviour
    {
        //public static WRTCClientPeer Instance = null;

        //
        public RTCPeerConnection clientPeerConnection;
        private MediaStream receiveStream;

        private DelegateOnIceConnectionChange clientOnIceConnectionChange;
        private DelegateOnIceCandidate clientOnIceCandidate;
        private DelegateOnTrack clientOntrack;

        private bool videoUpdateStarted;

        System.Action<Texture> receiveTextureFunc;

        public void InitReceiveStream(System.Action<Texture>  func)
        {
            receiveTextureFunc = func;

            //
            receiveStream = new MediaStream();

            clientOntrack = e =>
            {
                receiveStream.AddTrack(e.Track);
            };

            receiveStream.OnAddTrack = e =>
            {
                if (e.Track is VideoStreamTrack track)
                {
                    track.OnVideoReceived += tex =>
                    {
                        receiveTextureFunc.Invoke(tex);
                        //TestUI.Instance.SetReceiveImage(tex);
                    };
                }
            };
        }


        private void Awake()
        {
            //Instance = this;
            
        }

        private void Start()
        {
            clientOnIceCandidate = candidate => { OnIceCandidate(clientPeerConnection, candidate); };
            clientOnIceConnectionChange = state => { OnIceConnectionChange(clientPeerConnection, state); };
        }



        #region ��Ʈ��ũ ��������
        // ��Ŷ���� RTCSessionDescription, RTCIceCandidate ����ü �ְ� �޾ƾ� ���� �˼� �ִ�.
        // signaling�� webRTC������  ���� �������� �ʴ´�.


        public void RecvWRTCSetRemoteDescription(string sdpPacket)
        {
            //  2222222222222222222222222222222222222  >>>>>>>>>>>>>>>>>

            // GINO
            // ��Ŷ ����� �ϱ� ���ؼ�
            RTCSessionDescription desc = new RTCSessionDescription();
            desc.sdp = sdpPacket;
            desc.type = RTCSdpType.Offer;

            StartCoroutine(WRTCSetRemoteDescription(desc));
        }

        IEnumerator WRTCSetRemoteDescription(RTCSessionDescription desc)
        {
            //Debug.Log($"Client>>  setRemoteDescription start");

            var op2 = clientPeerConnection.SetRemoteDescription(ref desc);
            yield return op2;
            if (!op2.IsError)
            {
                //Debug.Log($"Client>>  SetRemoteDescription complete");
            }
            else
            {
                var error = op2.Error;
                Debug.LogError($"Error Detail Type: {error.message}");
                yield break;
            }


            //////////////////////////////////////////////////////////////////////////
            //
            // Ŭ���̾�Ʈ ���� ó�� ���
            //Debug.Log($"Client>>  createAnswer start");
            // Since the 'remote' side has no media stream we need
            // to pass in the right constraints in order for it to
            // accept the incoming offer of audio and video.

            var op3 = clientPeerConnection.CreateAnswer();
            yield return op3;
            if (!op3.IsError)
            {
                yield return OnCreateAnswerSuccess(clientPeerConnection, op3.Desc);
            }
            else
            {
                Debug.LogError($"Error Detail Type: {op3.Error.message}");
            }
        }


        public IEnumerator OnCreateAnswerSuccess(RTCPeerConnection clientPeer, RTCSessionDescription desc)
        {
            // Ŭ���̾�Ʈ ���� ó�� ���
            // �Ķ���� - RTCPeerConnection�� Ŭ���̾�Ʈ �̴�.

            //Debug.Log($"Answer from Client >> :\n{desc.sdp}");
            //Debug.Log($"Client >>  setLocalDescription start");
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


            //  �������� ����
            //  3333333333333333333333333333333  >>>>>>>>>>>>>>>>>

            ////////////////////////////////////////////////////////////////////////
            // ���� ���� ó��.

            // ��Ŷ ��� ������. �ᱹ�� 
            //  WRTCServerPeer.Instance.RecvWRTCSetRemoteDescription(sdpPacket); ȣ�� ��.
            SendWRTCSetRemoteDescription(new string(desc.sdp));
        }
        //

        void SendWRTCSetRemoteDescription(string sdpPacket)
        {

            WRTCServerPeer.Instance.RecvWRTCSetRemoteDescription(sdpPacket);
        }

        void SendWRTCAddIceCandidate(string Candidate, string SdpMid, int SdpMLineIndex)
        {
            WRTCServerPeer.Instance.RecvWRTCAddIceCandidate(Candidate, SdpMid, SdpMLineIndex);
        }

        // ���� AddIceCandidate
        public void RecvWRTCAddIceCandidate(string Candidate, string SdpMid, int SdpMLineIndex)
        {
            //clientPeerConnection.AddIceCandidate(candidate);
            //Debug.Log($"Client>> ICE candidate:\n {candidate.Candidate}");

            RTCIceCandidateInit init = new RTCIceCandidateInit();
            init.candidate = new string(Candidate);
            init.sdpMid = new string(SdpMid);
            init.sdpMLineIndex = SdpMLineIndex;

            //    >>>>>>>>>>>>>>>>>
            
            clientPeerConnection.AddIceCandidate(new RTCIceCandidate(init));
        }

        // ������ �������ų� ���°� �����
        void SendChangedStaus(RTCIceConnectionState state)
        {
            Debug.Log($"Client >> SendChangedStaus: {state}");
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

            //    >>>>>>>>>>>>>>>>>

            // ��� connection�� �����ϰ�.  AddIceCandidate ����� �Ѵ�.
            // �ٸ� Ŭ���̾�Ʈ connection ���� �˾ƿ;� �Ѵ�.  �̰��� webRTC������ signaling ��� �Ѵ���.
            //
            // �켱�� ���� ȣ��  Ŭ���̾�Ʈ ���� 
            // �⺻������ WebRTC���� signaling�� �������Ѵ�.

            // ��Ŷ ��� ������. �ᱹ�� 
           
            SendWRTCAddIceCandidate(candidate.Candidate, candidate.SdpMid, (int)candidate.SdpMLineIndex);
        }

        #endregion


        #region OnIceConnectionChange

        private void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state)
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

            Debug.Log($"Client>> candidate stats Id:{remoteCandidateStats.Id}, Type:{remoteCandidateStats.candidateType}");
            var updateText = TestUI.Instance.remoteCandidateId;
            updateText.text = remoteCandidateStats.Id;
        }

        #endregion

        #region �ܺ� UI �������̽�
        //
        public void OnStart(Camera cam)
        {
        }

        public void OnCall()
        {
            var configuration = WRTCUtil.GetSelectedSdpSemantics();

            clientPeerConnection = new RTCPeerConnection(ref configuration);
            clientPeerConnection.OnIceCandidate = clientOnIceCandidate;
            clientPeerConnection.OnIceConnectionChange = clientOnIceConnectionChange;
            clientPeerConnection.OnTrack = clientOntrack;

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

            clientPeerConnection.Close();
            clientPeerConnection.Dispose();
            clientPeerConnection = null;
        }

        public void OnRestartIce()
        {
        }

        public void AddTracks()
        {
        }

        public void RemoveTracks()
        {
            var tracks = receiveStream.GetTracks().ToArray();
            foreach (var track in tracks)
            {
                receiveStream.RemoveTrack(track);
            }
        }
        #endregion
    }

}