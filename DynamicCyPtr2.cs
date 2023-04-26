using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace CyRayTracingSystem.Utils
{
    public unsafe struct DynamicCyEnumerator2<T> : IEnumerator<T> where T : unmanaged
    {
        public T Current
        {
            get
            {
                index++;
                if (x + 1 > widthPtr[y])
                {
                    y++;
                    x = 0;
                }
                return ((T*)addressPtr[y])[x++];
            }
        }
        object IEnumerator.Current => Current;
        private int index;
        private int x, y;
        private int length;
        private DynamicCyPtr<ulong> addressPtr;
        private DynamicCyPtr<int> widthPtr;

        public DynamicCyEnumerator2(DynamicCyPtr<int> widthPtr, DynamicCyPtr<ulong> addressPtr, int height)
        {
            this.widthPtr = widthPtr;
            this.addressPtr = addressPtr;
            x = y = 0;
            int widthSum = 0;
            foreach (var w in widthPtr)
            {
                widthSum += w;
            }
            length = widthSum;
            index = 0;
        }

        public void Reset()
        {
            index = 0;
            x = y = 0;
        }

        public bool MoveNext()
        {
            return index < length;
        }

        public void Dispose()
        {
            
        }
    }
    
    public unsafe struct DynamicCyPtr2<T> : IEnumerable<T> where T : unmanaged
    {
        private DynamicCyPtr<ulong> addressPtr;
        private DynamicCyPtr<int> widthPtr;
        
        private int height;
        public int Height => height;

        private int size;
        private int Size
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

        public T* this[int y] => (T*) addressPtr[y];
        public T this[int x, int y]
        {
            get => ((T*) addressPtr[y])[x];
            set => ((T*) addressPtr[y])[x] = value;
        }

        public bool IsNull => height == 0;

        public DynamicCyPtr2(DynamicCyPtr2<T> ptr2, Allocator allocator = Allocator.Persistent)
        {
            addressPtr = new DynamicCyPtr<ulong>(ptr2.addressPtr, allocator);
            widthPtr = new DynamicCyPtr<int>(ptr2.widthPtr, allocator);
            height = ptr2.height;
            size = ptr2.Size;
            alignment = ptr2.Alignment;
        }

        public T* AddN(int n)
        {
            CyPtr<T> p = new CyPtr<T>(n);
            addressPtr.Add((ulong) p.Ptr);
            widthPtr.Add(n);
            height++;
            return p.Ptr;
        }

        public void Add(ref T[] array)
        {
            CyPtr<T> ptr = new CyPtr<T>(ref array);
            addressPtr.Add(ptr.Address);
            widthPtr.Add(array.Length);
            height++;
        }

        public void Free()
        {
            if (height != 0)
            {
                foreach (var address in addressPtr)
                {
                    UnsafeUtility.Free((void*) address, Allocator.Persistent);
                }

                addressPtr.Free();
                widthPtr.Free();
                height = 0;
            }
        }

        public DynamicCyEnumerator2<T> GetEnumerator()
        {
            return new DynamicCyEnumerator2<T>(widthPtr, addressPtr, height);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
