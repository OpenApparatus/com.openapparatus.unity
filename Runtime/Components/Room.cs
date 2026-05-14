using UnityEngine;
using OpenApparatus.Topology;

namespace OpenApparatus.Unity
{
    [DisallowMultipleComponent]
    public sealed class Room : MonoBehaviour
    {
        public int RoomId;
        public RoomType RoomType;
        public Vector2 GridPositionStudio;
        public Vector2Int[] TileIndices;
    }
}
