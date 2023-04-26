using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace CyRayTracingSystem.Utils
{
    public unsafe struct MemoryManager
    {
        private DynamicCyPtr<ulong> alloctedPtrs;

        private static DynamicCyPtr<ulong> globalPtrs;

        private static DynamicCyPtr<DynamicCyPtr<ulong>> tempGlobalPtrs;
        private static bool isAllocingTempMemory;

        public static int BeginAllocTempMemory()
        {
            isAllocingTempMemory = true;
            tempGlobalPtrs.Add(new DynamicCyPtr<ulong>());
            return tempGlobalPtrs.Count - 1;
        }

        public static void EndAllocTempMemory(int handle)
        {
            if (isAllocingTempMemory)
            {
                isAllocingTempMemory = false;
                DynamicCyPtr<ulong> ptrs = tempGlobalPtrs[handle];
                
                foreach (var temp in ptrs)
                {
                    UnsafeUtility.Free((void*) temp, Allocator.Persistent);
                }
                ptrs.Free();
            }
        }

        public T* Alloc<T>(T t, Allocator allocator) where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>();
            var alignment = UnsafeUtility.AlignOf<T>();
            var ptr = (T*) UnsafeUtility.Malloc(size, alignment, allocator);
            UnsafeUtility.MemCpy(ptr,&t, size);
            if (allocator == Allocator.Persistent)
                alloctedPtrs.Add((ulong) ptr);
            
            return ptr;
        }
        

        public T* Alloc<T>(int length, Allocator allocator) where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>();
            var alignment = UnsafeUtility.AlignOf<T>();
            var ptr = (T*) UnsafeUtility.Malloc(size * length, alignment, allocator);
            if (allocator == Allocator.Persistent)
                alloctedPtrs.Add((ulong) ptr);
            
            return ptr;
        }


        public T* Alloc<T>(int length, byte initByte, Allocator allocator) where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>();
            var alignment = UnsafeUtility.AlignOf<T>();
            var ptr = (T*) UnsafeUtility.Malloc(size * length, alignment, allocator);
            UnsafeUtility.MemSet(ptr, initByte, size * length);
            if (allocator == Allocator.Persistent)
                alloctedPtrs.Add((ulong) ptr);
            
            return ptr;
        }
        
        public static void FreeGlobal(void* ptr, Allocator allocator)
        {
            UnsafeUtility.Free(ptr, allocator);
        }

        public void Free(void* ptr, Allocator allocator)
        {
            UnsafeUtility.Free(ptr, allocator);
        }
        
        public static void FreeAllGlobal()
        {
            foreach (var ptr in globalPtrs)
            {
                UnsafeUtility.Free((void*) ptr, Allocator.Persistent);
            }

            foreach (var ptrs in tempGlobalPtrs)
            {
                foreach (var ptr in ptrs)
                {
                    UnsafeUtility.Free((void*) ptr, Allocator.Persistent);
                }
                ptrs.Free();
            }
            tempGlobalPtrs.Free();
        }

        public void FreeAll()
        {
            foreach (var ptr in alloctedPtrs)
            {
                UnsafeUtility.Free((void*) ptr, Allocator.Persistent);
            }
        }
    }
}