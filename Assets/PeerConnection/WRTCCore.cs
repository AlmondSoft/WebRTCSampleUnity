using UnityEngine;
using System.Collections.Generic;
using Unity.WebRTC;

namespace JWebRTC
{
    public class WRTCCore : MonoBehaviour
    {
        public static WRTCCore Instance = null;


        [SerializeField]
        List<GameObject> serverPeers;

        [SerializeField]
        List<GameObject> clientPeers;

        [SerializeField]
        int currentCallClientPeer;

        private void Awake()
        {
            Instance = this;
            currentCallClientPeer = -1;
        }


        #region �ܺ� �������̽� UI - ����
        public void StartServer(Camera cam)
        {
            serverPeers[0].GetComponent<WRTCServerPeer>().OnStart(cam);
        }
        public void CallServer()
        {
            serverPeers[0].GetComponent<WRTCServerPeer>().OnCall();
        }


        public void SendDescServer()
        {
            serverPeers[0].GetComponent<WRTCServerPeer>().SendWRTCSetRemoteDescription();
        }


        public void RestartIceServer()
        {
            serverPeers[0].GetComponent<WRTCServerPeer>().OnRestartIce();
        }

        public void HangUpServer()
        {
            serverPeers[0].GetComponent<WRTCServerPeer>().OnHangUp();
        }

        #endregion


        #region �ܺ� �������̽� UI - Ŭ���̾�Ʈ
        public void CreateClient()
        {

        }

        public void InitReceiveStream(int index, System.Action<Texture> func)
        {
            clientPeers[index].GetComponent<WRTCClientPeer>().InitReceiveStream(func);
        }

        public void CallClient(int index)
        {
            clientPeers[index].GetComponent<WRTCClientPeer>().OnCall();
            currentCallClientPeer = index;
        }

        public void HangUpClient(int index)
        {
            clientPeers[index].GetComponent<WRTCClientPeer>().OnHangUp();
            currentCallClientPeer = -1;
        }

       

        #endregion


        #region ���� ->  Ŭ���̾�Ʈ  : ��Ʈ��ũ ��������  

        public void RecvWRTCSetRemoteDescription_SC(string sdpPacket)
        {
            if (currentCallClientPeer == -1)
                return;

            clientPeers[currentCallClientPeer].GetComponent<WRTCClientPeer>().RecvWRTCSetRemoteDescription(sdpPacket);
        }

        public void RecvWRTCAddIceCandidate_SC(string CandidateWRTC, string SdpMidWRTC, int SdpMLineIndexWRTC)
        {
            if (currentCallClientPeer == -1)
                return;

            clientPeers[currentCallClientPeer].GetComponent<WRTCClientPeer>().RecvWRTCAddIceCandidate(CandidateWRTC, SdpMidWRTC, SdpMLineIndexWRTC);
        }

        public void ChangeStatusServer_SC(RTCIceConnectionState state)
        {
            Debug.Log($"ChangeStatusServer >> {state}");

        }

        #endregion

        #region Ŭ���̾�Ʈ -> ���� : ��Ʈ��ũ ��������

        public void RecvWRTCSetRemoteDescription_CS(string sdpPacket)
        {
            serverPeers[0].GetComponent<WRTCServerPeer>().RecvWRTCSetRemoteDescription(sdpPacket);
        }

        public void RecvWRTCAddIceCandidate_CS(string Candidate, string SdpMid, int SdpMLineIndex)
        {
            serverPeers[0].GetComponent<WRTCServerPeer>().RecvWRTCAddIceCandidate(Candidate, SdpMid, SdpMLineIndex);
        }


        #endregion

    }

}
