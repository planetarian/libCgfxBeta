
/* LUTS! Based on docs from 3dbrew. Have not researched this further.
 * This whole thing will be getting HEAVILY refactored soon,
 * so I don't recommend actually using this source code.
 * -planetarian-
 */

namespace libCgfx
{
    public class Luts : FlaggedCtrObject
    {
        public short Unknown1 { get; private set; }
        public short Unknown2 { get; private set; }
        public Offset SymbolOffset { get; private set; }
        public string Symbol { get; private set; }
        public int Unknown4 { get; private set; }
        public int Unknown5 { get; private set; }
        public Dict LutsDict { get; private set; }

        
        public Luts(CtrObject parent) : this(parent, parent.CurrentOffset)
        {
        }
        public Luts(CtrObject parent, int startOffset) : base(parent, startOffset)
        {
            Flags = ReadUInt32();

            Magic = ReadMagic();
            Log("Magic '" + Magic + "' for offset " + DisplayValue(StartOffset));
            LogIndent();
            Log("Flags: " + DisplayValue(Flags), 2);

            Unknown1 = ReadInt16();
            Log("Unknown1: " + DisplayValue(Unknown1));
            Unknown2 = ReadInt16();
            Log("Unknown2: " + DisplayValue(Unknown2));
            SymbolOffset = ReadOffset();
            Log("SymbolOffset: " + SymbolOffset);
            Symbol = ReadStringTerminated(SymbolOffset.Absolute);
            LogIndent();
            Log("Symbol: " + Symbol);
            LogIndent(false);

            Skip(0x8);

            Unknown4 = ReadInt32();
            Log("Uknown4: " + DisplayValue(Unknown4));
            Unknown5 = ReadInt32();
            Log("Uknown5: " + DisplayValue(Unknown5));

            Log("LutsDict: ");
            LogIndent();
            LutsDict = new Dict(this, CurrentOffset);

            LogIndent(false);

            LogIndent(false);
        }
    }
}
