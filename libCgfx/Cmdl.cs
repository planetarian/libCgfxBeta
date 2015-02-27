using System.Linq;
using Microsoft.Xna.Framework;

/* CMDL! Based on docs from 3dbrew, along with some original research.
 * Some small updates thanks to additional information from Gericom.
 * This whole thing will be getting HEAVILY refactored soon,
 * so I don't recommend actually using this source code.
 * -planetarian-
 */

namespace libCgfx
{
    public class Cmdl : FlaggedCtrObject
    {
        public int NameOffsetRelative { get; private set; }
        public int NameOffsetAbsolute { get; private set; }
        public string Name { get; private set; }


        public int Flags2 { get; private set; }
        public int IsBranchVisibleRaw { get; private set; }
        public bool IsBranchVisible { get; private set; }
        public int ChildrenCount { get; private set; }

        public int AnimTypesDictEntries { get; private set; }
        public Offset AnimTypesDictOffset { get; private set; }
        public Dict AnimTypesDict { get; private set; }

        public Vector3 Scale { get; private set; }
        public Vector3 Rotation { get; private set; }
        public Vector3 Translation { get; private set; }

        public Matrix LocalMatrix { get; private set; }
        public Matrix WorldMatrix { get; private set; }

        public int VertexInfoSobjEntries { get; private set; }
        public Offset VertexInfoSobjListOffset { get; private set; }
        public Offset[] VertexInfoSobjOffsets { get; private set; }
        public Sobj[] VertexInfoSobjItems { get; private set; } 

        public int MtobDictEntries { get; private set; }
        public Offset MtobDictOffset { get; private set; }
        public Dict MtobDict { get; private set; }

        public int VertexSobjEntries { get; private set; }
        public Offset VertexSobjListOffset { get; private set; }
        public Offset[] VertexSobjOffsets { get; private set; }
        public Sobj[] VertexSobjItems { get; private set; } 

        public int ShapeDictEntries { get; private set; }
        public Offset ShapeDictOffset { get; private set; }
        public Dict ShapeDict { get; private set; }

        public bool HasSkeletonSobj { get; private set; }
        public Offset SkeletonSobjOffset { get; private set; }
        public Sobj SkeletonSobj { get; private set; }


        public Cmdl(CtrObject parent) : this(parent, parent.CurrentOffset)
        {
        }

