using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

/* CGFX! Based on docs from 3dbrew along with some original research.
 * This whole thing will be getting HEAVILY refactored soon,
 * so I don't recommend actually using this source code.
 * -planetarian-
 */

namespace libCgfx
{
    public class Cgfx : CtrObject
    {
        public int NumEntries { get; private set; }

        public SizedCtrObject[] Entries { get; private set; }
        public ushort Endianness { get; private set; }
        public int HeaderSize { get; private set; }
        public int Version { get; private set; }
        public int FileSize { get; private set; }

        public List<Cmdl> CmdlObjects { get; private set; }
        public Model[] Models { get; private set; }
        public Dictionary<string, Texture> Textures { get; private set; }

        public Coverage Coverage { get; set; } = new Coverage();

        public Cgfx(string inputFilename, Action<string, int, int> logCallbackAction)
            : base(inputFilename, logCallbackAction)
        {
            RootObject = this;

            CmdlObjects = new List<Cmdl>();
            Textures = new Dictionary<string, Texture>();

            int signature = ReadInt32(false);
            if (signature == 0x0E) // senran kagura header
                Skip(0x80); // TODO: figure this out better. low-prio

            Magic = ReadMagic();
            Log("Magic '" + Magic + "' offset " + DisplayValue(StartOffset));

            Endianness = ReadUInt16();
            Log("Endianness: " + BitConverter.ToString(BitConverter.GetBytes(Endianness)));

            HeaderSize = ReadInt16();
            Log("HeaderSize: " + DisplayValue(HeaderSize));

            Version = ReadInt32();
            Log("Version: " + DisplayValue(Version));

            FileSize = ReadInt32();
            Log("FileSize: " + DisplayValue(FileSize));
            
            NumEntries = ReadInt32();
            Log("Entries: " + DisplayValue(NumEntries));

            Entries = new SizedCtrObject[NumEntries];

            for (int i = 0; i < NumEntries; i++)
            {
                LogBreak();
                CtrObject ctrObject;
                try
                {
                    ctrObject = ReadCtrObject();
                }
                catch (NotImplementedException ex)
                {
                    Log(ex.Message);
                    break;
                }

                var sizedCtrObject = ctrObject as SizedCtrObject;
                if (sizedCtrObject == null)
                    throw new InvalidOperationException(
                        "CGFX contains object without size specification.");

                Entries[i] = sizedCtrObject;

                SkipTo(sizedCtrObject.StartOffset + sizedCtrObject.Size, false);
            }

            LogBreak();

            GenerateModels();


        }

