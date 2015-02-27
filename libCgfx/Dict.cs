using System;
using System.Collections.Generic;

/* CGFX! Based on docs from 3dbrew along with some original research.
 * This whole thing will be getting HEAVILY refactored soon,
 * so I don't recommend actually using this source code.
 * -planetarian-
 */

namespace libCgfx
{
    public class Dict : SizedCtrObject
    {
        public int NumEntries { get; private set; }
        public List<DictEntry> Entries { get; private set; }

        public Dict(CtrObject parent) : this(parent, parent.CurrentOffset) { }
        public Dict(CtrObject parent, int startOffset) : base(parent, startOffset)
        {
            Entries = new List<DictEntry>();


            Magic = ReadMagic();
            Log("Magic '" + Magic + "' offset " + DisplayValue(StartOffset));
            LogIndent();

            Size = ReadInt32();
            Log("Size: " + DisplayValue(Size));

            NumEntries = ReadInt32();
            Log("Entry Descriptors: " + DisplayValue(NumEntries));
            LogIndent();

            Skip(0x10);

            for (int i = 0; i < NumEntries; i++)
            {
                var entry = new DictEntry();
                Log("Entry " + i + ":");
                LogIndent();

                entry.RefBit = ReadInt32();
                Log("RefBit: " + DisplayValue(entry.RefBit), 1);
                entry.LeftIndex = ReadInt16();
                Log("LeftIndex: " + DisplayValue(entry.LeftIndex), 1);
                entry.RightIndex = ReadInt16();
                Log("RightIndex: " + DisplayValue(entry.RightIndex), 1);

                
                entry.SymbolOffset = ReadOffset();
                Log("SymbolOffset " + entry.SymbolOffset, 2);

                entry.Symbol = ReadStringTerminated(entry.SymbolOffset.Absolute);
                Log("Symbol: '" + entry.Symbol + "'");

                entry.Value = ReadInt32();
                entry.ValueAsOffset = new Offset(PreviousOffset, entry.Value);

                string magic;
                entry.ValueHasMagic = TryReadMagic(entry.ValueAsOffset.Absolute + 0x4, out magic);
                Log("Value: " + DisplayValue(entry.Value)
                    + " (as offset: " + DisplayValue(entry.ValueAsOffset.Absolute) + ")");
                if (entry.ValueHasMagic)
                    Log("Magic: '" + magic + "'", true);

                LogIndent(false);
                Entries.Add(entry);
            }
            LogIndent(false);

            Log("Entries:");
            LogIndent();
            foreach (var entry in Entries)
            {
                if (!entry.ValueHasMagic) continue;

                SkipTo(entry.ValueAsOffset.Absolute, false);
                //Skip(0x4); // flags

                try
                {
                    entry.ValueObject = ReadCtrObject();
                }
                catch (NotImplementedException ex)
                {
                    Log(ex.Message);
                }

            }
            LogIndent(false);

            LogIndent(false);

        }
    }

    public class DictEntry
    {
        public Offset SymbolOffset { get; internal set; }
        public string Symbol { get; internal set; }

        public int Value { get; internal set; }
        public Offset ValueAsOffset { get; internal set; }
        public bool ValueHasMagic { get; internal set; }
        public CtrObject ValueObject { get; internal set; }

        // apparently an implementation of Radix trees
        // via Gericom
        public int RefBit { get; internal set; }
        public short LeftIndex { get; internal set; }
        public short RightIndex { get; internal set; }
    }
}
