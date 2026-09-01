using System;

namespace FargoItems.Content.Logic
{
    /// <summary>
    /// 直通车一次建造的不可变坐标计划。计划只依赖使用瞬间的目标坐标，不读取实时鼠标状态。
    /// </summary>
    public sealed class InstavatorBuildPlan
    {
        public InstavatorBuildPlan(int startX, int startY, int endY, int minOffset, int maxOffset)
        {
            StartX = startX;
            StartY = startY;
            EndY = endY;
            MinOffset = minOffset;
            MaxOffset = maxOffset;
        }

        public int StartX { get; }
        public int StartY { get; }
        public int EndY { get; }
        public int MinOffset { get; }
        public int MaxOffset { get; }
        public int Width => Math.Max(0, MaxOffset - MinOffset + 1);
        public int Height => Math.Max(0, EndY - StartY + 1);
        public int CellCount => Width * Height;

        public InstavatorBuildCell GetCell(int index)
        {
            if (index < 0 || index >= CellCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int column = index / Height;
            int row = index % Height;
            int offset = MinOffset + column;
            return new InstavatorBuildCell(StartX + offset, StartY + row, offset);
        }
    }

    public struct InstavatorBuildCell
    {
        public InstavatorBuildCell(int x, int y, int offset)
        {
            X = x;
            Y = y;
            Offset = offset;
        }

        public int X { get; }
        public int Y { get; }
        public int Offset { get; }
    }
}
