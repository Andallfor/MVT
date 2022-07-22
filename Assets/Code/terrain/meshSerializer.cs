#region License and information
/* * * * *
 * A quick mesh serializer that allows to serialize a Mesh as byte array. It should
 * support any kind of mesh including skinned meshes, multiple submeshes, different
 * mesh topologies as well as blendshapes. I tried my best to avoid unnecessary data
 * by only serializing information that is present. It supports Vector4 UVs. The index
 * data may be stored as bytes, ushorts or ints depending on the actual highest used
 * vertex index within a submesh. It uses a tagging system for optional "chunks". The
 * base information only includes the vertex position array and the submesh count.
 * Everything else is handled through optional chunks.
 * 
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2018 Markus Göbel (Bunny83)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * * * * */
#endregion License and information

namespace B83.MeshTools
{
    using System.IO;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Linq;
    using System.Threading.Tasks;

    public static class MeshSerializer
    {
        /*
         * Structure:
         * - Magic string "Mesh" (4 bytes)
         * - vertex count [int] (4 bytes)
         * - submesh count [int] (4 bytes)
         * - vertices [array of Vector3]
         * 
         * - additional chunks:
         *   [vertex attributes]
         *   - Name (name of the Mesh object)
         *   - Normals [array of Vector3]
         *   - Tangents [array of Vector4]
         *   - Colors [array of Color32]
         *   - UV0-4 [
         *       - component count[byte](2/3/4)
         *       - array of Vector2/3/4
         *     ]
         *   - BoneWeights [array of 4x int+float pair]
         *   
         *   [other data]
         *   - Submesh [
         *       - topology[byte]
         *       - count[int]
         *       - component size[byte](1/2/4)
         *       - array of byte/ushort/int
         *     ]
         *   - Bindposes [
         *       - count[int]
         *       - array of Matrix4x4
         *     ]
         *   - BlendShape [
         *       - Name [string]
         *       - frameCount [int]
         *       - frames [ array of:
         *           - frameWeight [float]
         *           - array of [
         *               - position delta [Vector3]
         *               - normal delta [Vector3]
         *               - tangent delta [Vector3]
         *             ]
         *         ]
         *     ]
         */
        private enum EChunkID : byte
        {
            End,
            Name,
            Normals,
            Tangents,
            Colors,
            BoneWeights,
            UV0, UV1, UV2, UV3,
            Submesh,
            Bindposes,
            BlendShape,
        }
        const uint m_Magic = 0x6873654D; // "Mesh"

