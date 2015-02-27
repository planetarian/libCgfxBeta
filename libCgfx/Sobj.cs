using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;

/* SOBJ! Based on docs from 3dbrew along with some original research.
 * Some small updates thanks to additional information from Gericom.
 * This whole thing will be getting HEAVILY refactored soon,
 * so I don't recommend actually using this source code.
 * This file in particular became a bit of a mess. >_>;;
 * -planetarian-
 */

namespace libCgfx
{
    public class Sobj : FlaggedCtrObject
    {
        public bool IsShape { get; private set; }
        public bool IsSkeleton { get; private set; }
        public bool IsMesh { get; private set; }

        public Vector3 OffsetPosition { get; private set; }

        public Offset NameOffset { get; private set; }
        public string Name { get; private set; }

        public int ShapeIndex { get; private set; }
        public int MaterialIndex { get; private set; }
        public Offset OwnerModelOffset { get; private set; }
        public byte IsVisibleRaw { get; private set; }
        public bool IsVisible { get; private set; }
        public byte RenderPriority { get; private set; }
        public short MeshNodeVisibilityIndex { get; private set; }


        public Offset MeshNodeNameOffset { get; private set; }
        public string MeshNodeName { get; internal set; }

        public int FaceGroupCount { get; private set; }
        public Offset FaceGroupOffset { get; private set; }
        public Offset[] FaceGroupOffsets { get; private set; }
        public FaceGroup[] FaceGroups { get; private set; }

        public int VertexGroupCount { get; private set; }
        public Offset VertexGroupOffset { get; private set; }
        public Offset[] VertexGroupOffsets { get; private set; }
        public VertexGroup[] VertexGroups { get; private set; }

        public uint Flags2 { get; private set; }

        // via Gericom
        public Offset OrientedBoundingBoxOffset { get; private set; }
        public OrientedBoundingBox OrientedBoundingBox { get; private set; }

        // via Gericom
        public int BaseAddress { get; private set; }

        // via Gericom
        public Offset BlendShapeOffset { get; private set; }
        public BlendShape BlendShape { get; private set; }

        // Skeleton
        // via Gericom
        public int NumBones { get; private set; }
        public Offset BoneDictOffset { get; private set; }
        public Dict BoneDict { get; private set; }
        public Offset RootBoneOffset { get; private set; }
        public Bone[] Bones { get; private set; }
        public Dictionary<int, Bone> BonesByOffset { get; private set; }
        public SkeletonScalingRule SkeletonScalingRule { get; private set; }
        public SkeletonFlags SkeletonFlags { get; private set; }

        public Sobj(CtrObject parent) : this(parent, parent.CurrentOffset)
        {
        }

        public Sobj(CtrObject parent, int startOffset) : base(parent, startOffset)
        {
            // 0x0
            Flags = ReadUInt32();

            // 0x4
            Magic = ReadMagic();
            Log("Magic '" + Magic + "' for offset " + DisplayValue(StartOffset));
            LogIndent();
            Log("Flags: " + DisplayValue(Flags), 2);
            
            IsShape = IsFlagSet(24 + 4);
            Log("IsShape: " + IsShape, 1);

            IsSkeleton = IsFlagSet(24 + 1);
            Log("IsSkeleton: " + IsSkeleton, 1);

            IsMesh = IsFlagSet(24);
            Log("IsMesh: " + IsMesh, 1);

            // 0x8
            Revision = ReadInt32();
            Log("Revision: " + DisplayValue(Revision));

            // 0xC
            NameOffset = ReadOffset();
            Log("NameOffset " + NameOffset, 2);
            Name = ReadStringTerminated(NameOffset.Absolute);
            Log("Name: '" + Name + "'", 1);

            // 0x10
            Skip(0x8); // Unknown

            if (IsShape)
                ReadShape();
            else if (IsMesh)
                ReadMesh();
            else if (IsSkeleton)
                ReadSkeleton();
            else
                throw new InvalidOperationException(
                    "Unkown SOBJ type with flags " + DisplayValue(Flags));

            LogIndent(false);
        }

