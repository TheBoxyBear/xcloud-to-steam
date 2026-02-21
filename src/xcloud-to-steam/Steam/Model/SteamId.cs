using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace xCloudToSteam.Steam.Model;

[InlineArray(4)]
public struct SteamId
{
	private byte _first;

	public SteamId(in ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length != 4)
			throw new ArgumentException("Id must be exactly 4 bytes long.", nameof(bytes));

		bytes.CopyTo(this);
	}

	public SteamId(int data)
		=> Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference<byte>(this), data);

	public SteamId(uint data)
		=> Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference<byte>(this), data);

	public static explicit operator SteamId(in ReadOnlySpan<byte> bytes) => new(bytes);

	public static explicit operator SteamId(int data) => new(data);

	public static explicit operator SteamId(uint data) => new(data);

	public static explicit operator int(SteamId id) => Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference<byte>(id));

	public static explicit operator uint(SteamId id) => Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference<byte>(id));

	public override readonly string ToString() => ((uint)this).ToString();
}