        private void GenerateModels()
        {

            //var dict = new Dict(this, 0xf8e8);
            int modelsCount = CmdlObjects.Count;
            Models = new Model[modelsCount];

            // Models
            for (int m = 0; m < modelsCount; m++)
            {
                Cmdl cmdl = CmdlObjects[m];

                var mtobs = new Mtob[0];
                if (cmdl.MtobDict != null)
                mtobs = cmdl.MtobDict.Entries.Select(e => (Mtob) e.ValueObject).ToArray();

                var model = new Model
                {
                    SkeletonSobj = cmdl.SkeletonSobj,
                    Name = cmdl.Name,
                    Meshes = new Mesh[cmdl.VertexInfoSobjEntries],
                    Materials = new Material[mtobs.Length]
                };

                for (int i = 0; i < mtobs.Length; i++)
                {
                    Mtob mtob = mtobs[i];
                    var mt = new Material
                    {
                        Name = mtob.Name,
                        UseTranslucency = mtob.TranslucencyKind == 1
                    };
                    if (mtob.TextureInfo0 != null)
                        mt.TextureName = mtobs[i].TextureInfo0.Txob.ReferencedName;
                    model.Materials[i] = mt;
                }

                // Meshes
                Sobj[] meshSobjs = cmdl.VertexInfoSobjItems;
                Sobj[] shapeSobjs = cmdl.VertexSobjItems;
                for (int s = 0; s < cmdl.VertexInfoSobjEntries; s++)
                {

                    //if (s != 1)
                        //continue;


                    Sobj meshSobj = meshSobjs[s];
                    Sobj shapeSobj = shapeSobjs[meshSobj.ShapeIndex];
                    VertexGroup vg = shapeSobj.VertexGroups[0];
                    int numVertices = vg.DataSize / vg.VertexFormatSize;

                    var mesh = new Mesh
                    {
                        Name = meshSobj.MeshNodeName,
                        ShapeIndex = meshSobj.ShapeIndex,
                        MaterialIndex = meshSobj.MaterialIndex,
                        IsVisible = meshSobj.IsVisible,
                        RenderPriority = meshSobj.RenderPriority,
                        MeshNodeVisibilityIndex = meshSobj.MeshNodeVisibilityIndex,
                        BoundingBox = shapeSobj.OrientedBoundingBox,
                        Vertices = new Vertex[numVertices]
                    };

                    int faceCount =
                        shapeSobj.FaceGroups.Sum(
                        fg => fg.SubFaceGroups.Sum(sfg => sfg.FaceGroupDescriptors.Sum(fgd => fgd.Data.Length)));
                    var faces = new int[faceCount];

                    int total = 0;
                    int fgs = 0;
                    foreach (FaceGroup fg in shapeSobj.FaceGroups)
                    {


                        //if (fgs++ != 0)
                            //continue;


                        // Big fucking TODO here:
                        // Don't apply the bone manipulations yet!
                        // This makes it much harder (if not impossible) to animate later.
                        Matrix boneMatrix = Matrix.Identity;
                        if (fg.BoneGroups != null)
                        {
                            Bone[] bones = fg.BoneGroups.Select(g => cmdl.SkeletonSobj.Bones[g]).ToArray();
                            if (fg.BoneGroups.Length == 1)
                                boneMatrix = boneMatrix*bones[0].GetMatrix();
                        }

                        // Now let's work on building the faces.
                        int[] indices = fg.SubFaceGroups.SelectMany(
                            sfg => sfg.FaceGroupDescriptors.SelectMany(fgd => fgd.Data)).ToArray();

                        for (int i = 0; i < indices.Length; i++, total++)
                        {
                            int index = indices[i];
                            if (mesh.Vertices[index] == null)
                            {

                                int componentPosition = index*vg.VertexFormatSize;
                                var vertex = new Vertex();
                                // Vertex components
                                for (int f = 0; f < vg.FvfCount; f++)
                                {
                                    FvfInfo fvf = vg.Fvfs[f];
                                    switch (fvf.Type)
                                    {
                                        case FvfType.Position:
                                            mesh.HasPosition = true;
                                            vertex.X = ReadValue(vg.Data, fvf, ref componentPosition) +
                                                       shapeSobj.OffsetPosition.X;
                                            vertex.Y = ReadValue(vg.Data, fvf, ref componentPosition) +
                                                       shapeSobj.OffsetPosition.Y;
                                            vertex.Z = ReadValue(vg.Data, fvf, ref componentPosition) +
                                                       shapeSobj.OffsetPosition.Z;
                                            if (fg.NumBoneGroups == 1)
                                                vertex.ApplyMatrix(boneMatrix);

                                            // Converts left-handed mesh vertices to right-handed.
                                            // Don't re-enable this until after the entire spec is implemented.
                                            //vertex.Z = -vertex.Z;
                                            break;
                                        case FvfType.Uv0:
                                            mesh.HasUv0 = true;
                                            vertex.U0 = ReadValue(vg.Data, fvf, ref componentPosition);
                                            vertex.V0 = ReadValue(vg.Data, fvf, ref componentPosition);
                                            break;
                                        case FvfType.Uv1:
                                            mesh.HasUv1 = true;
                                            vertex.U1 = ReadValue(vg.Data, fvf, ref componentPosition);
                                            vertex.V1 = ReadValue(vg.Data, fvf, ref componentPosition);
                                            break;
                                        case FvfType.Normal:
                                            mesh.HasNormals = true;
                                            vertex.Nx = ReadValue(vg.Data, fvf, ref componentPosition);
                                            vertex.Ny = ReadValue(vg.Data, fvf, ref componentPosition);
                                            vertex.Nz = ReadValue(vg.Data, fvf, ref componentPosition);
                                            // Converts left-handed mesh normals to right-handed.
                                            // Don't re-enable this until after the entire spec is implemented.
                                            //vertex.Nz = -vertex.Nz;
                                            break;
                                        case FvfType.Color0:
                                            mesh.HasColor0 = true;
                                            vertex.R = ReadValue(vg.Data, fvf, ref componentPosition);
                                            vertex.G = ReadValue(vg.Data, fvf, ref componentPosition);
                                            vertex.B = ReadValue(vg.Data, fvf, ref componentPosition);
                                            vertex.A = ReadValue(vg.Data, fvf, ref componentPosition);
                                            break;
                                        case FvfType.BoneIndex:
                                            for (int c = 0; c < fvf.NumComponents; c++)
                                            {
                                                byte boneGroupIndex = ReadRawBytes(vg.Data, fvf, ref componentPosition)[0];
                                                if (fvf.NumComponents > 1) continue;

                                                if (fg.BoneGroups != null)
                                                {
                                                    int boneIndex = fg.BoneGroups[boneGroupIndex];
                                                    Bone bone = cmdl.SkeletonSobj.Bones[boneIndex];
                                                    Matrix boneMatrix2 = bone.GetMatrix();
                                                    vertex.ApplyMatrix(boneMatrix2);
                                                }
                                            }
                                            break;
                                        default:
                                            // Skip it for now.
                                            // TODO: implement everything else =x
                                            componentPosition += GetFvfDataTypeLength(fvf.DataType) * fvf.NumComponents;
                                            //throw new NotImplementedException("FvfType not implemented: " + fvf.Type);
                                            break;
                                    }
                                }

                                // Sanity check
                                if (Single.IsNaN(vertex.X) || Single.IsNaN(vertex.Y) || Single.IsNaN(vertex.Z) ||
                                    Single.IsNaN(vertex.U0) || Single.IsNaN(vertex.V0))
                                {
                                    throw new InvalidOperationException("NaN discovered at vertex " + index);
                                }
                                mesh.Vertices[index] = vertex;
                            }
                            faces[total] = index;
                        }
                    }

                    mesh.Faces = faces;
                    /*
                    // This reverses the face winding so that polygons face the correct direction
                    // after Z-reversal/handedness-conversion. i.e 0,1,2 -> 2,1,0
                    // Don't re-enable this until after the entire spec is implemented.
                    mesh.Faces = new int[faceCount];
                    for (int i = 0; false && i < faces.Length; i++)
                    {
                        int mod = (i % 3) * 2 - 2;
                        int v = i - mod;
                        mesh.Faces[v] = faces[i];
                    }//*/

                    model.Meshes[s] = mesh;
                }

                Models[m] = model;
            }

            Log("Models discovered: " + modelsCount);
        }


