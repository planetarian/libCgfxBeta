namespace libCgfx
{
    public class CoverageNode
    {
        public CoverageNode Next { get; internal set; }
        public int OffsetStart { get; internal set; }
        public int OffsetEnd { get; internal set; }
        public int Length => OffsetEnd - OffsetStart + 1;
        internal int BytesCovered => Length + (Next?.BytesCovered ?? 0);
    }
}
