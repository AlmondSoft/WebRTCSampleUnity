using UnityEngine;
using System.Collections.Generic;
using Unity.WebRTC;

namespace JWebRTC
{
    public class WRTCCore : MonoBehaviour
    {
        public static WRTCCore Instance = null;

        [SerializeField]
        int streamSizeX, streamSizeY;

        MediaStream sendStream;

        [SerializeField]
        Dictionary<int, GameObject> serverPeers;

        [SerializeField]
        Dictionary<int, GameObject> clientPeers;

        int serverCounter, clientCounter;

        private void Awake()
        {
            Instance = this;

            serverCounter = 0;
            clientCounter = 0;

            serverPeers = new Dictionary<int, GameObject>();
            clientPeers = new Dictionary<int, GameObject>();
        }


        #region 외부 인터페이스 UI - 서버
        public int CreateServer(Camera cam, System.Action<Camera> action)
        {
            //
            GameObject serverPeer = new GameObject("serverPeer");
            serverPeer.transform.parent = gameObject.transform;
            serverPeer.AddComponent<WRTCServerPeer>();

            serverPeers.Add(++serverCounter, serverPeer);

            //
            if (sendStream == null)
            {
                sendStream = cam.CaptureStream(streamSizeX, streamSizeY);
            }

            serverPeers[serverCounter].GetComponent<WRTCServerPeer>().OnCreate(cam, sendStream);
            action.Invoke(cam);

            return serverCounter;
        }

        public void ReplyByServer(int index)
        {
            serverPeers[index].GetComponent<WRTCServerPeer>().SendWRTCSetRemoteDescription();
        }


        public void RestartIceServer(int index)
        {
            serverPeers[index].GetComponent<WRTCServerPeer>().OnRestartIce();
        }

        public void CloseServer(int index)
        {
            serverPeers[index].GetComponent<WRTCServerPeer>().OnClose();
        }

        #endregion


        #region 외부 인터페이스 UI - 클라이언트
        public int CreateClient(System.Action<Texture> func)
        {
            //
            GameObject clientPeer = new GameObject("clientPeer");
            clientPeer.transform.parent = gameObject.transform;
            clientPeer.AddComponent<WRTCClientPeer>();

            clientPeers.Add(++clientCounter, clientPeer);

            clientPeers[clientCounter].GetComponent<WRTCClientPeer>().InitReceiveStream(func);
            return clientCounter;
        }

        
        public void RequestByClient(int index)
        {
            clientPeers[index].GetComponent<WRTCClientPeer>().OnRequest();
        }

        public void CloseClient(int index)
        {
            clientPeers[index].GetComponent<WRTCClientPeer>().OnClose();
        }

       

        #endregion


        #region 서버 ->  클라이언트  : 네트워크 프로토콜  

        public void RecvWRTCSetRemoteDescription_SC(string sdpPacket)
        {
            clientPeers[1].GetComponent<WRTCClientPeer>().RecvWRTCSetRemoteDescription(sdpPacket);
        }

        public void RecvWRTCAddIceCandidate_SC(string CandidateWRTC, string SdpMidWRTC, int SdpMLineIndexWRTC)
        {
            clientPeers[1].GetComponent<WRTCClientPeer>().RecvWRTCAddIceCandidate(CandidateWRTC, SdpMidWRTC, SdpMLineIndexWRTC);
        }

        public void ChangeStatusServer_SC(RTCIceConnectionState state)
        {
            Debug.Log($"ChangeStatusServer >> {state}");
        }

        #endregion

        #region 클라이언트 -> 서버 : 네트워크 프로토콜

        public void RecvWRTCSetRemoteDescription_CS(string sdpPacket)
        {
            serverPeers[1].GetComponent<WRTCServerPeer>().RecvWRTCSetRemoteDescription(sdpPacket);
        }

        public void RecvWRTCAddIceCandidate_CS(string Candidate, string SdpMid, int SdpMLineIndex)
        {
            serverPeers[1].GetComponent<WRTCServerPeer>().RecvWRTCAddIceCandidate(Candidate, SdpMid, SdpMLineIndex);
        }


        #endregion

    }

}