        public static byte[] SerializeMesh(Mesh aMesh, string name)
        {
            using (var stream = new MemoryStream())
            {
                SerializeMesh(stream, aMesh, name);
                return stream.ToArray();
            }
        }
        public static void SerializeMesh(MemoryStream aStream, Mesh aMesh, string name)
        {
            using (var writer = new BinaryWriter(aStream))
                SerializeMesh(writer, aMesh, name);
        }
        public static void SerializeMesh(BinaryWriter aWriter, Mesh aMesh, string name)
        {
            poleTerrain.savedPositions[name] = new Dictionary<string, long[]>();
            poleTerrain.savedPositions[name]["count"] = new long[1] {aMesh.vertices.Length};

            aWriter.Write(m_Magic);
            var vertices = aMesh.vertices;
            int count = vertices.Length;
            int subMeshCount = aMesh.subMeshCount;
            aWriter.Write(count);
            aWriter.Write(subMeshCount);

            long p1 = aWriter.BaseStream.Length;
            foreach (var v in vertices) aWriter.WriteVector3(v);
            poleTerrain.savedPositions[name]["verts"] = new long[2] {p1, aWriter.BaseStream.Length - p1};

            // start of tagged chunks
            if (!string.IsNullOrEmpty(aMesh.name)) {
                aWriter.Write((byte)EChunkID.Name);
                p1 = aWriter.BaseStream.Length;
                aWriter.Write(aMesh.name);
            }
            var normals = aMesh.normals;
            if (normals != null && normals.Length == count)
            {
                aWriter.Write((byte)EChunkID.Normals);
                p1 = aWriter.BaseStream.Length;
                foreach (var v in normals) aWriter.WriteVector3(v);
                poleTerrain.savedPositions[name]["normals"] = new long[2] {p1, aWriter.BaseStream.Length - p1};
                normals = null;
            }
            List<Vector4> uvs = new List<Vector4>();
            for (int i = 0; i < 4; i++)
            {
                uvs.Clear();
                aMesh.GetUVs(i, uvs);
                if (uvs.Count == count)
                {
                    aWriter.Write((byte)((byte)EChunkID.UV0 + i));
                    byte channelCount = 2;
                    foreach (var uv in uvs)
                    {
                        if (uv.z != 0f)
                            channelCount = 3;
                        if (uv.w != 0f)
                        {
                            channelCount = 4;
                            break;
                        }
                    }
                    aWriter.Write(channelCount);
                    p1 = aWriter.BaseStream.Length;
                    if (channelCount == 2)
                        foreach (var uv in uvs)
                            aWriter.WriteVector2(uv);
                    else if (channelCount == 3)
                        foreach (var uv in uvs)
                            aWriter.WriteVector3(uv);
                    else
                        foreach (var uv in uvs)
                            aWriter.WriteVector4(uv);
                }
                poleTerrain.savedPositions[name]["uvs"] = new long[2] {p1, aWriter.BaseStream.Length - p1};
            }
            List<int> indices = new List<int>(count * 3);
            for (int i = 0; i < subMeshCount; i++)
            {
                indices.Clear();
                aMesh.GetIndices(indices, i);
                if (indices.Count > 0)
                {
                    aWriter.Write((byte)EChunkID.Submesh);
                    aWriter.Write((byte)aMesh.GetTopology(i));
                    aWriter.Write(indices.Count);
                    var max = indices.Max();
                    if (max < 256)
                    {
                        aWriter.Write((byte)1);
                        poleTerrain.savedPositions[name]["componentCount"] = new long[1] {1};
                        p1 = aWriter.BaseStream.Length;
                        foreach (var index in indices)
                            aWriter.Write((byte)index);
                    }
                    else if (max < 65536)
                    {
                        aWriter.Write((byte)2);
                        poleTerrain.savedPositions[name]["componentCount"] = new long[1] {2};
                        p1 = aWriter.BaseStream.Length;
                        foreach (var index in indices)
                            aWriter.Write((ushort)index);
                    }
                    else
                    {
                        aWriter.Write((byte)4);
                        poleTerrain.savedPositions[name]["componentCount"] = new long[1] {4};
                        p1 = aWriter.BaseStream.Length;
                        foreach (var index in indices)
                            aWriter.Write(index);
                    }
                    poleTerrain.savedPositions[name]["indices"] = new long[3] {p1, aWriter.BaseStream.Length - p1, indices.Count};
                }
            }
            aWriter.Write((byte)EChunkID.End);
        }


