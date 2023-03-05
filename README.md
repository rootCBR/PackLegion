**PackLegion is deprecated and has been superceded by [DisruptManager](https://github.com/rootCBR/DisruptManager).**

# PackLegion

This tool creates a new DAT/FAT file pair containing the files of the specified input folder.

## Usage

Executing the program without any arguments specified will prompt you with a description of all available command line options and a number of usage examples.

### Config

For any use case it is necessary to set up the paths inside `config.xml` properly. `OriginalCommonPath` and `OriginalPatchPath` should point to the FAT file of the game's original `common` and `patch` archives respectively. It is strongly recommended to copy the game's original `patch` archive into a seperate folder to then point `OriginalPatchPath` to.

#### Example

```xml
<Config>
    <OriginalCommonPath>D:\Games\Ubisoft\Watch Dogs Legion\data_win64\common.fat</OriginalCommonPath>
    <OriginalPatchPath>D:\Games\Ubisoft\Watch Dogs Legion\data_win64\_orig\patch.fat</OriginalPatchPath>
</Config>
```

### Command-line

`PackLegion.exe [options] [inputFolder] [outputArchive]`
* `options`         Options according to the list below (optional).
* `inputFolder`     Path to the folder containing the modded game files to pack.
* `outputArchive`   Path of the output archive.

#### Options
* `-o|original`     Use the game's original patch archive as a base for the output archive
                    instead of the output archive itself.
* `-c|combine`      Automatically combine your modified library files (*.lib) with
                    those that are already contained in the game files.
                    Make sure your modified library files only contain the modified objects.
* `-v|verbose`      Enable additional logging.
* `-n|nfo`          Generate *.nfo file along with the output archive.

#### Example (Batch)

```
"D:\Modding\Disrupt\Tools\PackLegion\PackLegion.exe" -o -c "D:\Modding\Disrupt\WDL\_patch" "D:\Games\Ubisoft\Watch Dogs Legion\data_win64\patch.fat"
pause
```