        private void ReadShape()
        {

            // 0x18
            Flags2 = ReadUInt32();
            Log("Flags2: " + DisplayValue(Flags2));

            // 0x1C
            OrientedBoundingBoxOffset = ReadOffset();
            Log("OrientedBoundingBoxOffset: " + OrientedBoundingBoxOffset);

            if (OrientedBoundingBoxOffset.Relative != 0)
                OrientedBoundingBox = ReadBoundingBox(OrientedBoundingBoxOffset.Absolute);
            
            // 0x20
            OffsetPosition = ReadVec3();
            Log("OffsetPosition: " + DisplayValue(OffsetPosition));

            // 0x2C
            // Faces/Primitives
            FaceGroupCount = ReadInt32();
            Log("FaceGroupCount: " + DisplayValue(FaceGroupCount));
            // 0x30
            FaceGroupOffset = ReadOffset();
            Log("FaceGroupOffset " + FaceGroupOffset, 2);

            // 0x34
            BaseAddress = ReadInt32();
            Log("BaseAddress: " + DisplayValue(BaseAddress), 1);

            // 0x38
            // Vertex
            VertexGroupCount = ReadInt32();
            Log("VertexGroupCount: " + DisplayValue(VertexGroupCount));

            // 0x3C
            VertexGroupOffset = ReadOffset();
            Log("VertexGroupOffset " + VertexGroupOffset, 2);

            // 0x40
            BlendShapeOffset = ReadOffset();
            Log("BlendShapeOffset " + BlendShapeOffset, 2);

            Log("FaceGroupOffsets at offset " + DisplayValue(CurrentOffset));
            FaceGroupOffsets = new Offset[FaceGroupCount];
            LogIndent();
            for (int i = 0; i < FaceGroupCount; i++)
            {
                // 0x44 + 4*i
                FaceGroupOffsets[i] = ReadOffset();
                Log("Entry " + i + " " + FaceGroupOffsets[i]);
            }
            LogIndent(false);

            Log("VertexGroupOffsets at offset " + DisplayValue(CurrentOffset));
            LogIndent();
            VertexGroupOffsets = new Offset[VertexGroupCount];
            for (int i = 0; i < VertexGroupCount; i++)
            {
                VertexGroupOffsets[i] = ReadOffset();
                Log("Entry " + i + " " + VertexGroupOffsets[i]);
            }
            LogIndent(false);

            Log("FaceGroups:");
            LogIndent();
            FaceGroups = new FaceGroup[FaceGroupCount];
            for (int i = 0; i < FaceGroupCount; i++)
            {
                SkipTo(FaceGroupOffsets[i].Absolute, i > 0);
                Log("FaceGroup " + i + " at offset " + DisplayValue(CurrentOffset));
                LogIndent();

                var fg = new FaceGroup();
                fg.NumBoneGroups = ReadInt32();
                Log("NumBoneGroups: " + DisplayValue(fg.NumBoneGroups));

                fg.BoneGroupsOffset = ReadOffset();
                Log("BoneGroupsOffset " + fg.BoneGroupsOffset);

                fg.SkinningMode = ReadUInt32();
                Log("SkinningMode: " + DisplayValue(fg.SkinningMode));

                fg.NumSubFaceGroups = ReadInt32();
                Log("NumSubFaceGroups: " + DisplayValue(fg.NumSubFaceGroups));

                fg.SubFaceGroupListOffset = ReadOffset();
                Log("SubFaceGroupListOffset " + fg.SubFaceGroupListOffset);

                SkipTo(fg.BoneGroupsOffset.Absolute);

                fg.BoneGroups = new int[fg.NumBoneGroups];
                Log("BoneGroups:");
                LogIndent();
                for (int b = 0; b < fg.NumBoneGroups; b++)
                {
                    fg.BoneGroups[b] = ReadInt32();
                    Log("BoneGroup " + b + ": " + DisplayValue(fg.BoneGroups[b]));
                }
                LogIndent(false);

                SkipTo(fg.SubFaceGroupListOffset);

                Log("SubFaceGroupOffsets:");
                LogIndent();
                fg.SubFaceGroupOffsets = new Offset[fg.NumSubFaceGroups];
                for (int sfgi = 0; sfgi < fg.NumSubFaceGroups; sfgi++)
                {
                    fg.SubFaceGroupOffsets[sfgi] = ReadOffset();
                    Log("SubFaceGroup " + sfgi + " offset " + fg.SubFaceGroupOffsets[sfgi]);
                }
                LogIndent(false);

                fg.SubFaceGroups = new SubFaceGroup[fg.NumSubFaceGroups];
                Log("SubFaceGroups:");
                LogIndent();
                for (int sfgi = 0; sfgi < fg.NumSubFaceGroups; sfgi++)
                {
                    var sfg = new SubFaceGroup();
                    Log("SubFaceGroup " + sfgi + ":");
                    LogIndent();

                    SkipTo(fg.SubFaceGroupOffsets[sfgi]);

                    sfg.NumFaceGroupDescriptors = ReadInt32();
                    Log("NumFaceGroupDescriptors: " + DisplayValue(sfg.NumFaceGroupDescriptors));

                    sfg.FaceGroupDescriptorListOffset = ReadOffset();
                    Log("FaceGroupDescriptorListOffset " + sfg.FaceGroupDescriptorListOffset);

                    sfg.NumBufferObjects = ReadInt32();
                    Log("NumBufferObjects: " + DisplayValue(sfg.NumBufferObjects));
                    sfg.BufferObjectsListOffset = ReadOffset();
                    Log("BufferObjectsListOffset " + sfg.BufferObjectsListOffset);
                    sfg.BufferObjects = new int[sfg.NumBufferObjects];

                    sfg.Flags = ReadUInt32();
                    Log("Flags: " + DisplayValue(sfg.Flags));

                    sfg.CommandAllocator = ReadUInt32();
                    Log("CommandAllocator: " + DisplayValue(sfg.CommandAllocator));

                    SkipTo(sfg.FaceGroupDescriptorListOffset); // should be zero bytes

                    Log("FaceGroupDescriptorOffsets:");
                    LogIndent();
                    sfg.FaceGroupDescriptorOffsets = new Offset[sfg.NumFaceGroupDescriptors];
                    for (int fgdi = 0; fgdi < sfg.NumFaceGroupDescriptors; fgdi++)
                    {
                        sfg.FaceGroupDescriptorOffsets[fgdi] = ReadOffset();
                        Log("FaceGroupDescriptor " + fgdi + " offset " + sfg.FaceGroupDescriptorOffsets[fgdi]);
                    }
                    LogIndent(false);

                    SkipTo(sfg.BufferObjectsListOffset);

                    Log("BufferObjects:");
                    LogIndent();
                    for (int u3 = 0; u3 < sfg.NumBufferObjects; u3++)
                    {
                        sfg.BufferObjects[u3] = ReadInt32();
                        Log("BufferObject " + u3 + ": " + DisplayValue(sfg.BufferObjects[u3]));
                    }
                    LogIndent(false);

                    sfg.FaceGroupDescriptors = new FaceGroupDescriptor[sfg.NumFaceGroupDescriptors];
                    Log("FaceGroupDescriptors:");
                    LogIndent();
                    for (int fgdi = 0; fgdi < sfg.NumFaceGroupDescriptors; fgdi++)
                    {
                        var fgd = new FaceGroupDescriptor();
                        Log("FaceGroupDescriptor " + fgdi + ":");
                        LogIndent();

                        fgd.Flags = ReadByte();
                        Log("Flags: " + DisplayValue(fgd.Flags));

                        fgd.DataType = fgd.Flags.IsBitSet(1)
                            ? VertexIndexDataType.Short
                            : VertexIndexDataType.Byte;
                        Log("DataType: " + fgd.DataType + " [" + DisplayValue((int)fgd.DataType) + "]");

                        Skip(0x3);

                        fgd.PrimitiveMode = ReadByte();
                        Log("PrimitiveMode: " + DisplayValue(fgd.PrimitiveMode));
                        fgd.IsVisibleRaw = ReadByte();
                        fgd.IsVisible = fgd.IsVisibleRaw == 1;
                        Log("IsVisible: " + fgd.IsVisible + " (" + DisplayValue(fgd.IsVisibleRaw) + ")");
                        
                        Skip(0x2); // padding

                        fgd.DataSize = ReadInt32();
                        Log("DataSize: " + DisplayValue(fgd.DataSize));

                        fgd.DataOffset = ReadOffset();
                        Log("DataOffset: " + fgd.DataOffset, 2);

                        fgd.BufferObject = ReadUInt32();
                        Log("BufferObject: " + DisplayValue(fgd.BufferObject));
                        fgd.LocationFlag = ReadUInt32();
                        Log("LocationFlag: " + DisplayValue(fgd.LocationFlag));
                        fgd.CommandCache = ReadUInt32();
                        Log("CommandCache: " + DisplayValue(fgd.CommandCache));
                        fgd.CommandCacheSize = ReadUInt32();
                        Log("CommandCacheSize: " + DisplayValue(fgd.CommandCacheSize));
                        fgd.LocationAddress = ReadUInt32();
                        Log("LocationAddress: " + DisplayValue(fgd.LocationAddress));
                        fgd.MemoryArea = ReadUInt32();
                        Log("MemoryArea: " + DisplayValue(fgd.MemoryArea));
                        fgd.BoundingBoxOffset = ReadOffset();
                        Log("BoundingBoxOffset: " + fgd.BoundingBoxOffset);
                        if (fgd.BoundingBoxOffset.Relative != 0)
                            fgd.BoundingBox = ReadBoundingBox(fgd.BoundingBoxOffset.Absolute);

                        switch (fgd.DataType)
                        {
                            case VertexIndexDataType.Byte:
                                fgd.Data =
                                    Util.For(fgd.DataSize).Select(n => (int)ReadByte(fgd.DataOffset.Absolute + n)).ToArray();
                                break;
                            case VertexIndexDataType.Short:
                                fgd.Data =
                                    Util.For(fgd.DataSize / 2)
                                        .Select(n => (int)ReadUInt16(fgd.DataOffset.Absolute + n * 2))
                                        .ToArray();
                                break;
                            default:
                                throw new InvalidOperationException("Invalid FaceData format " + fgd.DataType);
                        }

                        Log("Data: [" + String.Join(",",
                            fgd.Data.Take(20).Select(n => n.ToString(CultureInfo.InvariantCulture)).ToArray())
                            + (fgd.Data.Length > 20 ? ",..." : "") + "]");

                        LogIndent(false);
                        sfg.FaceGroupDescriptors[fgdi] = fgd;
                    }
                    LogIndent(false);
                    fg.SubFaceGroups[sfgi] = sfg;

                    LogIndent(false);
                }
                LogIndent(false);
                FaceGroups[i] = fg;

                LogIndent(false);
            }
            LogIndent(false);

            Log("VertexGroups:");
            LogIndent();
            VertexGroups = new VertexGroup[VertexGroupCount];
            for (int i = 0; i < VertexGroupCount; i++)
            {
                SkipTo(VertexGroupOffsets[i].Absolute);
                Log("VertexGroup " + i + " offset " + DisplayValue(CurrentOffset) + ":");
                LogIndent();

                var vg = new VertexGroup();

                vg.SobjType = (SobjType)ReadUInt32();
                Log("SobjType: " + vg.SobjType + " [" + DisplayValue((uint)vg.SobjType) + "]");


                if (vg.SobjType == SobjType.One)
                {
                    // Unimplemented format
                    Log("T00: " + (FvfType)(ReadInt32())); // fvfinfo[0]
                    Log("I01: " + DisplayValue(ReadInt32()));
                    Log("I02: " + DisplayValue(ReadInt32()));
                    Log("I03: " + DisplayValue(ReadInt32()));
                    Log("I04 (size?): " + DisplayValue(ReadInt32())); // fvfinfo[4]
                    Log("O05: " + ReadOffset()); // fvfinfo[5]
                    Log("I06: " + DisplayValue(ReadInt32()));
                    Log("I07: " + DisplayValue(ReadInt32()));

                    Log("B08: " + DisplayValue(ReadByte()));
                    Log("B09: " + DisplayValue(ReadByte())); // fvfinfo[8]
                    Log("B10: " + DisplayValue(ReadByte()));
                    Log("B11: " + DisplayValue(ReadByte()));

                    Log("I12: " + DisplayValue(ReadInt32())); // fvfinfo[12]
                    Log("F13: " + DisplayValue(ReadSingle()));
                    Log("I14: " + DisplayValue(ReadInt32())); // fvfinfo[14]
                }
                else if (vg.SobjType == SobjType.Two)
                {
                    
                    Skip(0x10); // Header

                    vg.DataSize = ReadInt32();
                    Log("DataSize: " + DisplayValue(vg.DataSize));
                    vg.DataOffset = ReadOffset();
                    Log("DataOffset " + vg.DataOffset, 2);

                    Skip(0x8); // Header2

                    vg.VertexFormatSize = ReadInt32();
                    Log("VertexFormatSize: " + DisplayValue(vg.VertexFormatSize));
                    vg.FvfCount = ReadInt32();
                    Log("FvfCount: " + DisplayValue(vg.FvfCount));
                    Skip(0x4);

                    vg.Data = ReadBytes(vg.DataOffset.Absolute, vg.DataSize);
                    //Util.For(vg.DataSize).Select(n => ReadSingle(vg.DataOffset.Absolute + (n*4))).ToArray();

                    Log("Data: [" + String.Join(",",
                        vg.Data.Take(20).Select(n => n.ToString(CultureInfo.InvariantCulture)).ToArray())
                        + (vg.Data.Length > 20 ? ",..." : "") + "]");

                    Log("Fvf Offsets:");
                    LogIndent();
                    vg.FvfOffsets = new Offset[vg.FvfCount];
                    for (int f = 0; f < vg.FvfCount; f++)
                    {
                        vg.FvfOffsets[f] = ReadOffset();
                        Log("FvfOffset " + f + " " + vg.FvfOffsets[f]);
                    }
                    LogIndent(false);

                    Log("Fvfs:");
                    LogIndent();
                    vg.Fvfs = new FvfInfo[vg.FvfCount];
                    for (int f = 0; f < vg.FvfCount; f++)
                    {
                        var fi = new FvfInfo { StartOffset = vg.FvfOffsets[f].Absolute };

                        Log("Fvf " + f + " offset " + DisplayValue(fi.StartOffset) + ":");
                        LogIndent();

                        fi.Flags = ReadInt32();
                        Log("Flags: " + DisplayValue(fi.Flags));
                        fi.Type = (FvfType)ReadInt32();
                        Log("Type: " + fi.Type + " [" + DisplayValue((int)fi.Type) + "]");
                        fi.Unknown2 = ReadInt32();
                        Log("U2: " + DisplayValue(fi.Unknown2));
                        fi.Unknown3 = ReadInt32();
                        Log("U3: " + DisplayValue(fi.Unknown3));
                        fi.Unknown4 = ReadInt32();
                        Log("U4: " + DisplayValue(fi.Unknown4));
                        fi.Unknown5 = ReadInt32();
                        Log("U5: " + DisplayValue(fi.Unknown5));
                        fi.Unknown6 = ReadInt32();
                        Log("U6: " + DisplayValue(fi.Unknown6));
                        fi.Unknown7 = ReadInt32();
                        Log("U7: " + DisplayValue(fi.Unknown7));
                        fi.Unknown8 = ReadInt32();
                        Log("U8: " + DisplayValue(fi.Unknown8));

                        fi.DataType = (FvfDataType)ReadByte();
                        Log("DataType: " + fi.DataType + " [" + DisplayValue((byte)fi.DataType) + "]"); // fvfdata[0]
                        fi.DataSize = ReadByte();
                        Log("B10: " + DisplayValue(fi.DataSize));
                        fi.Unknown11 = ReadByte();
                        Log("B11: " + DisplayValue(fi.Unknown11));
                        fi.Unknown12 = ReadByte();
                        Log("B12: " + DisplayValue(fi.Unknown12));

                        fi.NumComponents = ReadInt32();
                        Log("NumComponents: " + DisplayValue(fi.NumComponents)); // fvfdata[1]
                        fi.ValueScale = ReadSingle();
                        Log("ValueScale: " + DisplayValue(fi.ValueScale));
                        fi.Position = ReadInt32();
                        Log("ComponentPosition: " + DisplayValue(fi.Position)); // fvfdata[2]

                        vg.Fvfs[f] = fi;

                        LogIndent(false);
                    }
                    LogIndent(false);

                }
                else
                {
                    Log("I01: " + (FvfType)ReadInt32());
                    Log("I02: " + DisplayValue(ReadInt32()));
                    Log("B03: " + (FvfDataType)ReadByte());
                    Log("B04: " + DisplayValue(ReadByte()));
                    Log("B05: " + DisplayValue(ReadByte()));
                    Log("B06: " + DisplayValue(ReadByte()));
                    int u07 = ReadInt32();
                    Log("U07: " + DisplayValue(u07));
                    Log("U08: " + DisplayValue(ReadInt32()));
                    Log("U09: " + DisplayValue(ReadInt32()));
                    Log("U10: " + DisplayValue(ReadInt32()));
                    for (int c = 0; c < u07; c++)
                    {
                        Log("F11[" + c + "]: " + DisplayValue(ReadSingle()));
                    }
                }


                VertexGroups[i] = vg;
                LogIndent(false);
            }
            LogIndent(false);


            if (BlendShapeOffset.Relative != 0)
            {
                SkipTo(BlendShapeOffset.Absolute);
                Log("BlendShape:");
                LogIndent();
                BlendShape = new BlendShape();
                BlendShape.Unknown1 = ReadUInt32();
                Log("Unknown1: " + DisplayValue(BlendShape.Unknown1), 2);
                BlendShape.Unknown2 = ReadUInt32();
                Log("Unknown2: " + DisplayValue(BlendShape.Unknown2), 2);
                BlendShape.Unknown3 = ReadUInt32();
                Log("Unknown3: " + DisplayValue(BlendShape.Unknown3), 2);
                BlendShape.Unknown4 = ReadUInt32();
                Log("Unknown4: " + DisplayValue(BlendShape.Unknown4), 2);
                BlendShape.Unknown5 = ReadUInt32();
                Log("Unknown5: " + DisplayValue(BlendShape.Unknown5), 2);
                LogIndent(false);
            }


        }

