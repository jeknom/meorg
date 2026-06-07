# MeOrg

This is a command line tool that deduplicates and organizes media files from one directory to another by copying said files.

The tool recursively finds all media files from a provided source directory and a target directory. By default, the app will generate a sampled hash for each file to detect if it already exists in the target directory. If the file already exists, it will not be copied to the target.

The creation time of each file is inferred from either EXIF metadata or file system metadata if EXIF data is not available. The files are then organized into directories by the date they were created. By default, if a file is taken 4 hours after midnight, it will be organized into the previous day's directory. This is done with the assumption that if you have been partying past midnight, you still want those files to be organized under the directory where you mentally consider them to be within the same day.

After the files have been organized you can manually add suffixes by renaming directories to quickly recognize specific days. Even if you run the tool to the same target again, it will use the directory regardless if it has a suffix or not. However, if multiple directories prefixed with the date exist, the one without the suffix will be used as the default.

### Example 

By running `meorg organize --source my-media --target my-organized-media` for this kind of source directory:

```
my-media
├── backup-from-old-computer
│   ├── pics
│   │   └── IMG_1234.jpg
│   └── vids
│       └── VID_1234.mp4
└── IMG_4321.jpg
```

This kind of target will be generated:

```
my-organized-media
├── 2023-05-05
│   └── IMG_4321.jpg
├── 2023-06-18
│   └── IMG_1234.jpg
└── 2024-09-20
    └── VID_1234.mp4
```


## Install

**Prerequisite:** [Install .NET 10](https://dotnet.microsoft.com/en-us/download)

- Navigate to the app project: `cd ./MeOrg`.
- Create executable: `dotnet publish -c Release -r <runtime-id> --self-contained true -p:PublishSingleFile=true`
  - Replace \<runtime-id\> with your target platform: `win-x64` — Windows 64-bit, `linux-x64` — Linux 64-bit, `osx-x64` — macOS Intel, `osx-arm64` — macOS Apple Silicon
- Create symlink (at-least on Mac & Linux): `sudo ln -s <absolute-path-to-repo>/meorg/MeOrg/bin/Release/net10.0/osx-arm64/publish/MeOrg /usr/local/bin/meorg`
  - The executable will be created under `./MeOrg/bin/Release/net10.0/<runtime-id>/publish/` so you can also run it from there or setup any way that best suits you for running it.

## Usage

The simple usage is to run the following command:

> meorg organize --source \<path-to-your-source-directory\> --target \<path-to-your-target-directory\>

## Organize help print

```
Usage:
  MeOrg organize [options]

Options:
  --source <source> (REQUIRED)           Unorganized media source directory.
  --target <target> (REQUIRED)           Directory where to copy your organized media.
  --skip-dedupe                          Disables duplicate detection.
  --day-offset-hours <day-offset-hours>  Number of hours past midnight that still 
                                         count as the previous day. With the default 
                                         of 4, a photo taken at 3AM is filed under the 
                                         previous day's directory instead of the 
                                         current one. [default: 4]
  -?, -h, --help                         Show help and usage information
```

## Supported media types

Most common file types are supported (JPEG, PNG, HEIC, RAW, MP4, MOV, MKV...).

[For the full list of supported types, click here.](./MeOrg/src/Constants.cs)

## License

MeOrg is free software, licensed under the [GNU General Public License v3.0](./LICENSE).

Copyright (C) 2026 Johannes Palvanen

This program is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
PARTICULAR PURPOSE. See the [LICENSE](./LICENSE) file for details.