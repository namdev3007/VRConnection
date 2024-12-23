using Unity.Netcode;
using UnityEngine;

public class PlayerVRController : NetworkBehaviour
{
    public GameObject XROrigin; // Tham chiếu tới XR Origin hoặc XR Rig

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Vô hiệu hóa XR Rig cho những client không sở hữu
            if (XROrigin != null)
            {
                XROrigin.SetActive(false);
            }
        }
        else
        {
            // Bật XR Rig cho chủ sở hữu
            if (XROrigin != null)
            {
                XROrigin.SetActive(true);
            }
        }
    }
}