        private void ReadMesh()
        {
            // 0x18
            ShapeIndex = ReadInt32();
            Log("ShapeIndex: " + DisplayValue(ShapeIndex));

            // 0x1C
            MaterialIndex = ReadInt32();
            Log("MaterialIndex: " + DisplayValue(MaterialIndex));
            
            // 0x20
            OwnerModelOffset = ReadOffset();
            Log("OwnerModelOffset: " + OwnerModelOffset);

            // 0x24
            IsVisibleRaw = ReadByte();
            IsVisible = IsVisibleRaw == 1;
            Log("IsVisible: " + IsVisible + " [" + DisplayValue(IsVisibleRaw) + "]");
            // 0x25
            RenderPriority = ReadByte();
            Log("RenderPriority: " + DisplayValue(RenderPriority));
            // 0x26
            MeshNodeVisibilityIndex = ReadInt16();
            Log("MeshNodeVisibilityIndex: " + DisplayValue(MeshNodeVisibilityIndex));

            // 0x28
            Skip(0x8);
            Skip(0x10);
            Skip(0x10);
            Skip(0x10);
            Skip(0x10);

            // 0x70
            MeshNodeNameOffset = ReadOffset();
            Log("MeshNodeNameOffset " + MeshNodeNameOffset, 2);
            MeshNodeName = ReadStringTerminated(MeshNodeNameOffset.Absolute);
            Log("MeshNodeName: '" + MeshNodeName + "'");

            // 0x74
            Skip(0x10);
        }

