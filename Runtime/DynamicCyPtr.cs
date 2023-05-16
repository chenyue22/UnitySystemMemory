using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace CyRayTracingSystem.Utils
{
    public unsafe struct DynamicCyEnumerator<T> : IEnumerator<T> where T : unmanaged
    {
        public T Current => ptr[index++];

        object IEnumerator.Current => Current;
        private int index;
        private int length;
        private T* ptr;

        public DynamicCyEnumerator(T* ptr, int length)
        {
            this.ptr = ptr;
            this.length = length;
            index = 0;
        }

        public void Reset()
        {
            index = 0;
        }

        public bool MoveNext()
        {
            return index < length;
        }

        public void Dispose()
        {
            
        }
    }
    
    public unsafe struct DynamicCyPtr<T> : IEnumerable<T> where T : unmanaged
    {
        private ulong address;
        public T* Ptr => (T*) address;
        
        private int count;
        public int Count => count;
        private int capacity;

        private int size;
        public int Size
        {
            get
            {
                if (size == 0)
                {
                    size = UnsafeUtility.SizeOf<T>();
                }

                return size;
            }
        }

        private int alignment;
        private int Alignment
        {
            get
            {
                if (alignment == 0)
                {
                    alignment = UnsafeUtility.AlignOf<T>();
                }

                return alignment;
            }
        }

        public T this[int index] 
        {
            get => Ptr[index];
            set => *(Ptr + index) = value;
        }

        public DynamicCyPtr(DynamicCyPtr<T> ptr, Allocator allocator = Allocator.Persistent)
        {
            size = ptr.Size;
            alignment = ptr.Alignment;
            count = ptr.count;
            capacity = ptr.capacity;
            if (capacity > 0)
            {
                address = (ulong) UnsafeUtility.Malloc(size * capacity, ptr.Alignment, allocator);
                UnsafeUtility.MemCpy((void*) address, (void*) ptr.address, size * count);
            }
            else
            {
                address = 0;
            }
        }

        public static bool operator ==(DynamicCyPtr<T> p, ulong u)
        {
            return p.address == u;
        }

        public static bool operator !=(DynamicCyPtr<T> p, ulong u)
        {
            return p.address != u;
        }

        public T* AddN(int n)
        {
            var newCount = Count + n;
            bool dirty = false;
            while (newCount > capacity)
            {
                capacity = Mathf.Max(capacity, 2) * 2;
                dirty = true;
            }

            if (dirty)
            {
                var ptr = UnsafeUtility.Malloc(capacity * Size, Alignment, Allocator.Persistent);
                if (count > 0)
                {
                    UnsafeUtility.MemCpy(ptr, (void*) address, count * size);
                    UnsafeUtility.Free((void*) address, Allocator.Persistent);
                }
                address = (ulong) ptr;
            }

            return (T*) address + count;
        }

        public void Add(T t)
        {
            var newCount = Count + 1;
            if (newCount > capacity)
            {
                capacity = Mathf.Max(capacity, 2) * 2;
                var ptr = UnsafeUtility.Malloc(capacity * Size, Alignment, Allocator.Persistent);
                if (count > 0)
                {
                    UnsafeUtility.MemCpy(ptr, (void*) address, count * size);
                    UnsafeUtility.Free((void*) address, Allocator.Persistent);
                }
                address = (ulong) ptr;
            }

            ((T*) address)[count++] = t;
        }
        
        public void Add(T* ts, int n)
        {
            size = Size;
            var newCount = Count + n;
            bool dirty = false;
            while (newCount > capacity)
            {
                capacity = Mathf.Max(capacity, 2) * 2;
                dirty = true;
            }
            
            if (dirty)
            {
                var ptr = UnsafeUtility.Malloc(capacity * Size, Alignment, Allocator.Persistent);
                if (count > 0)
                {
                    UnsafeUtility.MemCpy(ptr, (void*) address, count * size);
                    UnsafeUtility.Free((void*) address, Allocator.Persistent);
                }
                address = (ulong) ptr;
            }
            
            UnsafeUtility.MemCpy((T*) address + count, ts, n * size);
            count += n;
        }

        public void Add(ref T[] ts)
        {
            size = Size;
            var newCount = Count + ts.Length;
            bool dirty = false;
            while (newCount > capacity)
            {
                capacity = Mathf.Max(capacity, 2) * 2;
                dirty = true;
            }
            
            if (dirty)
            {
                var ptr = UnsafeUtility.Malloc(capacity * Size, Alignment, Allocator.Persistent);
                if (count > 0)
                {
                    UnsafeUtility.MemCpy(ptr, (void*) address, count * size);
                    UnsafeUtility.Free((void*) address, Allocator.Persistent);
                }
                address = (ulong) ptr;
            }
            
            GCHandle gc = GCHandle.Alloc(ts, GCHandleType.Pinned);
            var src = gc.AddrOfPinnedObject();
            UnsafeUtility.MemCpy((T*) address + count, (T*) src, ts.Length * size);
            gc.Free();
            count += ts.Length;
        }

        public void RemoveAt(int index)
        {
            if (index == count - 1)
            {
                count -= 1;
            }

            if (index < count)
            {
                if (index == 0)
                {
                    count -= 1;
                    address += (ulong) size;
                }
                else
                {
                    UnsafeUtility.MemCpy(Ptr + index, Ptr + index + 1, size - index - 1);
                    count -= 1;
                }
            }
        }

        public void Free()
        {
            if (count > 0)
            {
                UnsafeUtility.Free((void*) address, Allocator.Persistent);
                count = 0;
                capacity = 0;
                address = 0;
            }
        }

        public DynamicCyEnumerator<T> GetEnumerator()
        {
            return new DynamicCyEnumerator<T>((T*) address, count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T[] ToArray()
        {
            T[] array = new T[Count];
            fixed (T* dest = array)
            {
                UnsafeUtility.MemCpy(dest, Ptr, size * Count);
            }
            return array;
        }
    }
}
