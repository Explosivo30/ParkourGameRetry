using System;
using System.Collections.Generic;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace EZCollabTool
{
    public enum EZMessageType
    {
        SceneSnapshot,
        SceneSnapshotBegin,
        SceneSnapshotChunk,
        SceneSnapshotEnd,
        RecoveryComplete,
        CreateObject,
        DestroyObject,
        TransformDelta,
        ComponentDelta,
        LockRequest,
        LockGranted,
        LockDenied,
        LockRelease,
        ObjectLocked,
        ObjectUnlocked,
        PeerJoined,
        PeerLeft,
        Heartbeat,
        Info
    }

    [Serializable]
    public class EZMessage
    {
        public const int MaxPayloadBytes = 64 * 1024 * 1024;

        public EZMessageType type;
        public string senderId;
        public long timestamp;
        public long sequence;
        public string payload;

        public static EZMessage Create(EZMessageType type, string senderId, object payload)
        {
            return new EZMessage
            {
                type = type,
                senderId = senderId,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                payload = JsonConvert.SerializeObject(payload)
            };
        }

        public T GetPayload<T>()
        {
            return JsonConvert.DeserializeObject<T>(payload);
        }

        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        }

        public static EZMessage FromBytes(byte[] bytes, int count)
        {
            string json = Encoding.UTF8.GetString(bytes, 0, count);
            return JsonConvert.DeserializeObject<EZMessage>(json);
        }
    }

    [Serializable]
    public class TransformPayload
    {
        public string guid;
        public float[] position;
        public float[] rotation;
        public float[] scale;

        public static TransformPayload FromTransform(string guid, Transform transform)
        {
            return new TransformPayload
            {
                guid = guid,
                position = new float[] { transform.position.x, transform.position.y, transform.position.z },
                rotation = new float[] { transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w },
                scale = new float[] { transform.localScale.x, transform.localScale.y, transform.localScale.z }
            };
        }

        public Vector3 GetPosition()
        {
            return new Vector3(position[0], position[1], position[2]);
        }

        public Quaternion GetRotation()
        {
            return new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);
        }

        public Vector3 GetScale()
        {
            return new Vector3(scale[0], scale[1], scale[2]);
        }
    }

    [Serializable]
    public class LockPayload
    {
        public string guid;
        public string peerId;
        public string peerName;
    }

    [Serializable]
    public class PeerPayload
    {
        public string peerId;
        public string peerName;
        public long lastSequence;
    }

    [Serializable]
    public class CreateObjectPayload
    {
        public string guid;
        public string parentGuid;
        public string objectName;
        public ObjectSourceType sourceType;
        public string prefabPath;
        public string prefabGuid;
        public string prefabHash;
        public string undoName;
        public bool releaseLockAfterApply;
        public PrimitiveType primitiveType;
        public SerializedTransform localTransform;
        public int siblingIndex;
        public bool active;
        public int layer;
        public string tag;
        public int staticEditorFlags;
        public List<SerializedComponent> components = new List<SerializedComponent>();
        public SerializedMesh mesh;
    }

    public enum ObjectSourceType
    {
        Primitive,
        Prefab,
        Empty,
        Generic
    }

    [Serializable]
    public class SerializedComponent
    {
        public string typeName;
        public int occurrenceIndex;
        public string json;
        public List<SerializedObjectReference> objectReferences = new List<SerializedObjectReference>();
    }

    [Serializable]
    public class SerializedObjectReference
    {
        public string propertyPath;
        public string assetGuid;
        public string assetPath;
        public string assetHash;
        public long localFileId;
        public string sceneObjectGuid;
        public string componentTypeName;
        public int componentOccurrenceIndex;
    }

    [Serializable]
    public class SerializedMesh
    {
        public string name;
        public int indexFormat;
        public float[] positions;
        public float[] normals;
        public float[] tangents;
        public float[] colors;
        public float[] uv0;
        public float[] uv1;
        public float[] uv2;
        public float[] uv3;
        public List<SerializedSubMesh> subMeshes = new List<SerializedSubMesh>();
    }

    [Serializable]
    public class SerializedSubMesh
    {
        public int topology;
        public int[] indices;
    }

    [Serializable]
    public class SerializedTransform
    {
        public float[] position;
        public float[] rotation;
        public float[] scale;

        public static SerializedTransform FromTransform(Transform transform)
        {
            return new SerializedTransform
            {
                position = new float[] { transform.localPosition.x, transform.localPosition.y, transform.localPosition.z },
                rotation = new float[] { transform.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w },
                scale = new float[] { transform.localScale.x, transform.localScale.y, transform.localScale.z }
            };
        }

        public Vector3 GetPosition()
        {
            return new Vector3(position[0], position[1], position[2]);
        }

        public Quaternion GetRotation()
        {
            return new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);
        }

        public Vector3 GetScale()
        {
            return new Vector3(scale[0], scale[1], scale[2]);
        }
    }

    [Serializable]
    public class DestroyObjectPayload
    {
        public string guid;
    }
}