        private void ReadSkeleton()
        {
            // 0x18
            NumBones = ReadInt32();
            Log("NumBones: " + DisplayValue(NumBones));

            // 0x1C
            BoneDictOffset = ReadOffset();
            Log("BoneDictOffset " + BoneDictOffset);

            // 0x20
            RootBoneOffset = ReadOffset();
            Log("RootBoneOffset " + RootBoneOffset);

            // 0x24
            SkeletonScalingRule = (SkeletonScalingRule)ReadUInt32();
            Log("SkeletonScalingRule: " + SkeletonScalingRule + " [" + DisplayValue((uint)SkeletonScalingRule) + "]");

            // 0x28
            SkeletonFlags = (SkeletonFlags)ReadUInt32();
            Log("SkeletonFlags: " + SkeletonFlags + " [" + DisplayValue((uint)SkeletonFlags) + "]");


            // Bone Dictionary
            Log("BoneDict:");
            SkipTo(BoneDictOffset);
            BoneDict = new Dict(this);


            // Bones
            Log("Bones:");
            SkipTo(RootBoneOffset);
            LogIndent();
            Bones = new Bone[NumBones];
            BonesByOffset = new Dictionary<int, Bone>(NumBones);
            for (int i = 0; i < NumBones; i++)
            {
                Log("Bone " + i + ":");
                LogIndent();

                Offset boneOffset = BoneDict.Entries[i].ValueAsOffset;
                SkipTo(boneOffset);
                var bone = new Bone(this);
                Bones[i] = bone;
                BonesByOffset.Add(bone.StartOffset, bone);

                LogIndent(false);
            }
            for (int i = 0; i < NumBones; i++)
            {
                var bone = Bones[i];
                if (bone.ParentOffset.Relative != 0)
                    bone.Parent = BonesByOffset[bone.ParentOffset.Absolute];
                if (bone.ChildOffset.Relative != 0)
                    bone.Child = BonesByOffset[bone.ChildOffset.Absolute];
                if (bone.PreviousSiblingOffset.Relative != 0)
                    bone.PreviousSibling = BonesByOffset[bone.PreviousSiblingOffset.Absolute];
                if (bone.NextSiblingOffset.Relative != 0)
                    bone.NextSibling = BonesByOffset[bone.NextSiblingOffset.Absolute];
            }
            LogIndent(false);
            
        }