        public Cmdl(CtrObject parent, int startOffset) : base(parent, startOffset)
        {
            RootObject.CmdlObjects.Add(this);

            // 0x0
            Flags = ReadUInt32();

            // 0x4
            Magic = ReadMagic();
            Log("Magic '" + Magic + "' for offset " + DisplayValue(StartOffset));
            LogIndent();
            Log("Flags: " + DisplayValue(Flags), 2);

            HasSkeletonSobj = IsFlagSet(7);
            Log("HasSkeletonSobj: " + HasSkeletonSobj);

            // 0x8
            Revision = ReadInt32();
            Log("Revision: " + DisplayValue(Revision));

            // 0xC
            NameOffsetRelative = ReadInt32();
            NameOffsetAbsolute = NameOffsetRelative + PreviousOffset;
            Log("NameOffset rel: " + DisplayValue(NameOffsetRelative)
                + " abs: " + DisplayValue(NameOffsetAbsolute), 2);
            Name = ReadStringTerminated(NameOffsetAbsolute);
            Log("Name: '" + Name + "'");

            // 0x10
            Skip(0x8);

            // 0x18
            Flags2 = ReadInt32();
            Log("Flags2: " + DisplayValue(Flags2));

            // 0x1C
            IsBranchVisibleRaw = ReadInt32();
            IsBranchVisible = IsBranchVisibleRaw == 1;
            Log("IsBranchVisible: " + IsBranchVisible + " (" + DisplayValue(IsBranchVisibleRaw) + ")");

            // 0x20
            ChildrenCount = ReadInt32();
            Log("ChildrenCount: " + DisplayValue(ChildrenCount));

            // 0x24
            Skip(0x4);

            // 0x28
            AnimTypesDictEntries = ReadInt32();
            Log("AnimTypesDictEntries: " + DisplayValue(AnimTypesDictEntries), 1);
            // 0x2C
            AnimTypesDictOffset = ReadOffset();
            Log("AnimTypesDictOffset " + AnimTypesDictOffset, 2);

            // 0x30
            Scale = new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
            Log("Scale: " + DisplayValue(Scale));
            // 0x3C
            Rotation = new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
            Log("Rotation: " + DisplayValue(Rotation));
            // 0x48
            Translation = new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
            Log("Translation: " + DisplayValue(Translation));


            // 0x54
            LocalMatrix = ReadMatrix43();
            Log("LocalMatrix: " + LocalMatrix);

            // 0x84
            WorldMatrix = ReadMatrix43();
            Log("WorldMatrix: " + WorldMatrix);

            // 0xB4
            VertexInfoSobjEntries = ReadInt32();
            Log("VertexInfoSobjEntries: " + DisplayValue(VertexInfoSobjEntries));
            // 0xB8
            VertexInfoSobjListOffset = ReadOffset();
            Log("VertexInfoSobjListOffset " + VertexInfoSobjListOffset, 2);

            // 0xBC
            MtobDictEntries = ReadInt32();
            Log("MtobDictEntries: " + DisplayValue(MtobDictEntries));
            // 0xC0
            MtobDictOffset = ReadOffset();
            Log("MtobDictOffset " + MtobDictOffset, 2);

            // 0xC4
            VertexSobjEntries = ReadInt32();
            Log("VertexSobjEntries: " + DisplayValue(VertexSobjEntries));
            // 0xC8
            VertexSobjListOffset = ReadOffset();
            Log("VertexSobjListOffset " + VertexSobjListOffset, 2);

            // 0xCC
            ShapeDictEntries = ReadInt32();
            Log("ShapeDictEntries: " + DisplayValue(ShapeDictEntries));
            // 0xD0
            ShapeDictOffset = ReadOffset();
            Log("ShapeDictOffset " + ShapeDictOffset, 2);
            
            // 0xD4
            Skip(0xC);

            LogBreak(2);

            if (HasSkeletonSobj)
            {
                // 0xE0
                SkeletonSobjOffset = ReadOffset();
                Log("SkeletonSobjOffset " + SkeletonSobjOffset, 2);
            }
            LogBreak(2);

            // 0xB8 + [0xB8]
            SkipTo(VertexInfoSobjListOffset.Absolute);

            Log("VertexInfoSobjOffsets:");
            LogIndent();
            VertexInfoSobjOffsets = new Offset[VertexInfoSobjEntries];
            for (int i = 0; i < VertexInfoSobjEntries; i++)
            {
                VertexInfoSobjOffsets[i] = ReadOffset();
                Log("VertexInfoSobjOffset " + i + " " + VertexInfoSobjOffsets[i], 2);
            }
            LogIndent(false);

            LogBreak(2);

            Log("VertexSobjOffsets:");
            LogIndent();
            VertexSobjOffsets = new Offset[VertexSobjEntries];
            for (int i = 0; i < VertexSobjEntries; i++)
            {
                VertexSobjOffsets[i] = ReadOffset();
                Log("VertexSobjOffset " + i + " " + VertexSobjOffsets[i], 2);
            }
            LogIndent(false);

            LogBreak();
            
            // DICTs

            SkipTo(AnimTypesDictOffset.Absolute);
            Log("AnimTypesDict:");
            AnimTypesDict = new Dict(this, CurrentOffset);

            if (MtobDictEntries > 0)
            {
                SkipTo(MtobDictOffset.Absolute, false);
                Log("MtobDict:");
                MtobDict = new Dict(this, CurrentOffset);
            }

            if (ShapeDictEntries > 0)
            {
                SkipTo(ShapeDictOffset.Absolute, false);
                Log("ShapeDict:");
                ShapeDict = new Dict(this, CurrentOffset);

                Log("Shapes:");
                LogIndent();
                for (int s = 0; s < ShapeDictEntries; s++)
                {
                    Log("Shape " + s);
                    LogIndent();
                    
                    int shapeMetaOffset = ShapeDict.Entries[s].ValueAsOffset.Absolute;
                    Offset shapeFinalOffset = ReadOffset(shapeMetaOffset);
                    Log("SymbolOffset " + shapeFinalOffset);
                    string shapeSymbol = ReadStringTerminated(shapeFinalOffset.Absolute);
                    Log("Symbol: '" + shapeSymbol + "'");
                    int shapeFinalValue = ReadInt32(shapeMetaOffset + 0x4);
                    Log("Value: " + DisplayValue(shapeFinalValue));

                    LogIndent(false);
                }
                LogIndent(false);
            }

            // Parse AnimTypes

            foreach (DictEntry entry in AnimTypesDict.Entries)
            {

                SkipTo(entry.ValueAsOffset.Absolute, false);

                Log("AnimationType '" + entry.Symbol + "'");
                LogIndent();

                Skip(0x8);
                
                int entrySymbolOffsetRelative = ReadInt32();
                int entrySymbolOffsetAbsolute = entrySymbolOffsetRelative + PreviousOffset;
                string symbol = ReadStringTerminated(entrySymbolOffsetAbsolute);
                Log("AnimationTypeInner symbol: '" + symbol + "':");

                Skip(0x8);

                Skip(ReadInt32(false));

                var animTypeDict = new Dict(this, CurrentOffset);

                LogIndent(false);
            }

            // Vertex Info

            Log("VertexInfo SOBJs:");
            LogIndent();
            VertexInfoSobjItems = CheckSobjList("VertexInfo", VertexInfoSobjOffsets);
            LogIndent(false);

            // Vertex

            Log("Vertex SOBJs:");
            LogIndent();
            VertexSobjItems = CheckSobjList("Mesh/Vertex", VertexSobjOffsets);
            LogIndent(false);

            for (int i = 0; i < VertexInfoSobjEntries; i++)
                VertexSobjItems[i].MeshNodeName = VertexInfoSobjItems[i].MeshNodeName;
            
            // Skeleton

            if (HasSkeletonSobj)
            {
                Log("Skeleton:");
                LogIndent();

                SkipTo(SkeletonSobjOffset.Absolute);
                SkeletonSobj = new Sobj(this);

                foreach (FaceGroup faceGroup in VertexSobjItems.SelectMany(v => v.FaceGroups))
                {
                    faceGroup.Bones = faceGroup.BoneGroups
                        .Select(g => SkeletonSobj.Bones[g])
                        .ToArray();
                }

                LogIndent(false);
            }

            LogIndent(false);
        }


        public Sobj[] CheckSobjList(string name, Offset[] sobjOffsets)
        {
            var sobjList = new Sobj[sobjOffsets.Length];
            for (int i = 0; i < sobjOffsets.Length; i++)
            {
                SkipTo(sobjOffsets[i].Absolute, false);

                Log(name + " " + i + " offset " + DisplayValue(sobjOffsets[i].Absolute));
                LogIndent();

                var sobj = new Sobj(this, CurrentOffset);
                sobjList[i] = sobj;

                LogIndent(false);

                SkipTo(sobj.CurrentOffset, false);
            }
            return sobjList;
        }
    }


    
}
