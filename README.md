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

## Usage

### Mouse action

|                | double click           | right click            | right double click | drag              | drag file/folder into       |
|----------------|------------------------|------------------------|--------------------|-------------------|-----------------------------|
| title bar      | toggle auto fold mode  | toggle auto fold mode  | /                  | (move window)     | /                           |
| address bar    | /                      | /                      | /                  | /                 | change directory            |
| item `..`      | go to parent directory | /                      | select directory   | /                 | move into parent directory  |
| file item      | open file              | context menu           | /                  | file dragged      | /                           |
| directory item | goto directory         | context menu           | /                  | directory dragged | move into directory         |
| empty area     | /                      | go to parent directory | /                  |                   | move into current directory |

### Keyboard shortcut

| key            | action                                      |
|----------------|---------------------------------------------|
| `Ctrl+C`       | copy file/folder                            |
| `Ctrl+X`       | cut file/folder                             |
| `Ctrl+V`       | paste to current folder                     |
| `Ctrl+W`       | exit app                                    |
| `Ctrl+O`       | select directory                            |
| `Ctrl+E`       | move window to cursor position              |
| `Delete`       | move item to recycle bin                    |
| `Shift+Delete` | remove item (permanently, will show dialog) |
