﻿using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtLongArray : NbtArray<long>
    {
        public override NbtType Type => NbtType.LongArray;

        public NbtLongArray(Utf8String? name, int count) : base(name, count)
        {
        }

        public NbtLongArray(int count) : base(null, count)
        {
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            writer.Write(Items);
        }
    }
}