        private OrientedBoundingBox ReadBoundingBox(int offset)
        {
            var obb = new OrientedBoundingBox();
            LogIndent();
            obb.Center = ReadVec3(offset + 0x4);
            Log("Center: " + obb.Center);
            obb.Orientation = ReadMatrix33(offset + 0x8);
            Log("Orientation: " + obb.Orientation);
            obb.Size = ReadVec3(offset + 0x2C);
            Log("Size: " + obb.Size);
            LogIndent(false);
            return obb;
        }

        public Matrix GetBoneMatrix(int jointId)
        {
            var matchingBone = Bones.FirstOrDefault(b => b.JointId == jointId);
            if (matchingBone==null)
                return Matrix.Identity;

            Matrix currentMatrix = matchingBone.GetMatrix();
            Matrix parentMatrix = GetBoneMatrix(matchingBone.ParentId);
            // 3DS matrix multiplication is backwards...
            Matrix result = parentMatrix*currentMatrix;
            return result;
        }
    }


    public class OrientedBoundingBox
    {
        public Vector3 Center;
        public Matrix Orientation;
        public Vector3 Size;
    }

    public class BlendShape
    {
        public uint Unknown1;
        public uint Unknown2;
        public uint Unknown3;
        public uint Unknown4;
        public uint Unknown5;
    }

