using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace CyRayTracingSystem.Utils
{
    public unsafe struct CyEnumerator2<T> : IEnumerator<T> where T : unmanaged
    {
        public T Current
        {
            get
            {
                if (x++ == width)
                {
                    y++;
                    x = 0;
                }

                return ptr[index++];
            }
        }
        object IEnumerator.Current => Current;
        private int index;
        private int width;
        private int x, y;
        private int length;
        private T* ptr;

        public CyEnumerator2(T* ptr, int width, int height)
        {
            this.ptr = ptr;
            this.width = width;
            x = y = 0;
            length = width * height;
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
    
    public unsafe struct CyPtr2<T> : IEnumerable<T> where T : unmanaged
    {
        private ulong address;
        public ulong Address => address;
        private int width, height;
        public int Width => width;
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

        public CyPtr2(int width, int height, Allocator allocator = Allocator.Persistent)
        {
            alignment = UnsafeUtility.AlignOf<T>();
            size = UnsafeUtility.SizeOf<T>();
            address = (ulong) UnsafeUtility.Malloc(size * width * height, alignment, allocator);
            UnsafeUtility.MemSet((void*) address, 0, size * width * height);
            this.width = width;
            this.height = height;
        }

        public CyPtr2(CyPtr2<T> ptr, Allocator allocator = Allocator.Persistent)
        {
            width = ptr.width;
            height = ptr.height;
            alignment = UnsafeUtility.AlignOf<T>();
            size = UnsafeUtility.SizeOf<T>();
            if (width != 0 && height != 0)
            {
                address = (ulong) UnsafeUtility.Malloc(size * width * height, alignment, allocator);
                UnsafeUtility.MemCpy((void*) address, (void*) ptr.address, size * width * height);
            }
            else
            {
                address = 0;
            }
        }

        public CyPtr2(ref T[][] array2D, Allocator allocator = Allocator.Persistent)
        {
            GCHandle gc = GCHandle.Alloc(array2D, GCHandleType.Pinned);
            height = array2D.Length;
            width = array2D[0].Length;
            size = UnsafeUtility.SizeOf<T>();
            alignment = UnsafeUtility.AlignOf<T>();
            address = (ulong) UnsafeUtility.Malloc(size * width * height, alignment, allocator);
            for (int i = 0; i < height; ++i)
            {
                GCHandle gc2 = GCHandle.Alloc(array2D[i], GCHandleType.Pinned);
                var src = gc2.AddrOfPinnedObject();
                UnsafeUtility.MemCpy((T*) address + width * i, (void*) src, size * width);
                gc2.Free();
            }
            gc.Free();
        }

        public T* this[int y] => (T*) address + y * width;
        public T this[int x, int y]
        {
            get => *((T*) address + x + y * width);
            set => *((T*) address + x + y * width) = value;
        }
        
        public static bool operator ==(CyPtr2<T> p, ulong u)
        {
            return p.address == u;
        }

        public static bool operator !=(CyPtr2<T> p, ulong u)
        {
            return p.address != u;
        }

        public void SetByte(byte b)
        {
            UnsafeUtility.MemSet((void*) address, b, width * height * size);
        }

        public void Free()
        {
            if (address != 0)
            {
                UnsafeUtility.Free((void*) address, Allocator.Persistent);
                width = 0;
                height = 0;
                address = 0;
            }
        }

        public CyEnumerator2<T> GetEnumerator()
        {
            return new CyEnumerator2<T>((T*) address, width, height);
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
