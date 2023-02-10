namespace Nagule;

public static class GuidHelper
{
    public static Guid Merge(Guid g1, Guid g2)
    {
        const int ByteCount = 16;

        var destBytes = new byte[ByteCount];
        var guid1Bytes = g1.ToByteArray();
        var guid2Bytes = g2.ToByteArray();

        for (int i = 0; i < ByteCount; i++) {
            destBytes[i] = (byte)(guid1Bytes[i] ^ guid2Bytes[i]);
        }
        return new Guid(destBytes);
    }
}