        public static deserialzedMeshData DeserializeMesh(byte[] aData)
        {
            using (var stream = new MemoryStream(aData))
                return DeserializeMesh(stream);
        }
        public static deserialzedMeshData DeserializeMesh(MemoryStream aStream)
        {
            using (var reader = new BinaryReader(aStream))
                return DeserializeMesh(reader);
        }
        public static deserialzedMeshData DeserializeMesh(BinaryReader aReader)
        {
            if (aReader.ReadUInt32() != m_Magic)
                return null;
            deserialzedMeshData md = new deserialzedMeshData();
            int count = aReader.ReadInt32();
            if (count > 65534)
                md.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            int subMeshCount = aReader.ReadInt32();
            Vector3[] verts = new Vector3[count];
            for (int i = 0; i < count; i++)
                verts[i] = aReader.ReadVector3();
            md.vertices = verts;
            md.subMeshCount = subMeshCount;
            byte componentCount = 0;

            // reading chunks
            var stream = aReader.BaseStream;
            while ((stream.CanSeek && stream.Position < stream.Length) || stream.CanRead)
            {
                var chunkID = (EChunkID)aReader.ReadByte();
                if (chunkID == EChunkID.End)
                    break;
                switch (chunkID)
                {
                    case EChunkID.Name:
                        md.name = aReader.ReadString();
                        break;
                    case EChunkID.Normals:
                        Vector3[] normals = new Vector3[count];
                        for (int i = 0; i < count; i++)
                            normals[i] = aReader.ReadVector3();
                        md.normals = normals;
                        break;
                    case EChunkID.Tangents:
                        break;
                    case EChunkID.Colors:
                        break;
                    case EChunkID.BoneWeights:
                        break;
                    case EChunkID.UV0:
                    case EChunkID.UV1:
                    case EChunkID.UV2:
                    case EChunkID.UV3:
                        List<Vector4> vector4List = new List<Vector4>();
                        int uvChannel = chunkID - EChunkID.UV0;
                        componentCount = aReader.ReadByte();
                        if (vector4List == null)
                            vector4List = new List<Vector4>(count);
                        vector4List.Clear();

                        if (componentCount == 2)
                        {
                            for (int i = 0; i < count; i++)
                                vector4List.Add(aReader.ReadVector2());
                        }
                        else if (componentCount == 3)
                        {
                            for (int i = 0; i < count; i++)
                                vector4List.Add(aReader.ReadVector3());
                        }
                        else if (componentCount == 4)
                        {
                            for (int i = 0; i < count; i++)
                                vector4List.Add(aReader.ReadVector4());
                        }
                        md.uvChannel = uvChannel;
                        md.uvs = vector4List;
                        break;
                    case EChunkID.Submesh:
                        var topology = (MeshTopology)aReader.ReadByte();
                        int indexCount = aReader.ReadInt32();
                        var indices = new int[indexCount];
                        componentCount = aReader.ReadByte();
                        if (componentCount == 2) {
                            for (int i = 0; i < indexCount; i++) indices[i] = aReader.ReadUInt16();
                        } else {
                            for (int i = 0; i < indexCount; i++) indices[i] = aReader.ReadInt32();
                        }
                        md.indices = indices;
                        md.topology = topology;
                        break;
                    case EChunkID.Bindposes:
                        break;
                    case EChunkID.BlendShape:
                        break;
                }
            }

            return md;
        }
        public static async Task<deserialzedMeshData> quickDeserialize(string path) {
            string name = Path.GetFileNameWithoutExtension(path);
            Dictionary<string, long[]> key = poleTerrain.savedPositions[name];

            long count = key["count"][0];
            byte[] data = File.ReadAllBytes(path);

            // where we save the processed data
            int[] gindices = new int[key["indices"][2]];
            Vector3[] gvertices = new Vector3[count];
            Vector3[] gnormals = new Vector3[count];
            Vector4[] guvs = new Vector4[count];

            // can we utlize one stream instead of creating multiple?
            // currently the issue is that they advance the position, so it would mess up the other readers
            // and since we want to thread them together they will interefere with each other
            // note: do test if threading helps or not, may cause too much overhead
            BinaryReader verts = new BinaryReader(new MemoryStream(data));
            BinaryReader norms = new BinaryReader(new MemoryStream(data));
            BinaryReader indcs = new BinaryReader(new MemoryStream(data));
            BinaryReader bruvs = new BinaryReader(new MemoryStream(data));

            verts.BaseStream.Seek(key["verts"][0], SeekOrigin.Begin);
            norms.BaseStream.Seek(key["normals"][0], SeekOrigin.Begin);
            indcs.BaseStream.Seek(key["indices"][0], SeekOrigin.Begin);
            bruvs.BaseStream.Seek(key["uvs"][0], SeekOrigin.Begin);

            // stuff that follows count -> normals, verts, uvs
            Task[] tasks = new Task[4];
            tasks[0] = Task.Run(() => {for (int i = 0; i < count; i++) gvertices[i] = verts.ReadVector3();});
            tasks[1] = Task.Run(() => {for (int i = 0; i < count; i++) gnormals[i] = norms.ReadVector3();});
            tasks[2] = Task.Run(() => {for (int i = 0; i < count; i++) guvs[i] = bruvs.ReadVector4();});
            long componentCount = poleTerrain.savedPositions[name]["componentCount"][0];
            if (componentCount == 2) {
                tasks[3] = Task.Run(() => {for (int i = 0; i < gindices.Length; i++) gindices[i] = indcs.ReadUInt16();});
            } else if (componentCount == 4) {
                tasks[3] = Task.Run(() => {for (int i = 0; i < gindices.Length; i++) gindices[i] = indcs.ReadInt32();});
            } else {
                Debug.LogWarning("Encountered unknown component count. Please implement count for 1");
                // https://pastebin.com/yW91qEQh
            }

            await Task.WhenAll(tasks);

            verts.Dispose();
            norms.Dispose();
            indcs.Dispose();
            bruvs.Dispose();

            deserialzedMeshData dmd = new deserialzedMeshData();
            dmd.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            dmd.subMeshCount = 1;
            dmd.vertices = gvertices;
            dmd.uvs = guvs.ToList();
            dmd.normals = gnormals;
            dmd.indices = gindices;
            dmd.topology = MeshTopology.Triangles;
            dmd.name = "a";
            dmd.uvChannel = 2;

            return dmd;
        }
    }

    public class deserialzedMeshData {
        public UnityEngine.Rendering.IndexFormat indexFormat;
        public int subMeshCount;
        public Vector3[] vertices, normals;
        public string name;
        public int uvChannel;
        public List<Vector4> uvs;
        public MeshTopology topology;
        public int[] indices;

