using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Tests.Client.Common.Api
{
	[TestClass]
	public sealed class MemoryApiTests
	{
		private IMemoryApi CreateDummyApi(byte[] memDomainContents)
			=> new MemoryApi(Console.WriteLine) //TODO capture and check for error messages?
			{
				MemoryDomainCore = new MemoryDomainList(new MemoryDomain[]
				{
					new MemoryDomainByteArray("ADomain", MemoryDomain.Endian.Little, memDomainContents, writable: true, wordSize: 1),
				}),
			};

		[TestMethod]
		public void TestBulkPeekByte()
		{
			var memApi = CreateDummyApi(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF });
			CollectionAssert.That.AreEqual(
				new byte[] { default, default, default },
				memApi.ReadByteRange(addr: -5, length: 3),
				"fully below lower boundary");
			CollectionAssert.That.AreEqual(
				new byte[] { default, default, 0x01, 0x23 },
				memApi.ReadByteRange(addr: -2, length: 4),
				"crosses lower boundary");
			CollectionAssert.That.AreEqual(
				new byte[] { default, 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, default },
				memApi.ReadByteRange(addr: -1, length: 10),
				"crosses both boundaries");
			CollectionAssert.That.AreEqual(
				new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF },
				memApi.ReadByteRange(addr: 0, length: 8),
				"whole domain");
			CollectionAssert.That.AreEqual(
				new byte[] { 0x23, 0x45, 0x67, 0x89, 0xAB },
				memApi.ReadByteRange(addr: 1, length: 5),
				"strict contains");
			CollectionAssert.That.AreEqual(
				Array.Empty<byte>(),
				memApi.ReadByteRange(addr: 3, length: 0),
				"empty");
			CollectionAssert.That.AreEqual(
				new byte[] { 0xCD, 0xEF, default },
				memApi.ReadByteRange(addr: 6, length: 3),
				"crosses upper boundary");
			CollectionAssert.That.AreEqual(
				new byte[] { default, default, default, default },
				memApi.ReadByteRange(addr: 9, length: 4),
				"fully above upper boundary");
		}

		[TestMethod]
		public void TestBulkPokeByte()
		{
			void TestCase(IReadOnlyList<byte> expected, Action<IMemoryApi> action, string message)
			{
				var memDomainContents = new byte[8];
				action(CreateDummyApi(memDomainContents));
				CollectionAssert.That.AreEqual(expected, memDomainContents, message);
			}
			TestCase(
				new byte[8],
				memApi => memApi.WriteByteRange(-5, new byte[] { 0x01, 0x23, 0x45 }),
				"fully below lower boundary");
			TestCase(
				new byte[] { 0x45, 0x67, default, default, default, default, default, default },
				memApi => memApi.WriteByteRange(-2, new byte[] { 0x01, 0x23, 0x45, 0x67 }),
				"crosses lower boundary");
			TestCase(
				new byte[] { 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE },
				memApi => memApi.WriteByteRange(-1, new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC }),
				"crosses both boundaries");
			TestCase(
				new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF },
				memApi => memApi.WriteByteRange(0, new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF }),
				"whole domain");
			TestCase(
				new byte[] { default, 0x01, 0x23, 0x45, 0x67, 0x89, default, default },
				memApi => memApi.WriteByteRange(1, new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89 }),
				"strict contains");
			TestCase(
				new byte[8],
				memApi => memApi.WriteByteRange(3, Array.Empty<byte>()),
				"empty");
			TestCase(
				new byte[] { default, default, default, default, default, default, 0x01, 0x23 },
				memApi => memApi.WriteByteRange(6, new byte[] { 0x01, 0x23, 0x45 }),
				"crosses upper boundary");
			TestCase(
				new byte[8],
				memApi => memApi.WriteByteRange(9, new byte[] { 0x01, 0x23, 0x45, 0x67 }),
				"fully above upper boundary");
		}

		[TestMethod]
		public void TestBulkPeekUshort()
		{
			ushort[] memContents = new ushort[] { 0x0123, 0x4567, 0x89AB, 0xCDEF };
			byte[] memBytes = new byte[8];
			memContents.BytesSpan().CopyTo(memBytes);

			var memApi = CreateDummyApi(memBytes);
			CollectionAssert.That.AreEqual(
				new ushort[] { default, default },
				memApi.ReadU16Range(addr: -6, count: 2),
				"fully below lower boundary");
			CollectionAssert.That.AreEqual(
				new ushort[] { default, 0x0123 },
				memApi.ReadU16Range(addr: -2, count: 2),
				"crosses lower boundary");
			CollectionAssert.That.AreEqual(
				new ushort[] { default, 0x0123, 0x4567, 0x89AB, 0xCDEF, default },
				memApi.ReadU16Range(addr: -2, count: 6),
				"crosses both boundaries");
			CollectionAssert.That.AreEqual(
				new ushort[] { 0x0123, 0x4567, 0x89AB, 0xCDEF },
				memApi.ReadU16Range(addr: 0, count: 4),
				"whole domain");
			CollectionAssert.That.AreEqual(
				new ushort[] { 0x4567, 0x89AB, 0xCDEF },
				memApi.ReadU16Range(addr: 2, count: 3),
				"strict contains");
			CollectionAssert.That.AreEqual(
				Array.Empty<ushort>(),
				memApi.ReadU16Range(addr: 2, count: 0),
				"empty");
			CollectionAssert.That.AreEqual(
				new ushort[] { 0xCDEF, default },
				memApi.ReadU16Range(addr: 6, count: 2),
				"crosses upper boundary");
			CollectionAssert.That.AreEqual(
				new ushort[] { default, default },
				memApi.ReadU16Range(addr: 8, count: 2),
				"fully above upper boundary");
		}

		[TestMethod]
		public void TestBulkPokeUshort()
		{
			unsafe void TestCase(IReadOnlyList<ushort> expected, Action<IMemoryApi> action, string message)
			{
				var memDomainContents = new byte[8];
				action(CreateDummyApi(memDomainContents));
				ushort[] memUshorts = new ushort[4];
				memDomainContents.CopyTo(memUshorts.BytesSpan());

				CollectionAssert.That.AreEqual(expected, memUshorts, message);
			}
			TestCase(
				new ushort[4],
				memApi => memApi.WriteU16Range(-4, new ushort[] { 0x0123, 0x4567 }),
				"fully below lower boundary");
			TestCase(
				new ushort[] { 0x4567, default, default, default },
				memApi => memApi.WriteU16Range(-2, new ushort[] { 0x0123, 0x4567 }),
				"crosses lower boundary");
			TestCase(
				new ushort[] { 0x4567, 0x89AB, 0xCDEF, 0xFEDC },
				memApi => memApi.WriteU16Range(-2, new ushort[] { 0x0123, 0x4567, 0x89AB, 0xCDEF, 0xFEDC, 0xBA98 }),
				"crosses both boundaries");
			TestCase(
				new ushort[] { 0x0123, 0x4567, 0x89AB, 0xCDEF },
				memApi => memApi.WriteU16Range(0, new ushort[] { 0x0123, 0x4567, 0x89AB, 0xCDEF }),
				"whole domain");
			TestCase(
				new ushort[] { default, 0x0123, 0x4567, 0x89AB },
				memApi => memApi.WriteU16Range(2, new ushort[] { 0x0123, 0x4567, 0x89AB }),
				"strict contains");
			TestCase(
				new ushort[4],
				memApi => memApi.WriteU16Range(2, Array.Empty<ushort>()),
				"empty");
			TestCase(
				new ushort[] { default, default, default, 0x0123 },
				memApi => memApi.WriteU16Range(6, new ushort[] { 0x0123, 0x4567 }),
				"crosses upper boundary");
			TestCase(
				new ushort[4],
				memApi => memApi.WriteU16Range(8, new ushort[] { 0x0123, 0x4567 }),
				"fully above upper boundary");
		}

		[TestMethod]
		public void TestBulkPeekUint()
		{
			uint[] memContents = new uint[] { 0x01234567, 0x89ABCDEF };
			byte[] memBytes = new byte[8];
			memContents.BytesSpan().CopyTo(memBytes);

			var memApi = CreateDummyApi(memBytes);
			CollectionAssert.That.AreEqual(
				new uint[] { default, default },
				memApi.ReadU32Range(addr: -10, count: 2),
				"fully below lower boundary");
			CollectionAssert.That.AreEqual(
				new uint[] { default, 0x01234567 },
				memApi.ReadU32Range(addr: -4, count: 2),
				"crosses lower boundary");
			CollectionAssert.That.AreEqual(
				new uint[] { default, 0x01234567, 0x89ABCDEF, default },
				memApi.ReadU32Range(addr: -4, count: 4),
				"crosses both boundaries");
			CollectionAssert.That.AreEqual(
				new uint[] { 0x01234567, 0x89ABCDEF },
				memApi.ReadU32Range(addr: 0, count: 2),
				"whole domain");
			CollectionAssert.That.AreEqual(
				new uint[] { 0x89ABCDEF },
				memApi.ReadU32Range(addr: 4, count: 1),
				"strict contains");
			CollectionAssert.That.AreEqual(
				Array.Empty<uint>(),
				memApi.ReadU32Range(addr: 0, count: 0),
				"empty");
			CollectionAssert.That.AreEqual(
				new uint[] { 0x89ABCDEF, default },
				memApi.ReadU32Range(addr: 4, count: 2),
				"crosses upper boundary");
			CollectionAssert.That.AreEqual(
				new uint[] { default, default },
				memApi.ReadU32Range(addr: 8, count: 2),
				"fully above upper boundary");
		}

		[TestMethod]
		public void TestBulkPokeUint()
		{
			unsafe void TestCase(IReadOnlyList<uint> expected, Action<IMemoryApi> action, string message)
			{
				var memDomainContents = new byte[8];
				action(CreateDummyApi(memDomainContents));
				uint[] memUints = new uint[2];
				memDomainContents.CopyTo(memUints.BytesSpan());

				CollectionAssert.That.AreEqual(expected, memUints, message);
			}
			TestCase(
				new uint[2],
				memApi => memApi.WriteU32Range(-8, new uint[] { 0x01234567, 0x89ABCDEF }),
				"fully below lower boundary");
			TestCase(
				new uint[] { 0x89ABCDEF, default },
				memApi => memApi.WriteU32Range(-4, new uint[] { 0x01234567, 0x89ABCDEF }),
				"crosses lower boundary");
			TestCase(
				new uint[] { 0x89ABCDEF, 0xFEDCBA98 },
				memApi => memApi.WriteU32Range(-4, new uint[] { 0x01234567, 0x89ABCDEF, 0xFEDCBA98, 0x76543210 }),
				"crosses both boundaries");
			TestCase(
				new uint[] { 0x01234567, 0x89ABCDEF },
				memApi => memApi.WriteU32Range(0, new uint[] { 0x01234567, 0x89ABCDEF }),
				"whole domain");
			TestCase(
				new uint[] { default, 0x01234567 },
				memApi => memApi.WriteU32Range(4, new uint[] { 0x01234567 }),
				"strict contains");
			TestCase(
				new uint[2],
				memApi => memApi.WriteU32Range(0, Array.Empty<uint>()),
				"empty");
			TestCase(
				new uint[] { default, 0x01234567 },
				memApi => memApi.WriteU32Range(4, new uint[] { 0x01234567, 0x89ABCDEF }),
				"crosses upper boundary");
			TestCase(
				new uint[2],
				memApi => memApi.WriteU32Range(8, new uint[] { 0x01234567, 0x89ABCDEF }),
				"fully above upper boundary");
		}

		[TestMethod]
		public void TestHash()
		{
			var memApi = CreateDummyApi(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF });
			_ = Assert.Throws<ArgumentException>(
				() => memApi.HashRegion(addr: -5, count: 3),
				"fully below lower boundary");
			_ = Assert.Throws<ArgumentException>(
				() => memApi.HashRegion(addr: -2, count: 4),
				"crosses lower boundary");
			_ = Assert.Throws<ArgumentException>(
				() => memApi.HashRegion(addr: -1, count: 10),
				"crosses both boundaries");
			Assert.AreEqual(
				"55C53F5D490297900CEFA825D0C8E8E9532EE8A118ABE7D8570762CD38BE9818",
				memApi.HashRegion(addr: 0, count: 8),
				"whole domain");
			Assert.AreEqual(
				"233ABF8F525463C943A10F4DC3080B1BDA19D2EEAA4ACF4676F44689CED29232",
				memApi.HashRegion(addr: 1, count: 5),
				"strict contains");
			Assert.AreEqual(
				SHA256Checksum.EmptyFile,
				memApi.HashRegion(addr: 3, count: 0),
				"empty");
			_ = Assert.Throws<ArgumentException>(
				() => memApi.HashRegion(addr: 6, count: 3),
				"crosses upper boundary");
			_ = Assert.Throws<ArgumentException>(
				() => memApi.HashRegion(addr: 9, count: 4),
				"fully above upper boundary");
		}
	}
}