        internal int GetFvfDataTypeLength(FvfDataType dataType)
        {
            switch (dataType)
            {
                case FvfDataType.SByte:
                case FvfDataType.Byte:
                    return 1;
                case FvfDataType.Short:
                case FvfDataType.UShort:
                    return 2;
                case FvfDataType.Int:
                case FvfDataType.UInt:
                case FvfDataType.Float:
                    return 4;
                default:
                    throw new NotImplementedException("Unknown FvfDataType: " + dataType);
            }
        }

        internal byte[] ReadRawBytes(byte[] data, FvfInfo fvf, ref int componentPosition)
        {
            int vLen = GetFvfDataTypeLength(fvf.DataType);
            var rawBytes = new byte[vLen];
            Buffer.BlockCopy(data, componentPosition, rawBytes, 0, vLen);
            componentPosition += vLen;

            return rawBytes;
        }

        internal float ReadValue(byte[] data, FvfInfo fvf, ref int componentPosition)
        {
            int rawValue;
            return ReadValue(data, fvf, ref componentPosition, out rawValue);
        }

        internal float ReadValue(byte[] data, FvfInfo fvf, ref int componentPosition, out int rawValue)
        {
            float result;

            byte[] rawBytes = ReadRawBytes(data, fvf, ref componentPosition);

            switch (fvf.DataType)
            {
                case FvfDataType.SByte:
                    result = (sbyte)rawBytes[0];
                    break;
                case FvfDataType.Byte:
                    result = rawBytes[0];
                    break;
                case FvfDataType.Short:
                    result = BitConverter.ToInt16(rawBytes, 0);
                    break;
                case FvfDataType.UShort:
                    result = BitConverter.ToUInt16(rawBytes, 0);
                    break;
                case FvfDataType.Int:
                    result = BitConverter.ToInt32(rawBytes, 0);
                    break;
                case FvfDataType.UInt:
                    result = BitConverter.ToUInt32(rawBytes, 0);
                    break;
                case FvfDataType.Float:
                    result = BitConverter.ToSingle(rawBytes, 0);
                    break;
                default:
                    throw new NotImplementedException("Unknown FvfDataType " + fvf.DataType);
            }
            rawValue = (int)result;
            result *= fvf.ValueScale;
            return result;
        }
    }

