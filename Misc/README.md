# Misc

This directory is for helper tools.

## CalcRor13Hash

This tool is to calculate ROR13 hash of API name or DLL name for shellcoding.
If you want to calculate the hash for ASCII string, set name with `-a` option.

```
PS C:\Dev> .\CalcRor13Hash.exe -a GetProcAddress

[*] Input (ASCII) : GetProcAddress
[*] ROR13 Hash    : 0x7C0DFCAA

PS C:\Dev> .\CalcRor13Hash.exe -a GETPROCADDRESS

[*] Input (ASCII) : GETPROCADDRESS
[*] ROR13 Hash    : 0x1ACAEE7A
```

To caluculate for Unicode string, set name with `-u` option:

```
PS C:\Dev> .\CalcRor13Hash.exe -u kernel32.dll

[*] Input (Unicode) : kernel32.dll
[*] ROR13 Hash      : 0xBF5AFD6F
```


## PeRipper

This tool is for dumping executable code from PE file.

```
PS C:\Dev> .\PeRipper.exe -h

PeRipper - Tool to get byte data from PE file.

Usage: PeRipper.exe [Options]

        -h, --help           : Displays this help message.
        -a, --analyze        : Flag to get PE file's information.
        -d, --dump           : Flag to dump data bytes.
        -e, --export         : Flag to export raw data bytes to a file.
        -f, --format         : Specifies output format of dump data. "cs", "c" and "py" are allowed.
        -s, --size           : Specifies data size to rip.
        -p, --pe             : Specifies a PE file to load.
        -r, --rawoffset      : Specifies base address to rip with PointerToRawData.
        -v, --virtualaddress : Specifies base address to rip with VirtualAddress.
```

To check a target PE file's section and export function's information, set `-a` flag as well as a target PE file with `-p` option:

```
PS C:\Dev> .\PeRipper.exe -p C:\Windows\System32\notepad.exe -a

[*] Raw Data Size : 201216 bytes
[*] Architecture  : AMD64
[*] Header Size   : 0x400 bytes
[*] EntryPoint:
    [*] VirtualAddress   : 0x00023F40
    [*] PointerToRawData : 0x00023340
[*] Sections (Count = 7):
    [*] .text Section:
        [*] VirtualAddress   : 0x00001000
        [*] PointerToRawData : 0x00000400
        [*] VirtualSize      : 0x247FF
        [*] SizeOfRawData    : 0x24800
    [*] .rdata Section:
        [*] VirtualAddress   : 0x00026000
        [*] PointerToRawData : 0x00024C00
        [*] VirtualSize      : 0x9280
        [*] SizeOfRawData    : 0x9400
    [*] .data Section:
        [*] VirtualAddress   : 0x00030000
        [*] PointerToRawData : 0x0002E000
        [*] VirtualSize      : 0x2728
        [*] SizeOfRawData    : 0xE00
    [*] .pdata Section:
        [*] VirtualAddress   : 0x00033000
        [*] PointerToRawData : 0x0002EE00
        [*] VirtualSize      : 0x10EC
        [*] SizeOfRawData    : 0x1200
    [*] .didat Section:
        [*] VirtualAddress   : 0x00035000
        [*] PointerToRawData : 0x00030000
        [*] VirtualSize      : 0x178
        [*] SizeOfRawData    : 0x200
    [*] .rsrc Section:
        [*] VirtualAddress   : 0x00036000
        [*] PointerToRawData : 0x00030200
        [*] VirtualSize      : 0xBD8
        [*] SizeOfRawData    : 0xC00
    [*] .reloc Section:
        [*] VirtualAddress   : 0x00037000
        [*] PointerToRawData : 0x00030E00
        [*] VirtualSize      : 0x2D4
        [*] SizeOfRawData    : 0x400
[*] Export functions (Count = 0):
[*] Done.
```

To dump bytes from a target PE file, set `-d` flag as follows.
Base address and size must be specified in hex format.
If you want to use virutal address as base address, set the value with `-v` option:

```
PS C:\Dev> .\PeRipper.exe -p C:\Windows\System32\notepad.exe -d -v 0x1000 -s 0x40

[*] Raw Data Size : 201216 bytes
[*] Architecture  : AMD64
[*] Header Size   : 0x400 bytes
[*] VirtualAddress (0x00001000) is in .text section.
[*] Dump 0x40 bytes in Hex Dump format:

                       00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F

    0000000000001000 | CC CC CC CC CC CC CC CC-4C 8B DC 48 81 EC 88 00 | IIIIIIII L.ÜH.ì..
    0000000000001010 | 00 00 48 8B 05 57 F4 02-00 48 33 C4 48 89 44 24 | ..H..Wô. .H3ÄH.D$
    0000000000001020 | 70 48 8B 84 24 B8 00 00-00 45 33 C9 49 89 43 D8 | pH..$,.. .E3ÉI.CO
    0000000000001030 | 45 33 C0 48 8B 84 24 B0-00 00 00 83 64 24 6C 00 | E3AH..$° ....d$l.

[*] Done.
```

If you want to use raw data offset as base address, set the value with `-r` option:

```
PS C:\Dev> .\PeRipper.exe -p C:\Windows\System32\notepad.exe -d -r 0x400 -s 0x40

[*] Raw Data Size : 201216 bytes
[*] Architecture  : AMD64
[*] Header Size   : 0x400 bytes
[*] PointerToRawData (0x00000400) is in .text section.
[*] Dump 0x40 bytes in Hex Dump format:

                       00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F

    0000000000000400 | CC CC CC CC CC CC CC CC-4C 8B DC 48 81 EC 88 00 | IIIIIIII L.ÜH.ì..
    0000000000000410 | 00 00 48 8B 05 57 F4 02-00 48 33 C4 48 89 44 24 | ..H..Wô. .H3ÄH.D$
    0000000000000420 | 70 48 8B 84 24 B8 00 00-00 45 33 C9 49 89 43 D8 | pH..$,.. .E3ÉI.CO
    0000000000000430 | 45 33 C0 48 8B 84 24 B0-00 00 00 83 64 24 6C 00 | E3AH..$° ....d$l.

[*] Done.
```

To dump data as some programing language format, set `-f` option.
It supports `cs` (CSharp), `c` (C/C++) and `py` (Python):

```
PS C:\Dev> .\PeRipper.exe -p C:\Windows\System32\notepad.exe -d -r 0x400 -s 0x40 -f cs

[*] Raw Data Size : 201216 bytes
[*] Architecture  : AMD64
[*] Header Size   : 0x400 bytes
[*] PointerToRawData (0x00000400) is in .text section.
[*] Dump 0x40 bytes in CSharp format:

var data = new byte[] {
    0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0x4C, 0x8B, 0xDC, 0x48,
    0x81, 0xEC, 0x88, 0x00, 0x00, 0x00, 0x48, 0x8B, 0x05, 0x57, 0xF4, 0x02,
    0x00, 0x48, 0x33, 0xC4, 0x48, 0x89, 0x44, 0x24, 0x70, 0x48, 0x8B, 0x84,
    0x24, 0xB8, 0x00, 0x00, 0x00, 0x45, 0x33, 0xC9, 0x49, 0x89, 0x43, 0xD8,
    0x45, 0x33, 0xC0, 0x48, 0x8B, 0x84, 0x24, 0xB0, 0x00, 0x00, 0x00, 0x83,
    0x64, 0x24, 0x6C, 0x00
};

[*] Done.

PS C:\Dev> .\PeRipper.exe -p C:\Windows\System32\notepad.exe -d -r 0x400 -s 0x40 -f c

[*] Raw Data Size : 201216 bytes
[*] Architecture  : AMD64
[*] Header Size   : 0x400 bytes
[*] PointerToRawData (0x00000400) is in .text section.
[*] Dump 0x40 bytes in C Language format:

char data[] = {
    0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0x4C, 0x8B, 0xDC, 0x48,
    0x81, 0xEC, 0x88, 0x00, 0x00, 0x00, 0x48, 0x8B, 0x05, 0x57, 0xF4, 0x02,
    0x00, 0x48, 0x33, 0xC4, 0x48, 0x89, 0x44, 0x24, 0x70, 0x48, 0x8B, 0x84,
    0x24, 0xB8, 0x00, 0x00, 0x00, 0x45, 0x33, 0xC9, 0x49, 0x89, 0x43, 0xD8,
    0x45, 0x33, 0xC0, 0x48, 0x8B, 0x84, 0x24, 0xB0, 0x00, 0x00, 0x00, 0x83,
    0x64, 0x24, 0x6C, 0x00
};

[*] Done.

PS C:\Dev> .\PeRipper.exe -p C:\Windows\System32\notepad.exe -d -r 0x400 -s 0x40 -f py

[*] Raw Data Size : 201216 bytes
[*] Architecture  : AMD64
[*] Header Size   : 0x400 bytes
[*] PointerToRawData (0x00000400) is in .text section.
[*] Dump 0x40 bytes in Python format:

data = bytearray(
    b"\xCC\xCC\xCC\xCC\xCC\xCC\xCC\xCC\x4C\x8B\xDC\x48"
    b"\x81\xEC\x88\x00\x00\x00\x48\x8B\x05\x57\xF4\x02"
    b"\x00\x48\x33\xC4\x48\x89\x44\x24\x70\x48\x8B\x84"
    b"\x24\xB8\x00\x00\x00\x45\x33\xC9\x49\x89\x43\xD8"
    b"\x45\x33\xC0\x48\x8B\x84\x24\xB0\x00\x00\x00\x83"
    b"\x64\x24\x6C\x00"
)

[*] Done.
```

To export raw data bytes into a file, set `-e` flag insted of `-d` flag.
Exported files are named as `bytes_from_module.bin` or `bytes_from_module_{index}.bin`:

```
PS C:\Dev> .\PeRipper.exe -p C:\Windows\System32\notepad.exe -e -r 0x80 -s 0x40

[*] Raw Data Size : 201216 bytes
[*] Architecture  : AMD64
[*] Header Size   : 0x400 bytes
[*] The specified base address is in header region.
[*] Export 0x40 bytes raw data to C:\Dev\bytes_from_module.bin.
[*] Done.

PS C:\Dev> Format-Hex .\bytes_from_module.bin


           Path: C:\Dev\bytes_from_module.bin

           00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F

00000000   A2 13 95 77 E6 72 FB 24 E6 72 FB 24 E6 72 FB 24  ¢.wærû$ærû$ærû$
00000010   EF 0A 68 24 D6 72 FB 24 F2 19 FF 25 EC 72 FB 24  ï.h$Örû$ò..%ìrû$
00000020   F2 19 F8 25 E5 72 FB 24 F2 19 FA 25 EF 72 FB 24  ò.ø%årû$ò.ú%ïrû$
00000030   E6 72 FA 24 CE 77 FB 24 F2 19 F3 25 F9 72 FB 24  ærú$Îwû$ò.ó%ùrû$
```