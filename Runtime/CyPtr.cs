using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace CyRayTracingSystem.Utils
{
    unsafe public struct CyPtr
    {
        private IntPtr intPtr;
        
        public CyPtr(TypedReference tr)
        {
            var ptr = *(IntPtr*) (&tr);
            intPtr = Marshal.AllocHGlobal(ptr);
        }

        public CyPtr(void* ptr)
        {
            intPtr = (IntPtr) ptr;
        }

        public T* Ptr<T>() where T : unmanaged
        {
            return (T*) intPtr;
        }
    }
    
    public unsafe struct CyEnumerator<T> : IEnumerator<T> where T : unmanaged
    {
        public T Current => *(ptr + index++);
        object IEnumerator.Current => Current;
        private int index;
        private int length;
        private T* ptr;

        public CyEnumerator(T* ptr, int length)
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
    
    public unsafe struct CyPtr<T> : IEnumerable<T> where T : unmanaged
    {
        private ulong address;
        public ulong Address => address;
        public T* Ptr => (T*) address;
        private int length;
        public int Length => length;
        
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
        public int Alignment
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

        public bool IsNull => length == 0 && address == 0;

        public CyPtr(int n, Allocator allocator = Allocator.Persistent)
        {
            length = n;
            size = UnsafeUtility.SizeOf<T>();
            alignment = UnsafeUtility.AlignOf<T>();
            address = (ulong) UnsafeUtility.Malloc(size * length, alignment, allocator);
            UnsafeUtility.MemSet((void*) address, 0, size * length);
        }

        public void SetByte(byte b)
        {
            UnsafeUtility.MemSet((void*) address, b, size * length);
        }

        public CyPtr(T* ptr, int length, Allocator allocator = Allocator.Persistent)
        {
            this.length = length;
            size = UnsafeUtility.SizeOf<T>();
            alignment = UnsafeUtility.AlignOf<T>();
            address = (ulong) UnsafeUtility.Malloc(size * length, alignment, allocator);
            UnsafeUtility.MemCpy((void*) address, ptr, size * length);
        }
        
        public CyPtr(T[] array, Allocator allocator = Allocator.Persistent)
        {
            length = array.Length;
            size = UnsafeUtility.SizeOf<T>();
            alignment = UnsafeUtility.AlignOf<T>();
            GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var ptr = handle.AddrOfPinnedObject();            
            address = (ulong) UnsafeUtility.Malloc(size * length, alignment, allocator);
            UnsafeUtility.MemCpy((void*) address, (void*)ptr, size * length);
            handle.Free();
        }

        public CyPtr(CyPtr<T> ptr, Allocator allocator = Allocator.Persistent)
        {
            length = ptr.length;
            size = ptr.Size;
            alignment = ptr.Alignment;
            address = (ulong) UnsafeUtility.Malloc(size * length, alignment, allocator);
            UnsafeUtility.MemCpy((void*) address, ptr.Ptr, size * length);
        }

        public CyPtr(ref T[] array, Allocator allocator = Allocator.Persistent)
        {
            length = array.Length;
            size = UnsafeUtility.SizeOf<T>();
            alignment = UnsafeUtility.AlignOf<T>();
            address = (ulong) UnsafeUtility.Malloc(size * length, alignment, allocator);
            
            GCHandle gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var source = gcHandle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy((void*) address, (void*) source, size * length);
            gcHandle.Free();
        }

        public void Reserve(void* ptr, int n)
        {
            address = (ulong) ptr;
            length = n;
        }

        public T this[int index]
        {
            get => *((T*) address + index);
            set => *((T*) address + index) = value;
        }

        public CyPtr<T> Split(int index, int n)
        {
            CyPtr<T> cyPtr = new CyPtr<T>();
            cyPtr.address = (ulong) (Ptr + index);
            cyPtr.alignment = alignment;
            cyPtr.length = n;
            cyPtr.size = size;
            return cyPtr;
        }
        
        public static implicit operator T*(CyPtr<T> p)
        {
            return p.Ptr;
        }

        public static T* operator +(CyPtr<T> p, int offset)
        {
            return (T*) p.address + offset;
        }

        public static T* operator +(int offset, CyPtr<T> p)
        {
            return (T*) p.address + offset;
        }

        public static bool operator ==(CyPtr<T> p, ulong u)
        {
            return p.address == u;
        }

        public static bool operator !=(CyPtr<T> p, ulong u)
        {
            return p.address != u;
        }

        public void Free(Allocator allocator = Allocator.Persistent)
        {
            if (length > 0)
            {
                UnsafeUtility.Free((void*) address, allocator);
                length = 0;
                address = 0;
            }
        }

        public void CopyFrom(CyPtr<T> ptr)
        {
            UnsafeUtility.MemCpy((void*) address, (void*) ptr.address, ptr.Length * size);
        }
        
        public void CopyFrom(void* ptr, int offset, int length)
        {
            if (offset + length > this.length) return;
            UnsafeUtility.MemCpy((byte*) address + offset, ptr, length);
        }

        public CyEnumerator<T> GetEnumerator()
        {
            return new CyEnumerator<T>((T*) address, length);
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
            T[] array = new T[length];
            fixed (T* dest = array)
            {
                UnsafeUtility.MemCpy(dest, Ptr, size * length);
            }
            return array;
        }
    }
}
