# dotnet-trx-merge

# Status
[![1]][2] [![6]][7] [![3]][4] [![5]][4]

# Description
This tool scope is to allow the merge of several trx files in a single one. This would prepare the files to later be imported into a reporting tool.

# Installation
```sh
dotnet tool install --global dotnet-trx-merge
```

# Usage
```sh
trx-merge [OPTIONS]
```


## Options
| option               | description                                                                                                            |
| -------------------- |------------------------------------------------------------------------------------------------------------------------|
| `--file, -f`           | Trx file to merge. Can be set several times.                                                                             |
| `--dir, -d`     | Folder to look for trx files.                                                                       |
| `--loglevel` | Log Level. *(default: Verbose)*                                                                                        |
| `--recursive, -r` | Search recursively in folder. Implies --dir is set.                                                         |
| `--output, -o` | Output file path                                                                        |

## 👤 Author & Contributors

👤 **Ricardo Pereira**

- Github: [@ricardofslp](https://github.com/ricardofslp)

👥 **Contributors**

- Github: [@joaoopereira](https://github.com/joaoopereira)

## :handshake: Contributing

Contributions, issues and feature requests are welcome!\
Feel free to check the [issues page](https://github.com/ricardofslp/dotnet-trx-merge/issues).

## Show your support

Give a :star: if this project helped you!

## :memo: License

Copyright © 2023 [Ricardo Pereira](https://github.com/ricardofslp).\
This tool is licensed under MIT License. See the [LICENSE](/LICENSE) file for details.

[1]: https://github.com/ricardofslp/dotnet-trx-merge/actions/workflows/cd.yml/badge.svg
[2]: https://github.com/ricardofslp/dotnet-trx-merge/actions/workflows/cd.yml
[3]: https://img.shields.io/nuget/v/dotnet-trx-merge.svg?label=dotnet-trx-merge
[4]: https://www.nuget.org/packages/dotnet-trx-merge
[5]: https://img.shields.io/nuget/dt/dotnet-trx-merge.svg?label=downloads
[6]: https://coveralls.io/repos/github/ricardofslp/dotnet-trx-merge/badge.svg?branch=main
[7]: https://coveralls.io/github/ricardofslp/dotnet-trx-merge?branch=main