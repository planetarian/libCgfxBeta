using System;
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
    public class Mtob : FlaggedCtrObject
    {
        public Offset NameOffset { get; private set; }
        public string Name { get; private set; }

        public int UnknownI2 { get; private set; }
        public int UnknownI3 { get; private set; }
        public int Flags2 { get; private set; }
        public int TexCoordConfig { get; private set; }
        public int TranslucencyKind { get; private set; }

        public MaterialColor MaterialColor { get; private set; }
        public Rasterization Rasterization { get; private set; }
        public FragmentOperation FragmentOperation { get; private set; }

        public int ActiveTextureCoordinatorCount { get; private set; }
        public TextureCoordinator[] TextureCoordinators { get; private set; }

        public Offset TexMapper0Offset { get; private set; }
        public Offset TexMapper1Offset { get; private set; }
        public Offset TexMapper2Offset { get; private set; }
        public Offset ProcTexMapperOffset { get; private set; }

        public TextureInfo TextureInfo0 { get; private set; }
        public TextureInfo TextureInfo1 { get; private set; }
        public TextureInfo TextureInfo2 { get; private set; }
        public TextureInfo ProcTextureInfo { get; private set; }

        public Offset ShaderOffset { get; private set; }
        public Offset FragmentShaderOffset { get; private set; }
        
        public Mtob(CtrObject parent) : this(parent, parent.CurrentOffset)
        {
        }
        public Mtob(CtrObject parent, int startOffset) : base(parent, startOffset)
        {
            Flags = ReadUInt32();

            Magic = ReadMagic();
            if (Magic != "MTOB")
                throw new Exception("Expected magic 'MTOB', found '" + Magic + "'");
            Log("Magic '" + Magic + "' for offset " + DisplayValue(StartOffset));
            LogIndent();

            Log("Flags: " + DisplayValue(Flags), 2);

            Revision = ReadInt32();
            Log("Revision: " + DisplayValue(Revision));

            NameOffset = ReadOffset();
            Log("Symbol1Offset: " + NameOffset, 2);

            Name = ReadStringTerminated(NameOffset.Absolute);
            Log("Name: '" + Name + "'");

            UnknownI2 = ReadInt32();
            Log("UnknownI2: " + DisplayValue(UnknownI2));
            UnknownI3 = ReadInt32();
            Log("UnknownI3: " + DisplayValue(UnknownI3));

            Flags2 = ReadInt32();
            Log("Flags2: " + DisplayValue(Flags2));
            TexCoordConfig = ReadInt32();
            Log("TexCoordConfig: " + DisplayValue(TexCoordConfig));
            TranslucencyKind = ReadInt32();
            Log("TranslucencyKind: " + DisplayValue(TranslucencyKind));

            MaterialColor = ReadMaterialColor();
            Rasterization = ReadRasterization();
            FragmentOperation = ReadFragmentOperation();

            ActiveTextureCoordinatorCount = ReadInt32();
            Log("ActiveTextureCoordinatorCount: " + DisplayValue(ActiveTextureCoordinatorCount));

            TextureCoordinators = new TextureCoordinator[3];
            TextureCoordinators[0] = ReadTextureCoordinator();
            TextureCoordinators[1] = ReadTextureCoordinator();
            TextureCoordinators[2] = ReadTextureCoordinator();

            TexMapper0Offset = ReadOffset();
            Log("TexMapper0Offset: " + TexMapper0Offset);
            TexMapper1Offset = ReadOffset();
            Log("TexMapper1Offset: " + TexMapper1Offset);
            TexMapper2Offset = ReadOffset();
            Log("TexMapper2Offset: " + TexMapper2Offset);
            ProcTexMapperOffset = ReadOffset();
            Log("ProcTexMapperOffset: " + ProcTexMapperOffset);

            ShaderOffset = ReadOffset();
            Log("ShaderOffset: " + ShaderOffset);
            FragmentShaderOffset = ReadOffset();
            Log("FragmentShaderOffset: " + FragmentShaderOffset);

            if (TexMapper0Offset.Relative != 0)
            {
                SkipTo(TexMapper0Offset);
                TextureInfo0 = ReadTextureInfo();
                Log("Texture0: " + TextureInfo0.TxobOffset);
            }
            if (TexMapper1Offset.Relative != 0)
            {
                SkipTo(TexMapper1Offset);
                TextureInfo1 = ReadTextureInfo();
                Log("Texture1: " + TextureInfo1.TxobOffset);
            }
            if (TexMapper2Offset.Relative != 0)
            {
                SkipTo(TexMapper2Offset);
                TextureInfo2 = ReadTextureInfo();
                Log("Texture2: " + TextureInfo2.TxobOffset);
            }
            if (ProcTexMapperOffset.Relative != 0)
            {
                SkipTo(ProcTexMapperOffset);
                ProcTextureInfo = ReadTextureInfo();
                Log("ProcTexture: " + ProcTextureInfo.TxobOffset);
            }

            LogIndent(false);
        }

        private MaterialColor ReadMaterialColor()
        {
            var mc = new MaterialColor();
            mc.Emission = ReadVec4();
            mc.Ambient = ReadVec4();
            mc.Diffuse = ReadVec4();
            mc.Specular0 = ReadVec4();
            mc.Specular1 = ReadVec4();
            mc.Constant0 = ReadVec4();
            mc.Constant1 = ReadVec4();
            mc.Constant2 = ReadVec4();
            mc.Constant3 = ReadVec4();
            mc.Constant4 = ReadVec4();
            mc.Constant5 = ReadVec4();

            mc.EmissionU32 = ReadUInt32();
            mc.AmbientU32 = ReadUInt32();
            mc.DiffuseU32 = ReadUInt32();
            mc.Specular0U32 = ReadUInt32();
            mc.Specular1U32 = ReadUInt32();
            mc.Constant0U32 = ReadUInt32();
            mc.Constant1U32 = ReadUInt32();
            mc.Constant2U32 = ReadUInt32();
            mc.Constant3U32 = ReadUInt32();
            mc.Constant4U32 = ReadUInt32();
            mc.Constant5U32 = ReadUInt32();

            mc.CommandCache = ReadUInt32();

            return mc;
        }

        private Rasterization ReadRasterization()
        {
            var rast = new Rasterization();

            rast.Flags = ReadUInt32();
            rast.CullingMode = ReadUInt32();
            rast.PolygonOffsetUnit = ReadSingle();
            rast.Command1 = ReadUInt32();
            rast.Command2 = ReadUInt32();
            
            return rast;
        }

        private FragmentOperation ReadFragmentOperation()
        {
            var f = new FragmentOperation();
            f.DepthFlags = ReadUInt32();
            f.DepthCmd1 = ReadUInt32();
            f.DepthCmd2 = ReadUInt32();
            f.DepthCmd3 = ReadUInt32();
            f.DepthCmd4 = ReadUInt32();

            f.BlendMode = ReadUInt32();
            f.BlendColor = ReadVec4();
            f.BlendCmd1 = ReadUInt32();
            f.BlendCmd2 = ReadUInt32();
            f.BlendCmd3 = ReadUInt32();
            f.BlendCmd4 = ReadUInt32();
            f.BlendCmd5 = ReadUInt32();
            f.BlendCmd6 = ReadUInt32();

            f.StencilCmd1 = ReadUInt32();
            f.StencilCmd2 = ReadUInt32();
            f.StencilCmd3 = ReadUInt32();
            f.StencilCmd4 = ReadUInt32();

            return f;
        }

        private TextureCoordinator ReadTextureCoordinator()
        {
            var tc = new TextureCoordinator();

            tc.SourceCoordinate = ReadUInt32();
            tc.MappingMethod = ReadUInt32();
            tc.ReferenceCamera = ReadInt32();
            tc.MatrixMode = ReadUInt32();
            tc.Scale = ReadVec2();
            tc.Rotation = ReadSingle();
            tc.Translation = ReadVec2();
            tc.Unknown = ReadUInt32();
            tc.Matrix = ReadMatrix43();

            return tc;
        }

        private TextureInfo ReadTextureInfo()
        {
            var ti = new TextureInfo();

            ti.Type = ReadUInt32();
            ti.DynamicAllocator = ReadUInt32();
            ti.TxobOffset = ReadOffset();
            ti.SamplerOffset = ReadOffset();

            SkipTo(ti.TxobOffset);
            ti.Txob = (Txob)ReadCtrObject();

            return ti;
        }
    }

    public class MaterialColor
    {
        public Vector4 Emission;//R,G,B,A singles
        public Vector4 Ambient;//and vertex color scale
        public Vector4 Diffuse;
        public Vector4 Specular0;
        public Vector4 Specular1;
        public Vector4 Constant0;
        public Vector4 Constant1;
        public Vector4 Constant2;
        public Vector4 Constant3;
        public Vector4 Constant4;
        public Vector4 Constant5;
        
        public uint EmissionU32;//U32
        public uint AmbientU32;
        public uint DiffuseU32;
        public uint Specular0U32;
        public uint Specular1U32;
        public uint Constant0U32;
        public uint Constant1U32;
        public uint Constant2U32;
        public uint Constant3U32;
        public uint Constant4U32;
        public uint Constant5U32;

        public uint CommandCache;
    }

    public class Rasterization
    {
        public uint Flags;
        public uint CullingMode;
        public Single PolygonOffsetUnit;
        public uint Command1;
        public uint Command2;
    }

    public class FragmentOperation
    {
        public uint DepthFlags;
        public uint DepthCmd1;
        public uint DepthCmd2;
        public uint DepthCmd3;
        public uint DepthCmd4;

        public uint BlendMode;
        public Vector4 BlendColor;
        public uint BlendCmd1;
        public uint BlendCmd2;
        public uint BlendCmd3;
        public uint BlendCmd4;
        public uint BlendCmd5;
        public uint BlendCmd6;

        public uint StencilCmd1;
        public uint StencilCmd2;
        public uint StencilCmd3;
        public uint StencilCmd4;


    }

    public class TextureCoordinator
    {
        public uint SourceCoordinate;
        public uint MappingMethod;
        public int ReferenceCamera;
        public uint MatrixMode;
        public Vector2 Scale;
        public Single Rotation;
        public Vector2 Translation;
        public uint Unknown;
        public Matrix Matrix;
    }

    public class TextureInfo
    {
        public uint Type;
        public uint DynamicAllocator;
        public Offset TxobOffset;
        public Offset SamplerOffset;
        public Txob Txob;
    }
}
