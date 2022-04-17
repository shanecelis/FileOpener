# FileOpener README

Lorem ipsum description.

## Installation

### Requirements

* Unity 2018.3 or later

```
$ git clone https://github.com/SeawispHunter/FileOpener.git
```

### Usage

Find the `manifest.json` file in the `Packages` directory in your project and edit it as follows:
```
{
  "dependencies": {
    "com.seawisphunter.fileopener": "https://github.com/SeawispHunter/FileOpener.git",
    ...
  },
}
```
To select a particular version of the package, add the suffix `#{git-tag}`.

* e.g. `"com.seawisphunter.fileopener": "https://github.com/SeawispHunter/FileOpener.git#1.2.0"`


## Acknowledgements

This package was originally generated from [Shane Celis](https://twitter.com/shanecelis)'s [unity-package-template](https://github.com/shanecelis/unity-package-template).