    public class Model
    {
        public string Name { get; internal set; }
        public Mesh[] Meshes { get; internal set; }
        public Material[] Materials { get; internal set; }

        public Sobj SkeletonSobj { get; internal set; }
    }

    
    public class Mesh
    {
        public string Name { get; internal set; }
        public int ShapeIndex { get; internal set; }
        public int MaterialIndex { get; internal set; }
        public bool IsVisible { get; internal set; }
        public byte RenderPriority { get; internal set; }
        public short MeshNodeVisibilityIndex { get; internal set; }

        public int[] Faces { get; internal set; }
        public Vertex[] Vertices { get; internal set; }

        public OrientedBoundingBox BoundingBox { get; internal set; }

        public bool HasPosition { get; internal set; }
        public bool HasNormals { get; internal set; }
        public bool HasUv0 { get; internal set; }
        public bool HasUv1 { get; internal set; }
        public bool HasColor0 { get; internal set; }
        public bool HasWeight { get; internal set; }
        public bool HasIndex { get; internal set; }

        public override string ToString()
        {
            return "[" + String.Join(",", Vertices.Take(5)
                .Select(v => v.ToString()).ToArray()) + "]";
        }
    }

    public class Shape
    {
        
    }

    public class Material
    {
        public string Name { get; internal set; }
        public bool UseTranslucency { get; internal set; }
        public string TextureName { get; internal set; }
    }

    public class Vertex
    {
        public int FormatSize { get; internal set; }

        public Vector3 Position
        {
            get { return new Vector3(X, Y, Z); }
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public float X { get; internal set; }
        public float Y { get; internal set; }
        public float Z { get; internal set; }

        public float U0 { get; internal set; }
        public float V0 { get; internal set; }
        public float U1 { get; internal set; }
        public float V1 { get; internal set; }

        public float Nx { get; internal set; }
        public float Ny { get; internal set; }
        public float Nz { get; internal set; }

        public float R { get; internal set; }
        public float G { get; internal set; }
        public float B { get; internal set; }
        public float A { get; internal set; }

        public UInt32 Unk1 { get; internal set; }
        public UInt32 Unk2 { get; internal set; }

        public byte[] BoneIDs { get; internal set; }
        public byte[] BoneWeights { get; internal set; }

        public void ApplyMatrix(Matrix m)
        {
            float x = m.M11*X + m.M12*Y + m.M13*Z + m.M14;
            float y = m.M21*X + m.M22*Y + m.M23*Z + m.M24;
            float z = m.M31*X + m.M32*Y + m.M33*Z + m.M34;
            X = x;
            Y = y;
            Z = z;
        }

        public static implicit operator Vector3(Vertex vert)
        {
            return new Vector3(vert.X, vert.Y, vert.Z);
        }

        public override string ToString()
        {
            return "[" + X + "," + Y + "," + Z + "," + U0 + "," + V0 + "]";
        }
    }
}