        public Mesh generate() {
            Mesh m = new Mesh();

            m.subMeshCount = subMeshCount;
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = vertices;
            m.SetIndices(indices, topology, 0, false);
            m.SetUVs(uvChannel, uvs);
            m.normals = normals;
            
            m.name = name;

            return m;
        }
    }


    public static class BinaryReaderWriterUnityExt
    {
        public static void WriteVector2(this BinaryWriter aWriter, Vector2 aVec)
        {
            aWriter.Write(aVec.x); aWriter.Write(aVec.y);
        }
        public static Vector2 ReadVector2(this BinaryReader aReader)
        {
            return new Vector2(aReader.ReadSingle(), aReader.ReadSingle());
        }
        public static void WriteVector3(this BinaryWriter aWriter, Vector3 aVec)
        {
            aWriter.Write(aVec.x); aWriter.Write(aVec.y); aWriter.Write(aVec.z);
        }
        public static Vector3 ReadVector3(this BinaryReader aReader)
        {
            return new Vector3(aReader.ReadSingle(), aReader.ReadSingle(), aReader.ReadSingle());
        }
        public static void WriteVector4(this BinaryWriter aWriter, Vector4 aVec)
        {
            aWriter.Write(aVec.x); aWriter.Write(aVec.y); aWriter.Write(aVec.z); aWriter.Write(aVec.w);
        }
        public static Vector4 ReadVector4(this BinaryReader aReader)
        {
            return new Vector4(aReader.ReadSingle(), aReader.ReadSingle(), aReader.ReadSingle(), aReader.ReadSingle());
        }

        public static void WriteColor32(this BinaryWriter aWriter, Color32 aCol)
        {
            aWriter.Write(aCol.r); aWriter.Write(aCol.g); aWriter.Write(aCol.b); aWriter.Write(aCol.a);
        }
        public static Color32 ReadColor32(this BinaryReader aReader)
        {
            return new Color32(aReader.ReadByte(), aReader.ReadByte(), aReader.ReadByte(), aReader.ReadByte());
        }

        public static void WriteMatrix4x4(this BinaryWriter aWriter, Matrix4x4 aMat)
        {
            aWriter.Write(aMat.m00); aWriter.Write(aMat.m01); aWriter.Write(aMat.m02); aWriter.Write(aMat.m03);
            aWriter.Write(aMat.m10); aWriter.Write(aMat.m11); aWriter.Write(aMat.m12); aWriter.Write(aMat.m13);
            aWriter.Write(aMat.m20); aWriter.Write(aMat.m21); aWriter.Write(aMat.m22); aWriter.Write(aMat.m23);
            aWriter.Write(aMat.m30); aWriter.Write(aMat.m31); aWriter.Write(aMat.m32); aWriter.Write(aMat.m33);
        }
        public static Matrix4x4 ReadMatrix4x4(this BinaryReader aReader)
        {
            var m = new Matrix4x4();
            m.m00 = aReader.ReadSingle(); m.m01 = aReader.ReadSingle(); m.m02 = aReader.ReadSingle(); m.m03 = aReader.ReadSingle();
            m.m10 = aReader.ReadSingle(); m.m11 = aReader.ReadSingle(); m.m12 = aReader.ReadSingle(); m.m13 = aReader.ReadSingle();
            m.m20 = aReader.ReadSingle(); m.m21 = aReader.ReadSingle(); m.m22 = aReader.ReadSingle(); m.m23 = aReader.ReadSingle();
            m.m30 = aReader.ReadSingle(); m.m31 = aReader.ReadSingle(); m.m32 = aReader.ReadSingle(); m.m33 = aReader.ReadSingle();
            return m;
        }

        public static void WriteBoneWeight(this BinaryWriter aWriter, BoneWeight aWeight)
        {
            aWriter.Write(aWeight.boneIndex0); aWriter.Write(aWeight.weight0);
            aWriter.Write(aWeight.boneIndex1); aWriter.Write(aWeight.weight1);
            aWriter.Write(aWeight.boneIndex2); aWriter.Write(aWeight.weight2);
            aWriter.Write(aWeight.boneIndex3); aWriter.Write(aWeight.weight3);
        }
        public static BoneWeight ReadBoneWeight(this BinaryReader aReader)
        {
            var w = new BoneWeight();
            w.boneIndex0 = aReader.ReadInt32(); w.weight0 = aReader.ReadSingle();
            w.boneIndex1 = aReader.ReadInt32(); w.weight1 = aReader.ReadSingle();
            w.boneIndex2 = aReader.ReadInt32(); w.weight2 = aReader.ReadSingle();
            w.boneIndex3 = aReader.ReadInt32(); w.weight3 = aReader.ReadSingle();
            return w;
        }
    }
}