    public class FaceGroup
    {
        public int NumBoneGroups { get; internal set; }
        public Offset BoneGroupsOffset { get; internal set; }
        public int[] BoneGroups { get; internal set; }
        public Bone[] Bones { get; internal set; }

        // via Gericom
        public uint SkinningMode { get; internal set; }

        public int NumSubFaceGroups { get; internal set; }
        public Offset SubFaceGroupListOffset { get; internal set; }
        public Offset[] SubFaceGroupOffsets { get; internal set; }
        public SubFaceGroup[] SubFaceGroups { get; internal set; }
        

    }

    public class SubFaceGroup
    {
        public int NumFaceGroupDescriptors { get; internal set; }
        public Offset FaceGroupDescriptorListOffset { get; internal set; }
        public Offset[] FaceGroupDescriptorOffsets { get; internal set; }
        public FaceGroupDescriptor[] FaceGroupDescriptors { get; internal set; }

        // via Gericom
        public int NumBufferObjects { get; internal set; }
        public Offset BufferObjectsListOffset { get; internal set; }
        public int[] BufferObjects { get; internal set; }
        public uint Flags { get; internal set; }
        public uint CommandAllocator { get; internal set; }
    }

    public class FaceGroupDescriptor
    {
        public byte Flags { get; internal set; }
        public VertexIndexDataType DataType { get; internal set; }
        
