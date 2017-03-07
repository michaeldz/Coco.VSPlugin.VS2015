using System.Collections;

namespace at.jku.ssw.Coco
{
    internal class BufferHelper
    {
        /// <summary>
        /// Buffer instance
        /// </summary>
        private Buffer buffer;

        /// <summary>
        /// Line start positions
        /// </summary>
        private ArrayList linePosition = new ArrayList();

        /// <summary>
        /// Buffer helper, scans the buffer for line starts
        /// </summary>
        /// <param name="buffer">Buffer of the atg file</param>
        public BufferHelper(Buffer buffer)
        {
            this.buffer = buffer;
            int oldPos = buffer.Pos;

            buffer.Pos = 0;
            int ch;
            linePosition.Add(buffer.Pos);
            while ((ch = buffer.Read()) != Buffer.EOF)
            {
                if (ch == '\r' && buffer.Peek() != '\n') ch = '\n';
                if (ch == '\n')
                {
                    linePosition.Add(buffer.Pos + 1);
                }
            }

            buffer.Pos = oldPos;
        }

        /// <summary>
        /// Convert file position to line and column
        /// </summary>
        /// <param name="pos">Absolute file position</param>
        /// <param name="line">Line number</param>
        /// <param name="column">Column number</param>
        public void PositionToLine(int pos, out int line, out int column)
        {
            int first = 0;
            int last = linePosition.Count;

            // Perform binary search for line number
            while (first + 1 < last)
            {
                int m = (last + first) / 2;
                int mpos = (int)linePosition[m];
                if (mpos > pos)
                {
                    last = m;
                }
                else if (mpos < pos)
                {
                    first = m;
                }
                else if (mpos == pos)
                {
                    first = m;
                    last = m;
                }
            }

            line = first;
            column = pos - (int)linePosition[line];
        }
    }
}