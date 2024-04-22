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
* Remember last window location
* Auto fold mode
* Drag/drop support
* Favorites

## Usage

### Mouse action

|                | double click                | right click              | drag              | drag file/folder into       |
|----------------|-----------------------------|--------------------------|-------------------|-----------------------------|
| title bar      | toggle auto fold mode       | toggle auto fold mode    | (move window)     | /                           |
| address bar    | /                           | /                        | /                 | change directory            |
| item `..`      | go to parent directory      | /                        | /                 | move into parent directory  |
| file item      | open file                   | context menu             | file dragged      | /                           |
| directory item | go to directory             | context menu             | directory dragged | move into directory         |
| favorites item | go to directory / open file | context menu             | item dragged      | add to favorites            |
| empty area     | go to parent directory      | go to explorer directory |                   | move into current directory |

### Keyboard shortcut

#### Global

| key              | action                                      |
|------------------|---------------------------------------------|
| `Ctrl + O`       | select directory                            |
| `Ctrl + E`       | move window to cursor position              |
| `Ctrl + W`       | exit app                                    |
| `Ctrl + D`       | add current directory to favorites          |

#### File and Directory list

| key              | action                                      |
|------------------|---------------------------------------------|
| `Ctrl + C`       | copy file/folder                            |
| `Ctrl + X`       | cut file/folder                             |
| `Ctrl + V`       | paste to current folder                     |
| `Delete`         | move item to recycle bin                    |
| `Alt + Up`       | go to parent directory                      |
| `Shift + Delete` | remove item (permanently, will show dialog) |
| `F2`             | start renaming                              |

#### Favorites

| key              | action                                      |
|------------------|---------------------------------------------|
| `Delete`         | remove from favorites                       |