        // via Gericom
        public byte PrimitiveMode { get; internal set; }
        public byte IsVisibleRaw { get; internal set; }
        public bool IsVisible { get; internal set; }

        public int DataSize { get; internal set; }
        public Offset DataOffset { get; internal set; }
        public int[] Data { get; internal set; }

        // via Gericom
        public uint BufferObject { get; internal set; }
        public uint LocationFlag { get; internal set; }
        public uint CommandCache { get; internal set; }
        public uint CommandCacheSize { get; internal set; }
        public uint LocationAddress { get; internal set; }
        public uint MemoryArea { get; internal set; }
        public Offset BoundingBoxOffset { get; internal set; }
        public OrientedBoundingBox BoundingBox { get; internal set; }
    }

    public class VertexGroup
    {
        public SobjType SobjType { get; internal set; }
        public int DataSize { get; internal set; }
        public Offset DataOffset { get; internal set; }
        public int VertexFormatSize { get; internal set; }
        public int FvfCount { get; internal set; }
        public Offset[] FvfOffsets { get; internal set; }
        public FvfInfo[] Fvfs { get; internal set; }
        public byte[] Data { get; internal set; }
        public Vertex[] Vertices { get; internal set; }
    }

    public class FvfInfo
    {
        public int StartOffset { get; internal set; }
        public int Flags { get; internal set; }
        public FvfType Type { get; internal set; }
        public int Unknown2 { get; internal set; }
        public int Unknown3 { get; internal set; }
        public int Unknown4 { get; internal set; }
        public int Unknown5 { get; internal set; }
        public int Unknown6 { get; internal set; }
        public int Unknown7 { get; internal set; }
        public int Unknown8 { get; internal set; }

