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


        #region �ܺ� �������̽� UI
        public void InitReceiveStream(int index, System.Action<Texture> func)
        {
            clientPeers[index].GetComponent<WRTCClientPeer>().InitReceiveStream(func);
        }


        public void OnCall(int index)
        {
            clientPeers[index].GetComponent<WRTCClientPeer>().OnCall();
            currentCallClientPeer = index;
        }

        public void OnHangUp(int index)
        {
            clientPeers[index].GetComponent<WRTCClientPeer>().OnHangUp();
            currentCallClientPeer = -1;
        }

        #endregion


        #region ���� ->  Ŭ���̾�Ʈ  : ��Ʈ��ũ ��������  

        public void RecvWRTCSetRemoteDescription(string sdpPacket)
        {
            if (currentCallClientPeer == -1)
                return;

            clientPeers[currentCallClientPeer].GetComponent<WRTCClientPeer>().RecvWRTCSetRemoteDescription(sdpPacket);
        }

        public void RecvWRTCAddIceCandidate(string CandidateWRTC, string SdpMidWRTC, int SdpMLineIndexWRTC)
        {
            if (currentCallClientPeer == -1)
                return;

            clientPeers[currentCallClientPeer].GetComponent<WRTCClientPeer>().RecvWRTCAddIceCandidate(CandidateWRTC, SdpMidWRTC, SdpMLineIndexWRTC);
        }

        public void ChangeStatusServer(RTCIceConnectionState state)
        {
            Debug.Log($"ChangeStatusServer >> {state}");

        }



        #endregion


    }

}
