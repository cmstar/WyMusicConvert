# 简介

一个命令行工具，用于转换网易云音乐客户端下载的NCM加密文件到未加密格式。

需要 .net framework 4.5，Win8 及以上 Windows 系统自带，Win7 需要安装。

基于网易云音乐v2.5.1(Build:196734)开发。

## 感谢

- [anonymous5l/ncmdump](https://github.com/anonymous5l/ncmdump) 提供了解密算法。
- [GameBelial/ncmdump](https://github.com/GameBelial/ncmdump) 提供了.net版本的实现参考。


# 使用方法

## 基本格式

程序的预设名称为 wyconv.exe。命令行参数的基本格式如下：

    wyconv.exe OPTION {ARGS}

- OPTION 指定操作的类型；
- {ARGS} 可变参数表，取决于使用何种 OPTION。

## OPTION分类

可以给定完整的操作名称，也可以只给出最前面的几个字符，只要能按前缀匹配到唯一一个结果即可，如下面几种方式效果相同

    wyconv ncm ...
    wyconv nc ...
    wyconv n ...

下文给出目前可用的操作。

### ncm

转换NCM加密文件到未加密格式。

    wyconv ncm [-f, --force] PATH [PATH [...]]

必须：

- PATH 指定一组需要转换的文件或目录。

可选：

- -f, --force 若指定此参数，则当解密后的目标文件已存在时，仍执行解密；否则该文件被跳过。

示例：

    wyconv ncm D:\CloudMusic\
    wyconv ncm -f D:\CloudMusic\a.ncm D:\CloudMusic\b.ncm

### uc

将缓存文件（.uc）转换为 .mp3 。

    wyconv uc [-f, --force] PATH [PATH [...]]

必须：

- PATH 指定一组需要转换的文件或目录。

可选：

- -f, --force 若指定此参数，则转换后的目标文件已存在时，仍执行转换；否则该文件被跳过。

示例：

    wyconv uc D:\CloudMusic\cache
    wyconv uc -f D:\CloudMusic\cache\614514-320-c138e9.uc.ncm

> 由于 .uc 不带有歌曲名称及描述信息，转换后的文件仅包含音频。歌曲信息需从网易的 API 或网页抓取，目前还没有实现，转换后这些信息还需人工补充。
> 