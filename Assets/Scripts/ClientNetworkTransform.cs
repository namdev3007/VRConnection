using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkTransform : NetworkTransform
{
    // Override phương thức để chỉ định rằng Client sẽ có quyền điều khiển
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Client sẽ điều khiển Transform thay vì Server
    }
}
