﻿using System;
using MinecraftServerSharp.Data.IO;
using MinecraftServerSharp.NBT;
using MinecraftServerSharp.World;

namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.ChunkData)]
    public struct ServerChunkData : IWritablePacket
    {
        [PacketProperty(0)] public Chunk Chunk { get; }
        [PacketProperty(1)] public bool FullChunk { get; }

        // TODO: create flash-copying (fast deep-clone) of chunks for serialization purposes

        public ServerChunkData(Chunk chunk, bool fullChunk)
        {
            Chunk = chunk;
            FullChunk = fullChunk;
        }

        public void Write(NetBinaryWriter writer)
        {
            writer.Write(Chunk.X);
            writer.Write(Chunk.Z);
            writer.Write(FullChunk);

            int mask = GetSectionMask(Chunk);
            writer.WriteVar(mask);

            var motionBlocking = new NbtLongArray(36, (Utf8String)"MOTION_BLOCKING");
            writer.Write(motionBlocking.AsCompound((Utf8String)"Heightmaps"));

            if (FullChunk)
            {
                Span<int> biomes = stackalloc int[1024];
                biomes.Fill(8); // 127=void
                writer.Write(biomes);
            }

            int sectionDataLength = GetChunkSectionDataLength(Chunk);
            writer.WriteVar(sectionDataLength);
            WriteChunk(writer, Chunk);

            // If you don't support block entities yet, use 0
            // If you need to implement it by sending block entities later with the update block entity packet,
            // do it that way and send 0 as well.  (Note that 1.10.1 (not 1.10 or 1.10.2) will not accept that)
            writer.WriteVar(0);

            //WriteVarInt(data, chunk.BlockEntities.Length);
            //foreach (CompoundTag tag in chunk.BlockEntities)
            //{
            //    WriteCompoundTag(data, tag);
            //}
        }

        public static int GetSectionMask(Chunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            int sectionMask = 0;
            for (int sectionY = 0; sectionY < Chunk.SectionCount; sectionY++)
            {
                if (!chunk[sectionY].IsEmpty)
                    sectionMask |= 1 << sectionY; // Set that bit to true in the mask
            }
            return sectionMask;
        }

        public static int GetChunkSectionDataLength(Chunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            int length = 0;

            for (int sectionY = 0; sectionY < Chunk.SectionCount; sectionY++)
            {
                var section = chunk[sectionY];
                if (!section.IsEmpty)
                    length += GetChunkSectionDataLength(section);
            }

            return length;
        }

        public static void WriteChunk(NetBinaryWriter writer, Chunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            for (int sectionY = 0; sectionY < Chunk.SectionCount; sectionY++)
            {
                var section = chunk[sectionY];
                if (!section.IsEmpty)
                    WriteChunkSection(writer, section);
            }
        }


        private static int GetUnderlyingDataLength(int bitsPerBlock)
        {
            return ChunkSection.BlockCount * bitsPerBlock / 64;
        }

        public static int GetChunkSectionDataLength(ChunkSection section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            IBlockPalette palette = section.BlockPalette;

            int length = 0;
            length += sizeof(short);
            length += sizeof(byte);
            length += palette.GetEncodedSize();

            int underlyingDataLength = GetUnderlyingDataLength(palette.BitsPerBlock);
            length += VarInt.GetEncodedSize(underlyingDataLength);
            length += underlyingDataLength * sizeof(ulong);

            return length;
        }

        // TODO: optimize
        public static void WriteChunkSection(NetBinaryWriter writer, ChunkSection section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            IBlockPalette palette = section.BlockPalette;
            int bitsPerBlock = palette.BitsPerBlock;

            writer.Write((short)ChunkSection.BlockCount);

            writer.Write((byte)bitsPerBlock);
            palette.Write(writer);

            int dataLength = GetUnderlyingDataLength(bitsPerBlock);

            // A bitmask that contains bitsPerBlock set bits
            uint individualValueMask = (uint)((1 << bitsPerBlock) - 1);

            // TODO: better allocation management, 8k on stack is not great
            Span<ulong> data = dataLength <= 1024 ? stackalloc ulong[dataLength] : new ulong[dataLength];

            int blockIndex = 0;
            for (int y = 0; y < ChunkSection.Height; y++)
            {
                for (int z = 0; z < ChunkSection.Width; z++)
                {
                    for (int x = 0; x < ChunkSection.Width; x++, blockIndex++)
                    {
                        int startLong = blockIndex * bitsPerBlock / 64;
                        int startOffset = blockIndex * bitsPerBlock % 64;
                        int endLong = ((blockIndex + 1) * bitsPerBlock - 1) / 64;

                        BlockState state = section.GetBlockState(blockIndex);
                        ulong value = palette.IdForState(state);
                        value &= individualValueMask;

                        data[startLong] |= value << startOffset;
                        if (startLong != endLong)
                            data[endLong] = value >> (64 - startOffset);
                    }
                }
            }

            writer.WriteVar(dataLength);
            writer.Write(data);
        }
    }
}
