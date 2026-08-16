using UnityEngine;

[RequireComponent (typeof(BoxCollider))]
public class RespawnToLastCheckpoint : MonoBehaviour
{
    private BoxCollider m_BoxCollider;

    private void Awake()
    {
        m_BoxCollider = GetComponent<BoxCollider>();
        m_BoxCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<ArcadeController>(out ArcadeController controller))
        {
            controller.TeleportToLastCheckPoint();
        }
    }
}
