using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int checkPointIndex;
    [SerializeField] private Transform spawnPoint;

    public int CheckpointIndex
    {
        get { return checkPointIndex; }
        set { checkPointIndex = value; }
    }

    public Vector3 CheckpointSpawnPosition
    {
        get { return spawnPoint != null ? spawnPoint.position : transform.position; }
    }

    public Quaternion CheckpointSpawnRotation
    {
        get { return spawnPoint != null ? spawnPoint.rotation : transform.rotation; }
    }


    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<RaceParticipant>(out RaceParticipant participant))
        {
            participant.UpdateCheckpoint(this);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, GetComponent<BoxCollider>().size);
    }
}