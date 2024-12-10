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



        #region 네트워크 프로토콜
        // 패킷으로 RTCSessionDescription, RTCIceCandidate 구조체 주고 받아야 함을 알수 있다.
        // signaling을 webRTC에서는  공식 지원하지 않는다.


        public void RecvWRTCSetRemoteDescription(string sdpPacket)
        {
            //  2222222222222222222222222222222222222  >>>>>>>>>>>>>>>>>

            // GINO
            // 패킷 통신을 하기 위해서
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
            // 클라이언트 응답 처리 계속
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
            // 클라이언트 응답 처리 계속
            // 파라미터 - RTCPeerConnection이 클라이언트 이다.

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


            //  서버에게 전송
            //  3333333333333333333333333333333  >>>>>>>>>>>>>>>>>

            ////////////////////////////////////////////////////////////////////////
            // 서버 응답 처리.

            // 패킷 통신 하지만. 결국은 
            //  WRTCServerPeer.Instance.RecvWRTCSetRemoteDescription(sdpPacket); 호출 됨.
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

        // 최종 AddIceCandidate
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

        // 접속이 끊어지거나 상태가 변경됨
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

            // 모든 connection이 성공하고.  AddIceCandidate 해줘야 한다.
            // 다른 클라이언트 connection 정보 알아와야 한다.  이것을 webRTC에서는 signaling 라고 한다함.
            //
            // 우선은 직접 호출  클라이언트 입장 
            // 기본적으로 WebRTC에서 signaling는 지원안한다.

            // 패킷 통신 하지만. 결국은 
           
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

        #region 외부 UI 인터페이스
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