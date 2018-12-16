# 简介

一个命令行工具，用于转换网易云音乐客户端下载的NCM加密文件到未加密格式。

需要 .net framework 4.5，Win8 及以上 Windows 系统自带，Win7 需要安装。

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


## 接下来……

转换客户端缓存文件（*.uc）。