        public FvfDataType DataType { get; internal set; }
        public byte DataSize { get; internal set; }
        public byte Unknown11 { get; internal set; }
        public byte Unknown12 { get; internal set; }

        public int NumComponents { get; internal set; }
        public float ValueScale { get; internal set; }
        public int Position { get; internal set; }
    }

    public enum SobjType : uint
    {
        One = 0x40000001,
        Two = 0x40000002,
        Unknown = 0x80000000
    }

    public enum VertexIndexDataType
    {
        Byte = 0x00,
        Short = 0x01
    }

    public enum FvfDataType : byte
    {
        SByte = 0x00,
        Byte = 0x01,
        Short = 0x02,
        UShort = 0x03,
        Int = 0x04,
        UInt = 0x05,
        Float = 0x06
    }

    public enum FvfType : byte
    {
        Position = 0x00,
        Normal,
        Tangent,
        Color0,
        Uv0,
        Uv1,
        Uv2,
        BoneIndex,
        BoneWeight,
        User0,
        User1,
        User2,
        User3,
        User4,
        User5,
        User6,
        User7,
        User8,
        User9,
        User10,
        User11,
        Interleave,
        Quantity
    }


    // via Gericom

    public enum SkeletonScalingRule : uint
    {
        Standard = 0,
        Maya = 1,
        Softimage = 2
    }

    public enum SkeletonFlags : uint
    {
        IsModelCoordinate = 1,
        IsTranslateAnimationEnabled = 2
    }
}
