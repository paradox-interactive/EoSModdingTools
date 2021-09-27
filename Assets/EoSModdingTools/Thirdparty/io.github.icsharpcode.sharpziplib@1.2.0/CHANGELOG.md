# [1.2.0] - 2019-08-10

## Fixes:

 - ZipEntry name mismatch when attempting to delete a directory entry ([#295](https://github.com/icsharpcode/SharpZipLib/pull/295))

- Revert ArraySegment simplification to speed up CRC32 calculation ([#301](https://github.com/icsharpcode/SharpZipLib/pull/301))

- Allow AES Zip to better handle reading partial stream data ([#308](https://github.com/icsharpcode/SharpZipLib/pull/308))

- Always write Zip64 extra size fields when size is -1 (too big for non-Zip64) ([#314](https://github.com/icsharpcode/SharpZipLib/pull/314))

- Throw exception when attempting to read a zero code length symbol (

  \#316

  )

  - This should fix most issues where reading Zip-files get stuck in an infinite loop

- `ZipOutputStream.CloseEntry()` now works for Stored AES encrypted entries ([#323](https://github.com/icsharpcode/SharpZipLib/pull/323))

- Empty string is now treated as no RootPath in TarArchive ([#336](https://github.com/icsharpcode/SharpZipLib/pull/336))

- ZipAESStream now handle reads of less data than the AES block size ([#331](https://github.com/icsharpcode/SharpZipLib/pull/331))

- Flushing a GZipOutputStream now attempts to deflate all input data before writing it to the underlying stream ([#225](https://github.com/icsharpcode/SharpZipLib/pull/225))

- StrongEncryption flag is no longer (incorrectly) set for WinzipAES encrypted entries ([#329](https://github.com/icsharpcode/SharpZipLib/pull/329))

- Attempting to read 0 bytes from a `GZipInputStream` no longer causes it to hang indefinitely ([#372](https://github.com/icsharpcode/SharpZipLib/pull/372))


## Features:

- HostSystem can now be set for Zipfiles, allowing creation of files targeting Linux filesystems ([#325](https://github.com/icsharpcode/SharpZipLib/pull/325))

- The SharpZip custom `Exception` types now implements `ISerializable` ([#369](https://github.com/icsharpcode/SharpZipLib/pull/369))

  - This allows them to be transmitted when using WCF

## Changes:

- ZipFile constructor now has a `leaveOpen` parameter ([#302](https://github.com/icsharpcode/SharpZipLib/pull/302))
- FastZip.ExtractZip now sets `isStreamOwner` in the ZipFile constructor ([#311](https://github.com/icsharpcode/SharpZipLib/pull/311))
- ZipFile now always tries to find the Zip64 central directory and prefers it if exists ([#369](https://github.com/icsharpcode/SharpZipLib/pull/369))
  - This will allow for better compatibility with other archivers.