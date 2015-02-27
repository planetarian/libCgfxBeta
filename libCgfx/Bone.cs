using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

/*
 * The data structures in this file have been
 * largely ported from EveryFileExplorer.
 * 
 * Credit goes to Gericom for figuring this stuff out =)
 * -planetarian-
 */

namespace libCgfx
{
    public class Bone : CtrObject
    {
        public Offset NameOffset { get; private set; }
        public string Name { get; private set; }

        public BoneFlags Flags { get; private set; }

        public uint JointId { get; private set; }

        public int ParentId { get; private set; }
        public Offset ParentOffset { get; private set; }
        public Bone Parent { get; internal set; }

        public Offset ChildOffset { get; private set; }
        public Bone Child { get; internal set; }

        public Offset PreviousSiblingOffset { get; private set; }
        public Bone PreviousSibling { get; internal set; }

        public Offset NextSiblingOffset { get; private set; }
        public Bone NextSibling { get; internal set; }

        public Vector3 Scale { get; private set; }
        public Vector3 Rotation { get; private set; }
        public Vector3 Translation { get; private set; }

        public Matrix LocalMatrix { get; private set; }
        public Matrix WorldMatrix { get; private set; }
        public Matrix InverseBaseMatrix { get; private set; }

        public BillboardMode BillboardMode { get; private set; }

        public uint Unknown1 { get; private set; }
        public uint Unknown2 { get; private set; }

        public Bone(CtrObject parent) : this(parent, parent.CurrentOffset) { }
        public Bone(CtrObject parent, Offset offset) : this(parent, offset.Absolute) { }
        public Bone(CtrObject parent, int offset) : base(parent, offset)
        {
            NameOffset = ReadOffset();
            Log("NameOffset " + NameOffset);
            Name = ReadStringTerminated(NameOffset.Absolute);
            Log("Name: '" + Name + "'");

            Flags = (BoneFlags) ReadUInt32();
            Log("Flags:");
            LogIndent();
            Flags.GetIndividualFlags().ForEach(Log);
            LogIndent(false);

            JointId = ReadUInt32();
            Log("JointId: " + DisplayValue(JointId));
            ParentId = ReadInt32();
            Log("ParentId: " + DisplayValue(ParentId));

            ParentOffset = ReadOffset();
            Log("ParentOffset " + ParentOffset);
            ChildOffset = ReadOffset();
            Log("ChildOffset " + ChildOffset);
            PreviousSiblingOffset = ReadOffset();
            Log("PreviousSiblingOffset " + PreviousSiblingOffset);
            NextSiblingOffset = ReadOffset();
            Log("NextSiblingOffset " + NextSiblingOffset);

            Scale = ReadVec3();
            Log("Scale: " + Scale);
            Rotation = ReadVec3();
            Log("Rotation: " + Rotation);
            Translation = ReadVec3();
            Log("Translation: " + Translation);

            LocalMatrix = ReadMatrix43();
            Log("LocalMatrix: " + LocalMatrix);
            WorldMatrix = ReadMatrix43();
            Log("WorldMatrix: " + WorldMatrix);
            InverseBaseMatrix = ReadMatrix43();
            Log("InverseBaseMatrix: " + InverseBaseMatrix);

            BillboardMode = (BillboardMode) ReadUInt32();
            Log("BillboardMode: " + BillboardMode + " [" + DisplayValue((uint) BillboardMode) + "]");

            Unknown1 = ReadUInt32();
            Log("Unknown1: " + Unknown1);
            Unknown2 = ReadUInt32();
            Log("Unknown2: " + Unknown2);
        }

        public Vector3 GetTranslation()
        {
            if (Parent == null)
                return GetCalculatedPosition();

            return GetCalculatedPosition() + Parent.GetTranslation();
        }

        public Vector3 GetCalculatedPosition()
        {
            Quaternion rotation = Rotation.ToRotationQuaternion();
            Vector3 final = Vector3.Transform(Translation, rotation);
            return final;
        }

        public Matrix GetMatrix()
        {
            Matrix mtx = Matrix.Identity;

            var rxSin = (float)Math.Sin(Rotation.X);
            var rxCos = (float)Math.Cos(Rotation.X);
            var rySin = (float)Math.Sin(Rotation.Y);
            var ryCos = (float)Math.Cos(Rotation.Y);
            var rzSin = (float)Math.Sin(Rotation.Z);
            var rzCos = (float)Math.Cos(Rotation.Z);
            mtx[2, 0] = -rySin;
            mtx[0, 0] = rzCos * ryCos;
            mtx[1, 0] = rzSin * ryCos;
            mtx[2, 1] = ryCos * rxSin;
            mtx[2, 2] = ryCos * rxCos;
            mtx[0, 1] = (rxSin * rzCos * rySin) - rxCos * rzSin;
            mtx[1, 2] = (rxCos * rzSin * rySin) - rxSin * rzCos;
            mtx[0, 2] = (rxCos * rzCos * rySin) + rxSin * rzSin;
            mtx[1, 1] = (rxSin * rzSin * rySin) + rxCos * rzCos;

            mtx.M11 *= Scale.X;
            mtx.M12 *= Scale.Y;
            mtx.M13 *= Scale.Z;
            mtx.M21 *= Scale.X;
            mtx.M22 *= Scale.Y;
            mtx.M23 *= Scale.Z;
            mtx.M31 *= Scale.X;
            mtx.M32 *= Scale.Y;
            mtx.M33 *= Scale.Z;

            mtx[0, 3] = Translation.X;
            mtx[1, 3] = Translation.Y;
            mtx[2, 3] = Translation.Z;

            if (Parent != null)
                mtx = Parent.GetMatrix() * mtx;

            return mtx;
        }
    }

    // via Gericom
    [Flags]
    public enum BoneFlags : uint
    {
        IsIdentity = 1 << 0,
        IsTranslateZero = 1 << 1,
        IsRotateZero = 1 << 2,
        IsScaleOne = 1 << 3,
        IsUniformScale = 1 << 4,
        IsSegmentScaleCompensate = 1 << 5,
        IsNeedRendering = 1 << 6,
        IsLocalMatrixCalculate = 1 << 7,
        IsWorldMatrixCalculate = 1 << 8,
        HasSkinningMatrix = 1 << 9
    }

    public enum BillboardMode : uint
    {
        Off,
        World,
        WorldViewpoint,
        Screen,
        ScreenViewpoint,
        YAxial,
        YAxialViewpoint
    }
}
