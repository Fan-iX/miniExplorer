# miniExploer

A mini file browser based on .net framework winforms

## Build

Run

```
dotnet publish
```

And you will see stand alone executable file in `bin\Publish\miniExplorer.exe`

## Features

* Native file/folder name/icon support
* High DPI support
* Multi-tab support
* Restore opened tabs and window location
* Auto fold mode
* Drag/drop support
* Favorites

## Usage

### Mouse action

|                | click                | double click                | right click               | drag              | drag file/folder into       |
|----------------|----------------------|-----------------------------|---------------------------|-------------------|-----------------------------|
| title bar      | (focus window)       | toggle auto fold mode       | toggle auto fold mode     | (move window)     | /                           |
| `🔖` button    | toggle favorites     | /                           | list explorer directories | /                 | change directory            |
| address bar    | (focus)              | /                           | /                         | /                 | change directory            |
| `←` button     | go to last directory | /                           | list browser histories    | /                 | change directory            |
| `=` button     | settings menu        | /                           | /                         | /                 | /                           |
| tab pages      | (activate tab)       | close tab                   | context menu              | direcotry dragged | (activate)                  |
| `+` tab        | new tab              | /                           | /                         | /                 | new tab                     |
| item `..`      | (focus)              | go to parent directory      | /                         | /                 | move into parent directory  |
| file item      | (focus)              | open file                   | shell context menu        | file dragged      | /                           |
| directory item | (focus)              | go to directory             | shell context menu        | directory dragged | move into directory         |
| favorites item | (focus)              | go to directory / open file | shell context menu        | item dragged      | add to favorites            |
| empty area     | /                    | go to parent directory      | /                         | (select items)    | move into current directory |

### Keyboard shortcut

#### Global

| key              | action                                      |
|------------------|---------------------------------------------|
| `Ctrl + O`       | select directory                            |
| `Ctrl + E`       | move window to cursor position              |
| `Ctrl + W`       | close tab                                   |
| `Ctrl + D`       | add current directory to favorites          |
| `Ctrl + R`/`F5`  | refresh                                     |
| `Ctrl + F`       | focus on adress bar                         |

#### File and Directory list

| key              | action                                      |
|------------------|---------------------------------------------|
| `Ctrl + C`       | copy file/folder                            |
| `Ctrl + X`       | cut file/folder                             |
| `Ctrl + V`       | paste to current folder                     |
| `Delete`         | move item to recycle bin                    |
| `Alt + Up`       | go to parent directory                      |
| `Backspace`      | go to last directory                        |
| `Shift + Delete` | remove item (permanently, will show dialog) |
| `F2`             | start renaming                              |

#### Favorites

| key              | action                                      |
|------------------|---------------------------------------------|
| `Delete`         | remove from favorites                       |
