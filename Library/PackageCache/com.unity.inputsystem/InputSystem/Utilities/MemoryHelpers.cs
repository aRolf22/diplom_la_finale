using System;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Utilities
{
    internal static unsafe class MemoryHelpers
    {
        public struct BitRegion
        {
            public uint bitOffset;
            public uint sizeInBits;

            public bool isEmpty => sizeInBits == 0;

            public BitRegion(uint bitOffset, uint sizeInBits)
            {
                this.bitOffset = bitOffset;
                this.sizeInBits = sizeInBits;
            }

            public BitRegion(uint byteOffset, uint bitOffset, uint sizeInBits)
            {
                this.bitOffset = byteOffset * 8 + bitOffset;
                this.sizeInBits = sizeInBits;
            }

            public BitRegion Overlap(BitRegion other)
            {
                ////REVIEW: too many branches; this can probably be done much smarter

                var thisEnd = bitOffset + sizeInBits;
                var otherEnd = other.bitOffset + other.sizeInBits;

                if (thisEnd <= other.bitOffset || otherEnd <= bitOffset)
                    return default;

                var end = Math.Min(thisEnd, otherEnd);
                var start = Math.Max(bitOffset, other.bitOffset);

                return new BitRegion(start, end - start);
            }
        }

        public static bool Compare(void* ptr1, void* ptr2, BitRegion region)
        {
            if (region.sizeInBits == 1)
                return ReadSingleBit(ptr1, region.bitOffset) == ReadSingleBit(ptr2, region.bitOffset);
            return MemCmpBitRegion(ptr1, ptr2, region.bitOffset, region.sizeInBits);
        }

        public static uint ComputeFollowingByteOffset(uint byteOffset, uint sizeInBits)
        {
            return (uint)(byteOffset + sizeInBits / 8 + (sizeInBits % 8 > 0 ? 1 : 0));
        }

        public static void WriteSingleBit(void* ptr, uint bitOffset, bool value)
        {
            var byteOffset = bitOffset >> 3;
            bitOffset &= 7;
            if (value)
                *((byte*)ptr + byteOffset) |= (byte)(1U << (int)bitOffset);
            else
                *((byte*)ptr + byteOffset) &= (byte)~(1U << (int)bitOffset);
        }

        public static bool ReadSingleBit(void* ptr, uint bitOffset)
        {
            var byteOffset = bitOffset >> 3;
            bitOffset &= 7;
            return (*((byte*)ptr + byteOffset) & (1U << (int)bitOffset)) != 0;
        }

        public static void MemCpyBitRegion(void* destination, void* source, uint bitOffset, uint bitCount)
        {
            var destPtr = (byte*)destination;
            var sourcePtr = (byte*)source;

            // If we're offset by more than a byte, adjust our pointers.
            if (bitOffset >= 8)
            {
                var skipBytes = bitOffset / 8;
                destPtr += skipBytes;
                sourcePtr += skipBytes;
                bitOffset %= 8;
            }

            // Copy unaligned prefix, if any.
            if (bitOffset > 0)
            {
                var byteMask = 0xFF << (int)bitOffset;
                if (bitCount + bitOffset < 8)
                    byteMask &= 0xFF >> (int)(8 - (bitCount + bitOffset));

                *destPtr = (byte)(((*destPtr & ~byteMask) | (*sourcePtr & byteMask)) & 0xFF);

                // If the total length of the memory region is equal or less than a byte,
                // we're done.
                if (bitCount + bitOffset <= 8)
                    return;

                ++destPtr;
                ++sourcePtr;

                bitCount -= 8 - bitOffset;
            }

            // Copy contiguous bytes in-between, if any.
            var byteCount = bitCount / 8;
            if (byteCount >= 1)
                UnsafeUtility.MemCpy(destPtr, sourcePtr, byteCount);

            // Copy unaligned suffix, if any.
            var remainingBitCount = bitCount % 8;
            if (remainingBitCount > 0)
            {
                destPtr += byteCount;
                sourcePtr += byteCount;

                // We want the lowest remaining bits.
                var byteMask = 0xFF >> (int)(8 - remainingBitCount);

                *destPtr = (byte)(((*destPtr & ~byteMask) | (*sourcePtr & byteMask)) & 0xFF);
            }
        }

        /// <summary>
        /// Compare two memory regions that may be offset by a bit count and have a length expressed
        /// in bits.
        /// </summary>
        /// <param name="ptr1">Pointer to start of first memory region.</param>
        /// <param name="ptr2">Pointer to start of second memory region.</param>
        /// <param name="bitOffset">Offset in bits from each of the pointers to the start of the memory region to compare.</param>
        /// <param name="bitCount">Number of bits to compare in the memory region.</param>
        /// <param name="mask">If not null, only compare bits set in the mask. This allows comparing two memory regions while
        /// ignoring specific bits.</param>
        /// <returns>True if the two memory regions are identical, false otherwise.</returns>
        public static bool MemCmpBitRegion(void* ptr1, void* ptr2, uint bitOffset, uint bitCount, void* mask = null)
        {
            var bytePtr1 = (byte*)ptr1;
            var bytePtr2 = (byte*)ptr2;
            var maskPtr = (byte*)mask;

            // If we're offset by more than a byte, adjust our pointers.
            if (bitOffset >= 8)
            {
                var skipBytes = bitOffset / 8;
                bytePtr1 += skipBytes;
                bytePtr2 += skipBytes;
                if (maskPtr != null)
                    maskPtr += skipBytes;
                bitOffset %= 8;
            }

            // Compare unaligned prefix, if any.
            if (bitOffset > 0)
            {
                // If the total length of the memory region is less than a byte, we need
                // to mask out parts of the bits we're reading.
                var byteMask = 0xFF << (int)bitOffset;
                if (bitCount + bitOffset < 8)
                    byteMask &= 0xFF >> (int)(8 - (bitCount + bitOffset));

                if (maskPtr != null)
                {
                    byteMask &= *maskPtr;
                    ++maskPtr;
                }

                var byte1 = *bytePtr1 & byteMask;
                var byte2 = *bytePtr2 & byteMask;

                if (byte1 != byte2)
                    return false;

                // If the total length of the memory region is equal or less than a byte,
                // we're done.
                if (bitCount + bitOffset <= 8)
                    return true;

                ++bytePtr1;
                ++bytePtr2;

                bitCount -= 8 - bitOffset;
            }

            // Compare contiguous bytes in-between, if any.
            var byteCount = bitCount / 8;
            if (byteCount >= 1)
            {
                if (maskPtr != null)
                {
                    ////REVIEW: could go int by int here for as long as we can
                    // Have to go byte-by-byte in order to apply the masking.
                    for (var i = 0; i < byteCount; ++i)
                    {
                        var byte1 = bytePtr1[i];
                        var byte2 = bytePtr2[i];
                        var byteMask = maskPtr[i];

                        if ((byte1 & byteMask) != (byte2 & byteMask))
                            return false;
                    }
                }
                else
                {
                    if (UnsafeUtility.MemCmp(bytePtr1, bytePtr2, byteCount) != 0)
                        return false;
                }
            }

            // Compare unaligned suffix, if any.
            var remainingBitCount = bitCount % 8;
            if (remainingBitCount > 0)
            {
                bytePtr1 += byteCount;
                bytePtr2 += byteCount;

                // We want the lowest remaining bits.
                var byteMask = 0xFF >> (int)(8 - remainingBitCount);

                if (maskPtr != null)
                {
                    maskPtr += byteCount;
                    byteMask &= *maskPtr;
                }

                var byte1 = *bytePtr1 & byteMask;
                var byte2 = *bytePtr2 & byteMask;

                if (byte1 != byte2)
                    return false;
            }

            return true;
        }

        public static void MemSet(void* destination, int numBytes, byte value)
        {
            var to = (byte*)destination;
            var pos = 0;

            unchecked
            {
                // 64bit blocks.
                #if UNITY_64
                while (numBytes >= 8)
                {
                    *(ulong*)&to[pos] = ((ulong)value << 56) | ((ulong)value << 48) | ((ulong)value << 40) | ((ulong)value << 32)
                        | ((ulong)value << 24) | ((ulong)value << 16) | ((ulong)value << 8) | value;
                    numBytes -= 8;
                    pos += 8;
                }
                #endif

                // 32bit blocks.
                while (numBytes >= 4)
                {
                    *(uint*)&to[pos] = ((uint)value << 24) | ((uint)value << 16) | ((uint)value << 8) | value;
                    numBytes -= 4;
                    pos += 4;
                }

                // Remaining bytes.
                while (numBytes > 0)
                {
                    to[pos] = value;
                    numBytes -= 1;
                    pos += 1;
                }
            }
        }

        /// <summary>
        /// Copy from <paramref name="source"/> to <paramref name="destination"/> all the bits that
        /// ARE set in <paramref name="mask"/>.
        /// </summary>
        /// <param name="destination">Memory to copy to.</param>
        /// <param name="source">Memory to copy from.</param>
        /// <param name="numBytes">Number of bytes to copy.</param>
        /// <param name="mask">Bitmask that determines which bits to copy. Bits that are set WILL be copied.</param>
        public static void MemCpyMasked(void* destination, void* source, int numBytes, void* mask)
        {
            var from = (byte*)source;
            var to = (byte*)destination;
            var bits = (byte*)mask;
            var pos = 0;

            unchecked
            {
                // Copy 64bit blocks.
                #if UNITY_64
                while (numBytes >= 8)
                {
                    *(ulong*)(to + pos) &= ~*(ulong*)(bits + pos); // Preserve unmasked bits.
                    *(ulong*)(to + pos) |= *(ulong*)(from + pos) & *(ulong*)(bits + pos); // Copy masked bits.
                    numBytes -= 8;
                    pos += 8;
                }
                #endif

                // Copy 32bit blocks.
                while (numBytes >= 4)
                {
                    *(uint*)(to + pos) &= ~*(uint*)(bits + pos); // Preserve unmasked bits.
                    *(uint*)(to + pos) |= *(uint*)(from + pos) & *(uint*)(bits + pos); // Copy masked bits.
                    numBytes -= 4;
                    pos += 4;
                }

                // Copy remaining bytes.
                while (numBytes > 0)
                {
                    unchecked
                    {
                        to[pos] &= (byte)~bits[pos]; // Preserve unmasked bits.
                        to[pos] |= (byte)(from[pos] & bits[pos]); // Copy masked bits.
                    }
                    numBytes -= 1;
                    pos += 1;
                }
            }
        }

        /// <summary>
        /// Reads bits memory region as unsigned int, up to and including 32 bits, least-significant bit first (LSB).
        /// </summary>
        /// <param name="ptr">Pointer to memory region.</param>
        /// <param name="bitOffset">Offset in bits from the pointer to the start of the unsigned integer.</param>
        /// <param name="bitCount">Number of bits to read.</param>
        /// <returns>Read unsigned integer.</returns>
        public static uint ReadMultipleBitsAsUInt(void* ptr, uint bitOffset, uint bitCount)
        {
            if (ptr == null)
                throw new ArgumentNullException(nameof(ptr));
            if (bitCount > sizeof(int) * 8)
                throw new ArgumentException("Trying to read more than 32 bits as int", nameof(bitCount));

            // Shift the pointer up on larger bitmasks and retry.
            if (bitOffset > 32)
            {
                var newBitOffset = (int)bitOffset % 32;
                var intOffset = ((int)bitOffset - newBitOffset) / 32;
                ptr = (byte*)ptr + (intOffset * 4);
                bitOffset = (uint)newBitOffset;
            }

            // Bits out of byte.
            if (bitOffset + bitCount <= 8)
            {
                var value = *(byte*)ptr;
                value >>= (int)bitOffset;
                var mask = 0xFFu >> (8 - (int)bitCount);
                return value & mask;
            }

            // Bits out of short.
            if (bitOffset + bitCount <= 16)
            {
                var value = *(ushort*)ptr;
                value >>= (int)bitOffset;
                var mask = 0xFFFFu >> (16 - (int)bitCount);
                return value & mask;
            }

            // Bits out of int.
            if (bitOffset + bitCount <= 32)
            {
                var value = *(uint*)ptr;
                value >>= (int)bitOffset;
                var mask = 0xFFFFFFFFu >> (32 - (int)bitCount);
                return value & mask;
            }

            throw new NotImplementedException("Reading int straddling int boundary");
        }

        /// <summary>
        /// Writes unsigned int as bits to memory region, up to and including 32 bits, least-significant bit first (LSB).
        /// </summary>
        /// <param name="ptr">Pointer to memory region.</param>
        /// <param name="bitOffset">Offset in bits from the pointer to the start of the unsigned integer.</param>
        /// <param name="bitCount">Number of bits to read.</param>
        /// <param name="value">Value to write.</param>
        public static void WriteUIntAsMultipleBits(void* ptr, uint bitOffset, uint bitCount, uint value)
        {
            if (ptr == null)
                throw new ArgumentNullException(nameof(ptr));
            if (bitCount > sizeof(int) * 8)
                throw new ArgumentException("Trying to write more than 32 bits as int", nameof(bitCount));

            // Shift the pointer up on larger bitmasks and retry.
            if (bitOffset > 32)
            {
                var newBitOffset = (int)bitOffset % 32;
                var intOffset = ((int)bitOffset - newBitOffset) / 32;
                ptr = (byte*)ptr + (intOffset * 4);
                bitOffset = (uint)newBitOffset;
            }

            // Bits out of byte.
            if (bitOffset + bitCount <= 8)
            {
                var byteValue = (byte)value;
                byteValue <<= (int)bitOffset;
                var mask = ~((0xFFU >> (8 - (int)bitCount)) << (int)bitOffset);
                *(byte*)ptr = (byte)((*(byte*)ptr & mask) | byteValue);
                return;
            }

            // Bits out of short.
            if (bitOffset + bitCount <= 16)
            {
                var ushortValue = (ushort)value;
                ushortValue <<= (int)bitOffset;
                var mask = ~((0xFFFFU >> (16 - (int)bitCount)) << (int)bitOffset);
                *(ushort*)ptr = (ushort)((*(ushort*)ptr & mask) | ushortValue);
                return;
            }

            // Bits out of int.
            if (bitOffset + bitCount <= 32)
            {
                var uintValue = (uint)value;
                uintValue <<= (int)bitOffset;
                var mask = ~((0xFFFFFFFFU >> (32 - (int)bitCount)) << (int)bitOffset);
                *(uint*)ptr = (*(uint*)ptr & mask) | uintValue;
                return;
            }

            throw new NotImplementedException("Writing int straddling int boundary");
        }

        /// <summary>
        /// Reads bits memory region as two's complement integer, up to and including 32 bits, least-significant bit first (LSB).
        /// For example reading 0xff as 8 bits will result in -1.
        /// </summary>
        /// <param name="ptr">Pointer to memory region.</param>
        /// <param name="bitOffset">Offset in bits from the pointer to the start of the integer.</param>
        /// <param name="bitCount">Number of bits to read.</param>
        /// <returns>Read integer.</returns>
        public static int ReadTwosComplementMultipleBitsAsInt(void* ptr, uint bitOffset, uint bitCount)
        {
            // int is already represented as two's complement
            return (int)ReadMultipleBitsAsUInt(ptr, bitOffset, bitCount);
        }

        /// <summary>
        /// Writes bits memory region as two's complement integer, up to and including 32 bits, least-significant bit first (LSB).
        /// </summary>
        /// <param name="ptr">Pointer to memory region.</param>
        /// <param name="bitOffset">Offset in bits from the pointer to the start of the integer.</param>
        /// <param name="bitCount">Number of bits to read.</param>
        /// <param name="value">Value to write.</param>
        public static void WriteIntAsTwosComplementMultipleBits(void* ptr, uint bitOffset, uint bitCount, int value)
        {
            // int is already represented as two's complement, so write as-is
            WriteUIntAsMultipleBits(ptr, bitOffset, bitCount, (uint)value);
        }

        /// <summary>
        /// Reads bits memory region as excess-K integer where K is set to (2^bitCount)/2, up to and including 32 bits, least-significant bit first (LSB).
        /// For example reading 0 as 8 bits will result in -128. Reading 0xff as 8 bits will result in 127.
        /// </summary>
        /// <param name="ptr">Pointer to memory region.</param>
        /// <param name="bitOffset">Offset in bits from the pointer to the start of the integer.</param>
        /// <param name="bitCount">Number of bits to read.</param>
        /// <returns>Read integer.</returns>
        public static int ReadExcessKMultipleBitsAsInt(void* ptr, uint bitOffset, uint bitCount)
        {
            // https://en.wikipedia.org/wiki/Signed_number_representations#Offset_binary
            var value = (long)ReadMultipleBitsAsUInt(ptr, bitOffset, bitCount);
            var halfMax = (long)((1UL << (int)bitCount) / 2);
            return (int)(value - halfMax);
        }

        /// <summary>
        /// Writes bits memory region as excess-K integer where K is set to (2^bitCount)/2, up to and including 32 bits, least-significant bit first (LSB).
        /// </summary>
        /// <param name="ptr">Pointer to memory region.</param>
        /// <param name="bitOffset">Offset in bits from the pointer to the start of the integer.</param>
        /// <param name="bitCount">Number of bits to read.</param>
        /// <param name="value">Value to write.</param>
        public static void WriteIntAsExcessKMultipleBits(void* ptr, uint bitOffset, uint bitCount, int value)
        {
            // https://en.wikipedia.org/wiki/Signed_number_representations#Offset_binary
            var halfMax = (long)((1UL << (int)bitCount) / 2);
            var unsignedValue = halfMax + value;
            WriteUIntAsMultipleBits(ptr, bitOffset, bitCount, (uint)unsignedValue);
        }

        /// <summary>
        /// Reads bits memory region as normalized unsigned integer, up to and including 32 bits, least-significant bit first (LSB).
        /// For example reading 0 as 8 bits will result in 0.0f. Reading 0xff as 8 bits will result in 1.0f.
        /// </summary>
        /// <param name="ptr">Pointer to memory region.</param>
        /// <param name="bitOffset">Offset in bits from the pointer to the start of the unsigned integer.</param>
        /// <param name="bitCount">Number of bits to read.</param>
        /// <returns>Normalized unsigned integer.</returns>
        public static float ReadMultipleBitsAsNormalizedUInt(void* ptr, uint bitOffset, uint bitCount)
        {
            var uintValue = ReadMultipleBitsAsUInt(ptr, bitOffset, bitCount);
            var maxValue = (uint)((1UL << (int)bitCount) - 1);
            return NumberHelpers.UIntToNormalizedFloat(uintValue, 0, maxValue);
        }

        /// <summary>
        /// Writes bits memory region as normalized unsigned integer, up to and including 32 bits, least-significant bit first (LSB).
        /// </summary>
        /// <param name="ptr">Pointer to memory region.</param>
        /// <param name="bitOffset">Offset in bits from the pointer to the start of the unsigned integer.</param>
        /// <param name="bitCount">Number of bits to read.</param>
        /// <param name="value">Normalized value to write.</param>
        public static void WriteNormalizedUIntAsMultipleBits(void* ptr, uint bitOffset, uint bitCount, float value)
        {
            var maxValue = (uint)((1UL << (int)bitCount) - 1);
            var uintValue = NumberHelpers.NormalizedFloatToUInt(value, 0, maxValue);
            WriteUIntAsMultipleBits(ptr, bitOffset, bitCount, uintValue);
        }

        public static void SetBitsInBuffer(void* buffer, int byteOffset, int bitOffset, int sizeInBits, bool value)
        {
            if (buffer == null)
                throw new ArgumentException("A buffer must be provided to apply the bitmask on", nameof(buffer));
            if (sizeInBits < 0)
                throw new ArgumentException("Negative sizeInBits", nameof(sizeInBits));
            if (bitOffset < 0)
                throw new ArgumentException("Negative bitOffset", nameof(bitOffset));
            if (byteOffset < 0)
                throw new ArgumentException("Negative byteOffset", nameof(byteOffset));

            // If we're offset by more than a byte, adjust our pointers.
            if (bitOffset >= 8)
            {
                var skipBytes = bitOffset / 8;
                byteOffset += skipBytes;
                bitOffset %= 8;
            }

            var bytePos = (byte*)buffer + byteOffset;
            var sizeRemainingInBits = sizeInBits;

            // Handle first byte separately if unaligned to byte boundary.
            if (bitOffset != 0)
            {
                var mask = 0xFF << bitOffset;
                if (sizeRemainingInBits + bitOffset < 8)
                {
                    mask &= 0xFF >> (8 - (sizeRemainingInBits + bitOffset));
                }

                if (value)
                    *bytePos |= (byte)mask;
                else
                    *bytePos &= (byte)~mask;
                ++bytePos;
                sizeRemainingInBits -= 8 - bitOffset;
            }

            // Handle full bytes in-between.
            while (sizeRemainingInBits >= 8)
            {
                *bytePos = value ? (byte)0xFF : (byte)0;
                ++bytePos;
                sizeRemainingInBits -= 8;
            }

            // Handle unaligned trailing byte, if present.
            if (sizeRemainingInBits > 0)
            {
                var mask = (byte)(0xFF >> 8 - sizeRemainingInBits);
                if (value)
                    *bytePos |= mask;
                else
                    *bytePos &= (byte)~mask;
            }

            Debug.Assert(bytePos <= (byte*)buffer +
                ComputeFollowingByteOffset((uint)byteOffset, (uint)bitOffset + (uint)sizeInBits));
        }

        public static void Swap<TValue>(ref TValue a, ref TValue b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        public static uint AlignNatural(uint offset, uint sizeInBytes)
        {
            var alignment = Math.Min(8, sizeInBytes);
            return offset.AlignToMultipleOf(alignment);
        }
    }
}
