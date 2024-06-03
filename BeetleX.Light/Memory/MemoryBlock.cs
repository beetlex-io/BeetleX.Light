using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{

    internal class MemoryBlockPool
    {

        public static readonly MemoryBlockPool _default = new MemoryBlockPool();
        public static MemoryBlockPool Default => _default;
        public MemoryBlockPool()
        {
            _partitions = new Partition[Environment.ProcessorCount];
            for (int i = 0; i < _partitions.Length; i++)
            {
                _partitions[i] = new Partition();
            }
        }

        private Partition[] _partitions;

        public MemoryBlock Get(int length)
        {
            int index = Thread.GetCurrentProcessorId() % _partitions.Length;
            var result = _partitions[index].Pop();
            if (result == null)
            {
                result = new MemoryBlock(length);
                System.Threading.Interlocked.Increment(ref _allocatedQuantity);
                result.Partition = _partitions[index];
            }
            result.Next = null;
            result.Disposed = 0;
            result.Postion = 0;
            result.Allocated = 0;
            return result;
        }

        //internal class Partition
        //{
        //    private System.Collections.Concurrent.ConcurrentStack<MemoryBlock> mData = new System.Collections.Concurrent.ConcurrentStack<MemoryBlock>();

        //    public long Length => mData.Count;

        //    public MemoryBlock Pop()
        //    {
        //        mData.TryPop(out var result);
        //        return result;
        //    }

        //    public void Push(MemoryBlock data)
        //    {

        //        mData.Push(data);
        //    }

        //}

        internal class Partition
        {
            private Stack<MemoryBlock> mData = new Stack<MemoryBlock>();

            public long Length => mData.Count;

            public MemoryBlock Pop()
            {
                lock (this)
                {
                    if (mData.Count > 0)
                        return mData.Pop();
                    return null;

                }

            }

            public void Push(MemoryBlock data)
            {
                lock (this)
                {
                    mData.Push(data);
                }
            }

        }

        private long _allocatedQuantity = 0;

        public long AllocatedQuantity => _allocatedQuantity;

        public long Count
        {
            get
            {
                long result = 0;
                foreach (var item in _partitions)
                {
                    result += item.Length;
                }
                return result;
            }
        }
    }

    internal class MemoryBlock : IDisposable
    {
        public MemoryBlock(int length)
        {
            if (length < Constants.MemorySegmentMinSize)
                length = Constants.MemorySegmentMinSize;
            if (length > Constants.MemorySegmentMaxSize)
                length = Constants.MemorySegmentMaxSize;
            Data = MemoryPool<byte>.Shared.Rent(length);
            Length = Data.Memory.Length;
            Memory = Data.Memory;
        }

        private MemorySegment _memorySegment = new MemorySegment();

        public IMemoryOwner<byte> Data { get; set; }

        public Memory<byte> Memory { get; set; }

        public int Length { get; set; }

        public int Allocated { get; set; }

        public int Postion { get; set; }

        public MemoryBlock Next { get; set; }

        public void ReadAdvanceTo(int count)
        {
            Postion += count;
        }

        public void AdvanceTo(int count)
        {
            Allocated += count;
        }

        public MemorySegment GetUseMemorySegment()
        {
            _memorySegment.SetMemory(GetUseMemory());
            return _memorySegment;
        }

        public Memory<byte> Allot(int size)
        {
            if (size <= AvailableSize)
                return Memory.Slice(Allocated, size);
            else
                return Memory.Slice(Allocated, AvailableSize);
        }

        public int AvailableSize => Memory.Length - Allocated;


        public Memory<byte> GetUseMemory()
        {
            return Memory.Slice(Postion, Allocated - Postion);
        }

        public MemoryBlockPool.Partition Partition { get; set; }


        public int Disposed = 0;
        public void Dispose()
        {
            if (System.Threading.Interlocked.CompareExchange(ref Disposed, 1, 0) == 0)
            {
                //Data.Dispose();
                //Memory = null;
                //Data = null;
                //Next = null;
                Partition.Push(this);
            }
        }
    }
}
