using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libCgfx
{
    public class Coverage : IEnumerable<CoverageNode>
    {
        public CoverageNode Head { get; private set; }
        public int BytesCovered => Head?.BytesCovered ?? 0;

        public void Add(int offset)
        {
            // Simplifies the code, but may not be as efficient?
            //Add(offset, offset); return;

            // If no head yet, or offset is well before it
            if (Head == null || offset < Head.OffsetStart - 1)
            {
                Head = new CoverageNode
                {
                    OffsetStart = offset,
                    OffsetEnd = offset,
                    Next = Head
                };
                return;
            }
            
            for (CoverageNode node = Head; node != null; node = node.Next)
            {
                // If adjacent to start, extend to cover
                if (offset == node.OffsetStart - 1)
                {
                    node.OffsetStart = offset;
                    return;
                }

                // If covered by node already, no action necessary
                if (offset >= node.OffsetStart && offset <= node.OffsetEnd)
                    return;

                // If adjacent to end, extend to cover
                if (offset == node.OffsetEnd + 1)
                {
                    node.OffsetEnd = offset;

                    // If next node is now adjacent to this one, combine them
                    if (node.Next != null && node.Next.OffsetStart == offset + 1)
                    {
                        node.OffsetEnd = node.Next.OffsetEnd;
                        node.Next = node.Next.Next;
                    }
                    return;
                }

                // If not covered by or adjacent to this or the next node, create new node
                if (offset > node.OffsetEnd + 1 && (node.Next == null || offset < node.Next.OffsetStart - 1))
                {
                    node.Next = new CoverageNode
                    {
                        OffsetStart = offset,
                        OffsetEnd = offset,
                        Next = node.Next
                    };
                    return;
                }
            } // for nodes ....
        }

        public void Add(int offsetStart, int offsetEnd)
        {
            if (offsetEnd < offsetStart)
                throw new InvalidOperationException("offsetEnd must not be less than offsetStart.");

            // If no head yet, or range is well before it
            if (Head == null || offsetEnd < Head.OffsetStart - 1)
            {
                Head = new CoverageNode
                {
                    OffsetStart = offsetStart,
                    OffsetEnd = offsetEnd,
                    Next = Head
                };
                return;
            }

            for (CoverageNode node = Head; node != null; node = node.Next)
            {
                // If adjacent or overlapping, extend to cover
                if (offsetStart <= node.OffsetEnd + 1 && offsetEnd >= node.OffsetStart - 1)
                {
                    if (offsetStart < node.OffsetStart)
                        node.OffsetStart = offsetStart;
                    if (offsetEnd > node.OffsetEnd)
                        node.OffsetEnd = offsetEnd;

                    // Contain all nodes hereafter which lie within the new range
                    while (node.Next != null && node.Next.OffsetStart <= node.OffsetEnd + 1)
                    {
                        if (node.OffsetEnd < node.Next.OffsetEnd)
                            node.OffsetEnd = node.Next.OffsetEnd;
                        node.Next = node.Next.Next;
                    }
                    return;
                }

                // If not covered by or adjacent to this or the next node, create new node
                if (offsetStart > node.OffsetEnd + 1 && (node.Next == null || offsetEnd < node.Next.OffsetStart - 1))
                {
                    node.Next = new CoverageNode
                    {
                        OffsetStart = offsetStart,
                        OffsetEnd = offsetEnd,
                        Next = node.Next
                    };
                    return;
                }
            } // for nodes ....
        }

        public IEnumerator<CoverageNode> GetEnumerator()
        {
            for (CoverageNode node = Head; node != null; node = node.Next)
                yield return node;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
