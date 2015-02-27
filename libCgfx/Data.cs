using System.Collections.Generic;

/* DATA! Based on docs from 3dbrew along with some original research.
 * This whole thing will be getting HEAVILY refactored soon,
 * so I don't recommend actually using this source code.
 * -planetarian-
 */

namespace libCgfx
{
    public class Data : SizedCtrObject
    {
        public List<DictDescriptor> DictDescriptors { get; private set; }
        public Dict[] Dicts { get; private set; }
        
        public Data(CtrObject parent) : this(parent, parent.CurrentOffset)
        {
        }
        public Data(CtrObject parent, int startOffset) : base(parent, startOffset)
        {
            Magic = ReadMagic();
            Log("Magic '" + Magic + "' offset " + DisplayValue(StartOffset));

            LogIndent();

            Size = ReadInt32();
            Log("Size: " + DisplayValue(Size));

            Log("Entry Descriptors:");
            LogIndent();

            DictDescriptors = new List<DictDescriptor>();
            for (int i = 0; i < 16; i++)
            {
                int numEntries = ReadInt32();

                Offset offset = ReadOffset();

                // Empty descriptors are zeroed.
                if (numEntries == 0) continue;

                Log("Entry " + i + ":");
                LogIndent();
                Log("Entries: " + DisplayValue(numEntries));
                Log("Offset " + offset, 1);
                LogIndent(false);

                var descriptor = new DictDescriptor(numEntries, offset);
                DictDescriptors.Add(descriptor);
            }
            LogIndent(false);

            Log("Entries:");
            LogIndent();

            Dicts = new Dict[DictDescriptors.Count];
            for (int i = 0; i < DictDescriptors.Count; i++)
            {
                SkipTo(DictDescriptors[i].Offset.Absolute, false); 
                var dict = new Dict(this, CurrentOffset);
                Dicts[i] = dict;
            }
            LogIndent(false);

            LogIndent(false);

        }
    }

    public class DictDescriptor
    {
        public int NumEntries { get; internal set; }
        public Offset Offset { get; internal set; }

        public DictDescriptor() { }

        public DictDescriptor(int numEntries, Offset offset)
        {
            NumEntries = numEntries;
            Offset = offset;
        }
    }
}
