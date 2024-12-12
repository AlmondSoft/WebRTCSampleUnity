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

        int serverIndexCounter, clientIndexCounter;


        #region WEBRTC - Signaling  �� ó���� ��ü������ �ؾ� �Ѵ�. ���� ����.


        //temp
        public static string myUserKey;
        // ����� Ű�� ���� �Ѵ�.
        public static string CreateUserIDKey()
        {
            return "11";
        }


        // �Ʒ� ������ ���� node js�� �̵��ؾ� �Ѵ�. 
        public class RTCPeerInfo
        { 
            public enum Type { Server, Client};
            public Type type;
            public int index;

            public string otherPeerKey;
            public int otherPeerIndex;

            // ���Ŀ� ���� ���� �ִ´�.
        }

        // �Ʒ� ������ ���� node js�� �̵��ؾ� �Ѵ�. 
        //
        Dictionary<string, List<RTCPeerInfo>> listServerPeers;
        Dictionary<string, List<RTCPeerInfo>> listClientPeers;

        
        public void SignalingRTCSendRegisteryServerPeer(string myUserKey, int myServerIndex)
        {
            // �Ʒ� ������ ���� node js�� �̵��ؾ� �Ѵ�.  ���⼭�� ��Ŷ send�� �־�� �Ѵ�.

            if (!listServerPeers.ContainsKey(myUserKey))
            {
                List<RTCPeerInfo> peerList = new List<RTCPeerInfo>();
                listServerPeers.Add(myUserKey, peerList);
            }

            RTCPeerInfo peer = new RTCPeerInfo();
            peer.type = RTCPeerInfo.Type.Server;
            peer.index = myServerIndex;

            listServerPeers[myUserKey].Add(peer);
        }

        public void SignalingRTCSendRegisteryClientPeer(string myUserKey, int myClientIndex)
        {
            // �Ʒ� ������ ���� node js�� �̵��ؾ� �Ѵ�. ���⼭�� ��Ŷ send�� �־�� �Ѵ�.

            if (!listClientPeers.ContainsKey(myUserKey))
            {
                List<RTCPeerInfo> peerList = new List<RTCPeerInfo>();
                listClientPeers.Add(myUserKey, peerList);
            }

            RTCPeerInfo peer = new RTCPeerInfo();
            peer.type = RTCPeerInfo.Type.Client;
            peer.index = myClientIndex;

            listClientPeers[myUserKey].Add(peer);
        }

        
        // Ŭ���̾�Ʈ���� ���� ����� ���� Ű.  �˷��ִ� �������� �ʿ�.
        public void SignalingRTCGetWantServerPeerKey(string myUserKey, int myClientIndex, System.Action<string, int> action)
        {
            // �Ʒ� ������ ���� node js�� �̵��ؾ� �Ѵ�. ���⼭�� ��Ŷ send�� �־�� �Ѵ�.

            // � ������ ã���� ���� �ʿ�..


            // �켱�� �ӽ�..
            foreach (var key in listServerPeers.Keys)
            {
                if(listServerPeers[key].Count > 0)
                {
                    action.Invoke(key, listServerPeers[key][0].index);
                }
            }
        }

        // Ŭ���̾�Ʈ���� �ش� ���� ���� ���Ѵٴ� �޽��� ��.
        public void SignalingRTCSendRequestFromClientPeer(  string myClientKey, int myClientIndex, 
                                                            string wantServerKey, int wantServerIndex, 
                                                            System.Action<bool> action)
        {
            // �Ʒ� ������ ���� node js�� �̵��ؾ� �Ѵ�. ���⼭�� ��Ŷ send�� �־�� �Ѵ�.

            if (listServerPeers.ContainsKey(wantServerKey) 
                && listClientPeers.ContainsKey(myClientKey))
            {
                List<RTCPeerInfo> peerServerList = listServerPeers[wantServerKey];
                List<RTCPeerInfo> peerClientList = listClientPeers[wantServerKey];

                RTCPeerInfo peerServer = peerServerList.Find(peer => peer.index == wantServerIndex);
                if (peerServer != null)
                {
                    peerServer.otherPeerKey = myClientKey;
                    peerServer.otherPeerIndex = myClientIndex;

                    //
                    RTCPeerInfo peerClient = peerClientList.Find(peer => peer.index == myClientIndex);
                    if (peerClient != null)
                    {
                        peerClient.otherPeerKey = wantServerKey;
                        peerClient.otherPeerIndex = wantServerIndex;

                        action.Invoke(true);
                        return;
                    }
                }
            }

            action.Invoke(false);
        }

        // ������ Ŭ���̾�Ʈ���� Reply -> Description ������ ����.
        public void SignalingRTCSendSetRemoteDescriptionFromServer(string myServerKey, int myServerIndex, string sdpPacketWRTC)
        {
            // �Ʒ� ������ ���� node js�� �̵��ؾ� �Ѵ�.  ���⼭�� ��Ŷ send�� �־�� �Ѵ�.
            if (listServerPeers.ContainsKey(myServerKey))
            {
                List<RTCPeerInfo> peerServerList = listServerPeers[myServerKey];
                RTCPeerInfo peerServer = peerServerList.Find(peer => peer.index == myServerIndex);
                if(peerServer != null)
                {
                    // ��Ʈ�� ȣ��.. �켱�� ���� ȣ��
                    SignalingRTCRecvSetRemoteDescriptionFromServer(peerServer.otherPeerKey, peerServer.otherPeerIndex, sdpPacketWRTC);
                }
            }
        }

        // Ŭ���̾�Ʈ�� Description  ���� ����.
        public void SignalingRTCRecvSetRemoteDescriptionFromServer(string wantClientKey, int wantClientIndex, string sdpPacketWRTC)
        {
            RecvWRTCSetRemoteDescription_SC(wantClientIndex, sdpPacketWRTC);
        }


        // Ŭ���̾�Ʈ�� �������� - �������� �ٷ� Description ������ ����.
        public void SignalingRTCSendSetRemoteDescriptionFromClient(string myClientKey, int myClientIndex, string sdpPacketWRTC)
        {
            // �Ʒ� ������ ���� node js�� �̵��ؾ� �Ѵ�.  ���⼭�� ��Ŷ send�� �־�� �Ѵ�.
            if (listClientPeers.ContainsKey(myClientKey))
            {
                List<RTCPeerInfo> peerClientList = listClientPeers[myClientKey];
                RTCPeerInfo peerClient = peerClientList.Find(peer => peer.index == myClientIndex);
                if (peerClient != null)
                {
                    // ��Ʈ�� ȣ��.. �켱�� ���� ȣ��
                    SignalingRTCRecvSetRemoteDescriptionFromClient(peerClient.otherPeerKey, peerClient.otherPeerIndex, sdpPacketWRTC);
                }
            }
        }

        // ������ Description  ���� ����.
        public void SignalingRTCRecvSetRemoteDescriptionFromClient(string wantServerKey, int wantServerIndex, string sdpPacketWRTC)
        {
            RecvWRTCSetRemoteDescription_CS(wantServerIndex, sdpPacketWRTC);
        }


        // Ŭ���̾�Ʈ�� �������� Candidate ������ ������.  �ñ׳θ� ������ Ŭ���̾�Ʈ�� ���� ȣ���Ѵ�.
        public void SignalingRTCSendCandidateFromClient(string myClientKey, int myClientIndex, string Candidate, string SdpMid, int SdpMLineIndex)
        {
            // �Ʒ� ������ ���� node js�� �̵��ؾ� �Ѵ�.  ���⼭�� ��Ŷ send�� �־�� �Ѵ�.
            if (listClientPeers.ContainsKey(myClientKey))
            {
                List<RTCPeerInfo> peerClientList = listClientPeers[myClientKey];
                RTCPeerInfo peerClient = peerClientList.Find(peer => peer.index == myClientIndex);
                if (peerClient != null)
                {
                    // ��Ʈ�� ȣ��.. �켱�� ���� ȣ��
                    SignalingRTCRecvCandidateFromClient(peerClient.otherPeerKey, peerClient.otherPeerIndex, Candidate, SdpMid, SdpMLineIndex);
                }
            }
        }

        // ������ Candidate ���� ����
        public void SignalingRTCRecvCandidateFromClient(string wantServerKey, int wantServerIndex, string Candidate, string SdpMid, int SdpMLineIndex)
        {
            RecvWRTCAddIceCandidate_CS(wantServerIndex, Candidate, SdpMid, SdpMLineIndex);
        }


        // ���� �������ݰ��� ������ �ڵ彦��ŷ ����
        // ������ Ŭ���̾�Ʈ���� Candidate ������ ������.  �ñ׳θ� ������ Ŭ���̾�Ʈ�� ���� ȣ���Ѵ�.
        public void SignalingRTCSendCandidateFromServer(string myServerKey, int myServerIndex, string Candidate, string SdpMid, int SdpMLineIndex)
        {
            // �Ʒ� ������ ���� node js�� �̵��ؾ� �Ѵ�.  ���⼭�� ��Ŷ send�� �־�� �Ѵ�.
            if (listServerPeers.ContainsKey(myServerKey))
            {
                List<RTCPeerInfo> peerServerList = listServerPeers[myServerKey];
                RTCPeerInfo peerServer = peerServerList.Find(peer => peer.index == myServerIndex);
                if (peerServer != null)
                {
                    // ��Ʈ�� ȣ��.. �켱�� ���� ȣ��
                    SignalingRTCRecvCandidateFromServer(peerServer.otherPeerKey, peerServer.otherPeerIndex, Candidate, SdpMid, SdpMLineIndex);
                }
            }
        }
        public void SignalingRTCRecvCandidateFromServer(string wantClientKey, int wantClientIndex, string Candidate, string SdpMid, int SdpMLineIndex)
        {
            RecvWRTCAddIceCandidate_SC(wantClientIndex, Candidate, SdpMid, SdpMLineIndex);
        }


        #endregion


        private void Awake()
        {
            Instance = this;

            serverIndexCounter = 0;
            clientIndexCounter = 0;

            serverPeers = new Dictionary<int, GameObject>();
            clientPeers = new Dictionary<int, GameObject>();

            listServerPeers = new Dictionary<string, List<RTCPeerInfo>>();
            listClientPeers = new Dictionary<string, List<RTCPeerInfo>>();

            // TEMP..
            myUserKey = CreateUserIDKey();
        }



        #region �ܺ� �������̽� UI - ����
        public int CreateServer(Camera cam, System.Action<Camera> updateTextureCallback)
        {
            //
            GameObject serverPeer = new GameObject("serverPeer");
            serverPeer.transform.parent = gameObject.transform;
            serverPeer.AddComponent<WRTCServerPeer>();

            serverPeers.Add(++serverIndexCounter, serverPeer);

            //
            if (sendStream == null)
            {
                sendStream = cam.CaptureStream(streamSizeX, streamSizeY);
            }

            serverPeers[serverIndexCounter].GetComponent<WRTCServerPeer>().SetIndexKey(myUserKey, serverIndexCounter);
            serverPeers[serverIndexCounter].GetComponent<WRTCServerPeer>().OnCreate(cam, sendStream);
            updateTextureCallback.Invoke(cam);

            //
            SignalingRTCSendRegisteryServerPeer(myUserKey, serverIndexCounter);

            return serverIndexCounter;
        }

        public void ReplyByServer(int index)
        {
            if(serverPeers.ContainsKey(index))
                serverPeers[index].GetComponent<WRTCServerPeer>().SendWRTCSetRemoteDescription();
        }


        public void RestartIceServer(int index)
        {
            if (serverPeers.ContainsKey(index))
                serverPeers[index].GetComponent<WRTCServerPeer>().OnRestartIce();
        }

        public void CloseServer(int index)
        {
            if (serverPeers.ContainsKey(index))
            {
                GameObject server = serverPeers[index];
                server.GetComponent<WRTCServerPeer>().OnClose();
                serverPeers.Remove(index);

                Destroy(server);
                server = null;
            }
        }

        #endregion


        #region �ܺ� �������̽� UI - Ŭ���̾�Ʈ
        public int CreateClient(System.Action<Texture> func)
        {
            //
            GameObject clientPeer = new GameObject("clientPeer");
            clientPeer.transform.parent = gameObject.transform;
            clientPeer.AddComponent<WRTCClientPeer>();

            clientPeers.Add(++clientIndexCounter, clientPeer);

            clientPeers[serverIndexCounter].GetComponent<WRTCClientPeer>().SetIndexKey(myUserKey, clientIndexCounter);
            clientPeers[clientIndexCounter].GetComponent<WRTCClientPeer>().InitReceiveStream(func);

            SignalingRTCSendRegisteryClientPeer(myUserKey, clientIndexCounter);

            return clientIndexCounter;
        }

        
        public void RequestByClient(int index, string wantServerKey, int wantServerIndex)
        {
            if (clientPeers.ContainsKey(index))
            {
                //
                SignalingRTCSendRequestFromClientPeer(myUserKey, index, wantServerKey, wantServerIndex, (isSuccess) =>
                {
                    if (isSuccess)
                        clientPeers[index].GetComponent<WRTCClientPeer>().OnRequest();
                    else
                    {
                        CloseClient(index);
                    }
                });

                //
            }
        }

        public void CloseClient(int index)
        {
            if (clientPeers.ContainsKey(index))
            {
                GameObject client = clientPeers[index];
                client.GetComponent<WRTCClientPeer>().OnClose();
                clientPeers.Remove(index);

                Destroy(client);
                client = null;
            }
        }

       

        #endregion


        #region ���� ->  Ŭ���̾�Ʈ  : ��Ʈ��ũ ��������  

        public void RecvWRTCSetRemoteDescription_SC(int clientIndex, string sdpPacket)
        {
            if (clientPeers.ContainsKey(clientIndex))
                clientPeers[clientIndex].GetComponent<WRTCClientPeer>().RecvWRTCSetRemoteDescription(sdpPacket);
        }

        public void RecvWRTCAddIceCandidate_SC(int clientIndex, string CandidateWRTC, string SdpMidWRTC, int SdpMLineIndexWRTC)
        {
            if (clientPeers.ContainsKey(clientIndex))
                clientPeers[clientIndex].GetComponent<WRTCClientPeer>().RecvWRTCAddIceCandidate(CandidateWRTC, SdpMidWRTC, SdpMLineIndexWRTC);
        }

        public void ChangeStatusServer_SC(int key, RTCIceConnectionState state)
        {
            Debug.Log($"ChangeStatusServer >> {state}");
        }

        #endregion

        #region Ŭ���̾�Ʈ -> ���� : ��Ʈ��ũ ��������

        public void RecvWRTCSetRemoteDescription_CS(int serverIndex, string sdpPacket)
        {
            if (serverPeers.ContainsKey(serverIndex))
                serverPeers[serverIndex].GetComponent<WRTCServerPeer>().RecvWRTCSetRemoteDescription(sdpPacket);
        }

        public void RecvWRTCAddIceCandidate_CS(int serverIndex, string Candidate, string SdpMid, int SdpMLineIndex)
        {
            if (serverPeers.ContainsKey(serverIndex))
                serverPeers[serverIndex].GetComponent<WRTCServerPeer>().RecvWRTCAddIceCandidate(Candidate, SdpMid, SdpMLineIndex);
        }


        #endregion

    }

}
